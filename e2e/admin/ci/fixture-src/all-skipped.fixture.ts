import { test } from '@playwright/test';

test.describe('AC-06 all-skipped fixture', () => {
  test.skip('declaration skip', async () => {
    throw new Error('must not run');
  });

  test.fixme('fixme case', async () => {
    throw new Error('must not run');
  });

  test('conditional skip', async () => {
    test.skip(true, 'fixture: conditional skip');
    throw new Error('must not run');
  });
});
