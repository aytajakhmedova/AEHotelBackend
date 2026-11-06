import { spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
const existing = ['AEHotel.sln', 'AEHotel.API/Controllers/UsersController.cs', 'AEHotel.API/Controllers/AdminController.cs',
  'AEHotel.Service/Services/AdminService.cs', 'AEHotel.Service/Services/Interfaces/IAdminService.cs', 'AEHotel.Service/DependencyInjection.cs'];
const added = ['AEHotel.Service/Security/UserManagementPolicy.cs', 'AEHotel.SecurityTests/AEHotel.SecurityTests.csproj',
  'AEHotel.SecurityTests/SecurityApp.cs', 'AEHotel.SecurityTests/UserManagementTests.cs',
  'AEHotel.SecurityTests/frontend-security.mjs', 'AEHotel.SecurityTests/README.md'];
const front = ['src/api/auth.js', 'src/pages/Admin/Staff/AdminStaff.jsx', 'src/pages/Admin/Users/AdminUsers.jsx'];
const pairs = existing.map(file => ['backend/' + file, 'artifacts/security-before/' + file, file]);
pairs.push(...added.map(file => ['backend/' + file, '/dev/null', file]));
for (const file of front) {
  const actual = path.join(process.argv[2], file);
  if (!readFileSync(actual).equals(readFileSync('artifacts/frontend-edit/' + file))) throw new Error('Frontend changed: ' + file);
  pairs.push(['frontend/' + file, 'artifacts/frontend-before/' + file, 'artifacts/frontend-edit/' + file]);
}
let patch = '', additions = 0, deletions = 0;
const summary = [];
for (const [label, before, after] of pairs) {
  const result = spawnSync('git', ['-c', 'core.autocrlf=false', 'diff', '--no-index', '--no-color', '--', before, after], { encoding: 'utf8' });
  if (![0, 1].includes(result.status)) throw new Error(result.stderr);
  const lines = result.stdout.split('\n');
  const plus = lines.filter(line => line.startsWith('+') && !line.startsWith('+++')).length;
  const minus = lines.filter(line => line.startsWith('-') && !line.startsWith('---')).length;
  additions += plus; deletions += minus;
  summary.push({ file: label, additions: plus, deletions: minus });
  patch += lines.map(line => line.startsWith('diff --git ') ? `diff --git a/${label} b/${label}`
    : line.startsWith('--- ') ? before === '/dev/null' ? '--- /dev/null' : `--- a/${label}`
    : line.startsWith('+++ ') ? `+++ b/${label}` : line).join('\n');
}
writeFileSync('artifacts/security-fix.diff', patch);
writeFileSync('artifacts/security-diff-summary.json', JSON.stringify({ files: summary, additions, deletions }, null, 2));
console.log(JSON.stringify({ files: summary, additions, deletions }, null, 2));
