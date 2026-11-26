import { spawnSync } from 'node:child_process';
import path from 'node:path';

/**
 * Made-up participants, artifacts, negotiations and offers for the list and
 * detail pages, written straight into the run's database by
 * seed_entities.py (which refuses any other database). Every call starts
 * from the same rows, so a test that empties a table can put them back.
 *
 * What is there after a seed:
 *   - 205 participants (Alex Rivera, Blair Chen, Casey Novak who is
 *     suspended, Dana Okafor, Eli Moreau, and "Seed participant 001" to
 *     "200", every tenth suspended), and Robin Hart, soft-deleted;
 *   - 204 artifacts (Red bicycle, Oak desk, Vintage camera, Garden tools,
 *     "Seed artifact 001" to "200"), and Broken lamp, soft-deleted;
 *   - 29 negotiations, Open, Closed and Expired, one soft-deleted;
 *   - 1,234 offers in five statuses (none Withdrawn), one soft-deleted.
 */

const DB = process.env.SIGN_IN_E2E_DB ?? '';
const PYTHON = process.env.SIGN_IN_E2E_PYTHON ?? '';

export type Seeded = {
  participants: Record<string, { id: string; sub: string }>;
  artifacts: Record<string, string>;
  negotiations: Record<'bicycle' | 'desk' | 'camera' | 'tools' | 'second-bicycle' | 'deleted', string>;
  offers: Record<'terms' | 'accepted' | 'superseded' | 'deleted', string>;
  counts: { participants: number; offers: number };
};

type Table = 'Participants' | 'Artifacts' | 'Negotiations' | 'Offers';

export function seedEntities(options: { empty?: Table[] } = {}): Seeded {
  if (!DB || !PYTHON) throw new Error('SIGN_IN_E2E_DB and SIGN_IN_E2E_PYTHON are not set: start the suite with e2e/console/run.sh');
  const run = runEntitySeedScript({ db: DB, empty: options.empty ?? [] });
  if (run.status !== 0) throw new Error(`seeding the entities failed: ${run.stderr}`);
  return JSON.parse(run.stdout) as Seeded;
}

/** Runs seed_entities.py with the given request. The script decides whether it will touch the database named in it. */
export function runEntitySeedScript(request: Record<string, unknown>): { status: number | null; stdout: string; stderr: string } {
  const run = spawnSync(PYTHON, [path.join(__dirname, 'seed_entities.py')], {
    input: JSON.stringify(request),
    encoding: 'utf8',
    timeout: 60_000,
  });
  return { status: run.status, stdout: run.stdout, stderr: run.stderr };
}
