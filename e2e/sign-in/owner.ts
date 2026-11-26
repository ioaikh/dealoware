import { spawnSync } from 'node:child_process';
import crypto from 'node:crypto';
import path from 'node:path';

/**
 * The Core admin owner of the sign-in browser tests.
 *
 * Before every test the owner is put into one of two states, straight into
 * the run's database: a password and an enrolled authenticator (with its ten
 * recovery codes), or a password only. Everything a sign-in leaves behind
 * (pending sign-ins, sessions, failure counts, locks) is cleared with it, so
 * no test depends on what ran before.
 *
 * The values are worked out here the way the app does it, with the two
 * run-time keys run.sh hands over; seed_owner.py only writes the rows:
 *
 *   password         PBKDF2-HMAC-SHA512, 220,000 rounds (AdminPasswordHasher)
 *   authenticator    20 random bytes, AES-256-GCM as nonce + tag + ciphertext
 *                    (TotpSecretProtector)
 *   recovery codes   HMAC-SHA256 of the code without dashes, upper case
 *                    (AdminRecoveryCodeHasher)
 *
 * The password, the authenticator key and the recovery codes are made up at
 * run time and exist only in this process and in that throwaway database.
 */

const DB = process.env.SIGN_IN_E2E_DB ?? '';
const PYTHON = process.env.SIGN_IN_E2E_PYTHON ?? '';
const TOTP_KEY = process.env.SIGN_IN_E2E_TOTP_KEY ?? '';
const RECOVERY_KEY = process.env.SIGN_IN_E2E_RECOVERY_KEY ?? '';

export const OWNER_EMAIL = process.env.SIGN_IN_E2E_OWNER ?? '';

// Made up for this run. Hex only, so NFKC leaves it as it is.
export const OWNER_PASSWORD = crypto.randomBytes(16).toString('hex');

const PASSWORD_ROUNDS = 220_000;
const TOTP_PERIOD_SECONDS = 30;
const RECOVERY_ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
const RECOVERY_CODE_COUNT = 10;

export type Owner = {
  email: string;
  password: string;
  /** The authenticator key, or null for an owner who has not enrolled. */
  secret: Buffer | null;
  /** Unused recovery codes, as shown to the owner once: XXXX-XXXX-XXXX. */
  recoveryCodes: string[];
};

function hashPassword(password: string): string {
  const salt = crypto.randomBytes(16);
  const key = crypto.pbkdf2Sync(password, salt, PASSWORD_ROUNDS, 32, 'sha512');
  return `pbkdf2-sha512$${PASSWORD_ROUNDS}$${salt.toString('base64')}$${key.toString('base64')}`;
}

// The slow part. The password is the same for the whole run, so hash it once.
let ownerPasswordHash: string | undefined;

function encryptAuthenticatorKey(secret: Buffer): string {
  const nonce = crypto.randomBytes(12);
  const cipher = crypto.createCipheriv('aes-256-gcm', Buffer.from(TOTP_KEY, 'base64'), nonce);
  const ciphertext = Buffer.concat([cipher.update(secret), cipher.final()]);
  return Buffer.concat([nonce, cipher.getAuthTag(), ciphertext]).toString('base64');
}

function hashRecoveryCode(code: string): string {
  const normalised = code.replace(/[-\s]/g, '').toUpperCase();
  return crypto.createHmac('sha256', Buffer.from(RECOVERY_KEY, 'base64'))
    .update(normalised, 'utf8')
    .digest('hex')
    .toUpperCase();
}

function newRecoveryCode(): string {
  let letters = '';
  for (let i = 0; i < 12; i += 1) letters += RECOVERY_ALPHABET[crypto.randomInt(RECOVERY_ALPHABET.length)];
  return `${letters.slice(0, 4)}-${letters.slice(4, 8)}-${letters.slice(8, 12)}`;
}

/** Puts the owner into the given state and clears what earlier sign-ins left behind. */
export function seedOwner(state: { enrolled: boolean }): Owner {
  const fromTheRunner = {
    SIGN_IN_E2E_DB: DB,
    SIGN_IN_E2E_PYTHON: PYTHON,
    SIGN_IN_E2E_OWNER: OWNER_EMAIL,
    SIGN_IN_E2E_TOTP_KEY: TOTP_KEY,
    SIGN_IN_E2E_RECOVERY_KEY: RECOVERY_KEY,
  };
  for (const [name, value] of Object.entries(fromTheRunner)) {
    if (!value) throw new Error(`${name} is not set: start this suite with e2e/sign-in/run.sh`);
  }
  ownerPasswordHash ??= hashPassword(OWNER_PASSWORD);

  const secret = state.enrolled ? crypto.randomBytes(20) : null;
  const recoveryCodes = state.enrolled ? Array.from({ length: RECOVERY_CODE_COUNT }, newRecoveryCode) : [];
  const rows = {
    db: DB,
    email: OWNER_EMAIL,
    passwordHash: ownerPasswordHash,
    totpCipher: secret ? encryptAuthenticatorKey(secret) : null,
    recoveryHashes: recoveryCodes.map(hashRecoveryCode),
  };

  const run = runSeedScript(rows);
  if (run.status !== 0) {
    // Never the input: it holds the hashes.
    throw new Error(`seeding the owner failed: ${run.stderr}`);
  }
  return { email: OWNER_EMAIL, password: OWNER_PASSWORD, secret, recoveryCodes };
}

/** Runs seed_owner.py with the given rows. The script decides whether it will touch the database named in them. */
export function runSeedScript(rows: Record<string, unknown>): { status: number | null; stderr: string } {
  const run = spawnSync(PYTHON, [path.join(__dirname, 'seed_owner.py')], {
    input: JSON.stringify(rows),
    encoding: 'utf8',
    timeout: 30_000,
    windowsHide: true,
  });
  return { status: run.error ? null : run.status, stderr: run.error?.message ?? run.stderr };
}

/** The run's database file, as run.sh named it. */
export const RUN_DATABASE = DB;

/**
 * The authenticator code for a key, as an authenticator app shows it now
 * (RFC 6238: HMAC-SHA1, 30 seconds, 6 digits).
 */
export function authenticatorCode(secret: Buffer, at: Date = new Date()): string {
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(at.getTime() / 1000 / TOTP_PERIOD_SECONDS)));
  const hash = crypto.createHmac('sha1', secret).update(counter).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binary = ((hash[offset] & 0x7f) << 24)
    | (hash[offset + 1] << 16)
    | (hash[offset + 2] << 8)
    | hash[offset + 3];
  return String(binary % 1_000_000).padStart(6, '0');
}

/** A six-digit code that is not the right one now, a step earlier or a step later. */
export function wrongAuthenticatorCode(secret: Buffer, at: Date = new Date()): string {
  const near = [-1, 0, 1].map((step) => authenticatorCode(secret, new Date(at.getTime() + step * TOTP_PERIOD_SECONDS * 1000)));
  for (let n = 0; n < 1_000_000; n += 1) {
    const candidate = String(n).padStart(6, '0');
    if (!near.includes(candidate)) return candidate;
  }
  throw new Error('no wrong code exists');
}
