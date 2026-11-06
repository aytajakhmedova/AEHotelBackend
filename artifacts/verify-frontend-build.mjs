import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
const front = process.argv[2];
const { build } = await import(pathToFileURL(path.join(front, 'node_modules/vite/dist/node/index.js')));
const files = ['src/api/auth.js', 'src/pages/Admin/Staff/AdminStaff.jsx', 'src/pages/Admin/Users/AdminUsers.jsx'];
const baseline = new Map(await Promise.all(files.map(async file => [
  path.join(front, file).replaceAll('\\', '/'),
  await readFile(new URL('./frontend-before/' + file, import.meta.url), 'utf8')
])));
let baselineFailed = false;
try {
  await build({ root: front, logLevel: 'error', build: { write: false }, plugins: [{
    name: 'verify-original-sources', enforce: 'pre',
    load(id) { return baseline.get(id.replaceAll('\\', '/')); }
  }] });
} catch (error) {
  baselineFailed = true;
  console.log('BASELINE BUILD FAIL:', error.message);
}
console.log('Baseline CSS failure reproduced:', baselineFailed);
await build({ root: front, logLevel: 'warn', build: { write: false, cssMinify: false } });
console.log('CURRENT BUILD WITH CSS MINIFICATION DISABLED: PASS (diagnostic only; config unchanged)');
