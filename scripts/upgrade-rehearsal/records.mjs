// Upgrade rehearsal (T09 spec §2.3): the small fixed data set, written and read back through
// the engine's own HTTP API.
//   node records.mjs seed <baseUrl> <stateFile>              writes the records, keeps their ids
//   node records.mjs read <baseUrl> <stateFile> <outFile>     reads each record back, one per kind
//   node records.mjs write <baseUrl> <stateFile>             one write per kind of record, on the
//                                                            upgraded schema, each read back
// The two reads (before and after the new migrations) must be byte-identical. The writes show
// the previous release can still write every kind of record on the new schema.
// Throwaway data on a throwaway CI database: the names are example.test addresses.
import { readFileSync, writeFileSync } from 'node:fs';

const [mode, base, stateFile, outFile] = process.argv.slice(2);
if (!['seed', 'read', 'write'].includes(mode) || !base || !stateFile || (mode === 'read' && !outFile)) {
  console.error('usage: node records.mjs seed|write <baseUrl> <stateFile> | read <baseUrl> <stateFile> <outFile>');
  process.exit(2);
}

async function call(method, path, { token, body } = {}) {
  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token}`;
  const res = await fetch(base + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await res.text();
  if (res.status < 200 || res.status > 299) {
    throw new Error(`${method} ${path}: HTTP ${res.status}`);
  }
  return text ? JSON.parse(text) : null;
}

const tokenFor = async (apiKey) => (await call('POST', '/auth/token', { body: { apiKey } })).accessToken;

if (mode === 'seed') {
  const seller = await call('POST', '/auth/register', { body: { displayName: 'rehearsal.seller@example.test' } });
  const buyer = await call('POST', '/auth/register', { body: { displayName: 'rehearsal.buyer@example.test' } });
  const sellerToken = await tokenFor(seller.apiKey);
  const artifact = await call('POST', '/artifacts', {
    token: sellerToken,
    body: {
      entities: [{
        name: 'Rehearsal sedan',
        description: 'Fixed record for the upgrade rehearsal',
        properties: [{ name: 'mileage', type: 'number', value: '45000' }]
      }],
      intent: 'sell',
      values: [{ amount: 22000.0, currency: 'USD' }],
      locations: ['Boston']
    }
  });
  const negotiation = await call('POST', '/negotiations', {
    token: sellerToken,
    body: { artifactId: artifact.id, counterpartyParticipantId: buyer.sub, callerIntent: 'sell', counterpartyIntent: 'buy' }
  });
  const offer = await call('POST', `/negotiations/${negotiation.id}/offers`, {
    token: sellerToken,
    body: { amount: 22000.0, currency: 'USD', terms: 'Rehearsal terms, as sent' }
  });
  const strategy = await call('POST', '/strategies', {
    token: sellerToken,
    body: { name: 'Rehearsal strategy', strategyBody: 'Hold at 21000 or above.' }
  });
  writeFileSync(stateFile, JSON.stringify({
    sellerApiKey: seller.apiKey,
    artifactId: artifact.id,
    negotiationId: negotiation.id,
    offerId: offer.id,
    strategyId: strategy.id
  }));
  console.log('seeded: 2 participants, 1 artifact, 1 negotiation, 1 offer, 1 strategy');
} else if (mode === 'write') {
  // One write per kind, through the previous release's API on the upgraded schema.
  const s = JSON.parse(readFileSync(stateFile, 'utf8'));
  const token = await tokenFor(s.sellerApiKey);
  const participant = await call('POST', '/auth/register', { body: { displayName: 'rehearsal.after@example.test' } });
  await call('PATCH', '/profile', { token, body: { displayName: 'rehearsal.seller.renamed@example.test' } });
  const artifact = await call('POST', '/artifacts', {
    token,
    body: {
      entities: [{ name: 'Rehearsal van', description: 'Written after the upgrade', properties: [] }],
      intent: 'sell',
      values: [{ amount: 9000.0, currency: 'USD' }],
      locations: ['Boston']
    }
  });
  const negotiation = await call('POST', '/negotiations', {
    token,
    body: { artifactId: artifact.id, counterpartyParticipantId: participant.sub, callerIntent: 'sell', counterpartyIntent: 'buy' }
  });
  const offer = await call('POST', `/negotiations/${negotiation.id}/offers`, {
    token,
    body: { amount: 8500.0, currency: 'USD', terms: 'Written after the upgrade' }
  });
  const strategy = await call('POST', '/strategies', { token, body: { name: 'After', strategyBody: 'Written after the upgrade.' } });
  await call('PUT', `/strategies/${s.strategyId}`, { token, body: { name: 'Rehearsal strategy', strategyBody: 'Changed after the upgrade.' } });
  const buyerToken = await tokenFor(participant.apiKey);
  await call('POST', `/offers/${offer.id}/decline`, { token: buyerToken });
  // Read each write back.
  const profile = await call('GET', '/profile', { token });
  if (profile.displayName !== 'rehearsal.seller.renamed@example.test') throw new Error('the profile write did not hold');
  await call('GET', `/artifacts/${artifact.id}`, { token });
  await call('GET', `/negotiations/${negotiation.id}`, { token });
  const declined = await call('GET', `/offers/${offer.id}`, { token });
  if (String(declined.status).toLowerCase() !== 'declined') throw new Error('the offer decline did not hold');
  await call('GET', `/strategies/${strategy.id}`, { token });
  const changed = await call('GET', `/strategies/${s.strategyId}`, { token });
  if (changed.strategyBody !== 'Changed after the upgrade.') throw new Error('the strategy update did not hold');
  console.log('written and read back: participant, profile, artifact, negotiation, offer and its decline, strategy and its update');
} else {
  const s = JSON.parse(readFileSync(stateFile, 'utf8'));
  // The token is fresh each time, so it is not part of what is compared.
  const token = await tokenFor(s.sellerApiKey);
  const records = {
    profile: await call('GET', '/profile', { token }),
    artifact: await call('GET', `/artifacts/${s.artifactId}`, { token }),
    negotiation: await call('GET', `/negotiations/${s.negotiationId}`, { token }),
    offer: await call('GET', `/offers/${s.offerId}`, { token }),
    strategy: await call('GET', `/strategies/${s.strategyId}`, { token })
  };
  writeFileSync(outFile, JSON.stringify(records, null, 2) + '\n');
  console.log(`read back: ${Object.keys(records).join(', ')}`);
}
