// Upgrade rehearsal (T09 spec §2.3): the small fixed data set, written and read back through
// the engine's own HTTP API.
//   node records.mjs seed <baseUrl> <stateFile>              writes the records, keeps their ids
//   node records.mjs read <baseUrl> <stateFile> <outFile>     reads each record back, one per kind
// The two reads (before and after the new migrations) must be byte-identical.
// Throwaway data on a throwaway CI database: the names are example.test addresses.
import { readFileSync, writeFileSync } from 'node:fs';

const [mode, base, stateFile, outFile] = process.argv.slice(2);
if (!['seed', 'read'].includes(mode) || !base || !stateFile || (mode === 'read' && !outFile)) {
  console.error('usage: node records.mjs seed <baseUrl> <stateFile> | read <baseUrl> <stateFile> <outFile>');
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
