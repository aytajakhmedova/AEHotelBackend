// Usage: node frontend-security.mjs <frontend-root> <playwright-core-entry>
// All API requests are intercepted. No production users or API mutations are used.
import assert from 'node:assert/strict';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const [front, playwrightEntry] = process.argv.slice(2);
const { createServer } = await import(pathToFileURL(path.join(front, 'node_modules/vite/dist/node/index.js')));
const { chromium } = await import(pathToFileURL(path.resolve(playwrightEntry)));
const server = await createServer({ root: front, server: { host: '127.0.0.1', port: 5198, strictPort: true } });
await server.listen();
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const base = 'http://127.0.0.1:5198';
const staff = [
  { id: 'super', name: 'Protected', surname: 'Super', email: 'super@example.test', role: 'SuperAdmin', isActive: true },
  { id: 'ordinary', name: 'Ordinary', surname: 'Admin', email: 'ordinary@example.test', role: 'Admin', isActive: true },
];
const users = staff.map((member) => ({ ...member, roles: member.id === 'super' ? ['Admin', 'SuperAdmin'] : [member.role] }));
let passed = 0;
const check = (label) => { passed++; console.log(`PASS ${label}`); };

async function session(role, cachedRole = role) {
  const context = await browser.newContext();
  const writes = [];
  await context.route('**/*', async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.pathname.startsWith('/api/')) {
      if (request.method() !== 'GET') {
        writes.push({ path: url.pathname, method: request.method() });
        return route.fulfill({ status: 403, contentType: 'application/json', body: JSON.stringify({ message: 'Test forbids writes' }) });
      }
      const payload = url.pathname === '/api/staff' ? staff : url.pathname === '/api/users' ? users : [];
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(payload) });
    }
    if (url.origin === base) return route.continue();
    return route.abort();
  });
  if (role) {
    await context.addInitScript(({ role, cachedRole }) => {
      const claims = { sub: 'actor', exp: Math.floor(Date.now() / 1000) + 3600,
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'actor@example.test' };
      localStorage.setItem('authToken', 'test.' + btoa(JSON.stringify(claims)).replace(/=+$/, '') + '.fixture');
      localStorage.setItem('user', JSON.stringify({ name: 'Test actor', email: 'actor@example.test', roles: [cachedRole] }));
      localStorage.setItem('authExpiresAt', new Date(Date.now() + 3600000).toISOString());
    }, { role, cachedRole });
  }
  return { context, page: await context.newPage(), writes };
}

try {
  for (const cachedRole of ['Admin', 'SuperAdmin']) {
    const { context, page, writes } = await session('Admin', cachedRole);
    await page.goto(base + '/admin/isciler');
    const protectedRow = page.getByRole('row').filter({ hasText: 'super@example.test' });
    await protectedRow.waitFor();
    assert.equal(await protectedRow.getByRole('button').count(), 0);
    const ordinaryRow = page.getByRole('row').filter({ hasText: 'ordinary@example.test' });
    assert.equal(await ordinaryRow.getByRole('button', { name: 'Redaktə' }).count(), 1);
    assert.equal(await ordinaryRow.getByRole('button', { name: 'Sil', exact: true }).count(), 1);
    check(`Admin staff buttons; cached role=${cachedRole}`);
    await ordinaryRow.getByRole('button', { name: 'Redaktə' }).click();
    assert.equal(await page.locator('form option[value="SuperAdmin"]').count(), 0);
    check(`Admin cannot choose SuperAdmin; cached role=${cachedRole}`);
    await page.goto(base + '/admin/users');
    const userRow = page.getByRole('row').filter({ hasText: 'super@example.test' });
    await userRow.waitFor();
    assert.equal(await userRow.locator('select').count(), 0);
    assert.match(await userRow.innerText(), /SuperAdmin/);
    assert.equal(await page.getByRole('row').filter({ hasText: 'ordinary@example.test' }).locator('select').count(), 1);
    assert.equal(writes.length, 0);
    check(`Direct /admin/users protects multi-role SuperAdmin; cached role=${cachedRole}`);
    await context.close();
  }

  const { context, page } = await session('SuperAdmin', 'Admin');
  await page.goto(base + '/admin/isciler');
  const protectedRow = page.getByRole('row').filter({ hasText: 'super@example.test' });
  await protectedRow.waitFor();
  assert.equal(await protectedRow.getByRole('button', { name: 'Sil', exact: true }).count(), 1);
  await protectedRow.getByRole('button', { name: 'Redaktə' }).click();
  assert.equal(await page.locator('form select').first().inputValue(), 'SuperAdmin');
  assert.equal(await page.locator('form option[value="SuperAdmin"]').count(), 1);
  check('SuperAdmin retains staff controls and current role');
  await page.goto(base + '/admin/users');
  const superUser = page.getByRole('row').filter({ hasText: 'super@example.test' });
  await superUser.waitFor();
  assert.equal(await superUser.locator('select').inputValue(), 'SuperAdmin');
  check('SuperAdmin retains user role controls');

  const authChecks = await page.evaluate(async () => {
    const auth = await import('/src/api/auth.js');
    const original = localStorage.getItem('authToken');
    localStorage.setItem('authToken', 'malformed');
    const malformedDenied = !auth.hasRole('SuperAdmin');
    const expired = { role: 'SuperAdmin', exp: Math.floor(Date.now() / 1000) - 10 };
    localStorage.setItem('authToken', 'test.' + btoa(JSON.stringify(expired)) + '.fixture');
    const expiredDenied = !auth.hasRole('SuperAdmin');
    localStorage.setItem('authToken', original);
    return { malformedDenied, expiredDenied };
  });
  assert.deepEqual(authChecks, { malformedDenied: true, expiredDenied: true });
  check('Malformed and expired tokens fail closed despite cached role');
  await context.close();

  for (const role of ['HotelManager', 'Receptionist']) {
    const { context, page } = await session(role, 'SuperAdmin');
    await page.goto(base + '/admin/users');
    await page.waitForURL((url) => url.pathname !== '/admin/users');
    assert.equal(await page.getByRole('row').filter({ hasText: 'super@example.test' }).count(), 0);
    check(`${role} direct route is denied despite forged cached role`);
    await context.close();
  }

  const anonymous = await session(null);
  await anonymous.page.goto(base + '/admin/isciler');
  await anonymous.page.waitForURL((url) => url.pathname === '/login');
  check('Anonymous direct staff route requires login');
  await anonymous.context.close();
  console.log(`${passed} frontend browser checks passed.`);
} finally {
  await browser.close();
  await server.close();
}
