/**
 * `npm run start` - the whole stack for local development in one terminal:
 *
 *   [api]  dotnet run --project ../ST.LiquorTNT.Api   (https://localhost:7180, http://localhost:5180)
 *   [web]  vite                                       (http://localhost:5173, proxies /api -> :5180)
 *
 * Output of both is prefixed so it can be told apart; Ctrl+C stops both; if one of them exits, the other
 * is stopped too and this script exits with that code. No extra npm dependency - plain child_process.
 *
 * Overrides (environment variables):
 *   API_PROJECT  path to the API project folder or .csproj (default: ../ST.LiquorTNT.Api, the sibling project in src/)
 *   API_PROFILE  launchSettings profile to use (default: the project's default profile)
 */
import { spawn } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const apiProject = resolve(webRoot, process.env.API_PROJECT ?? '../ST.LiquorTNT.Api');
const isWindows = process.platform === 'win32';

if (!existsSync(apiProject)) {
  console.error(
    `[start] API project not found at ${apiProject}\n[start] Set API_PROJECT to the folder of ST.LiquorTNT.Api.csproj.`,
  );
  process.exit(1);
}

const colours = { api: '\u001b[36m', web: '\u001b[35m', reset: '\u001b[0m' };
const children = new Map();
let shuttingDown = false;

function run(name, command, args, options = {}) {
  // With a shell, an argument with spaces (e.g. "Liquor Application" in the path) must be quoted by hand.
  const quoted = isWindows ? args.map((a) => (/\s/.test(a) ? `"${a}"` : a)) : args;
  const child = spawn(command, quoted, {
    cwd: options.cwd ?? webRoot,
    env: { ...process.env, FORCE_COLOR: '1', ...options.env },
    stdio: ['inherit', 'pipe', 'pipe'],
    // npm is npm.cmd on Windows; a shell resolves either without us caring.
    shell: isWindows,
  });

  const prefix = `${colours[name]}[${name}]${colours.reset} `;
  const forward = (stream, target) => {
    let rest = '';
    stream.on('data', (chunk) => {
      const lines = (rest + chunk.toString()).split(/\r?\n/);
      rest = lines.pop() ?? '';
      for (const line of lines) target.write(prefix + line + '\n');
    });
    stream.on('end', () => {
      if (rest) target.write(prefix + rest + '\n');
    });
  };
  forward(child.stdout, process.stdout);
  forward(child.stderr, process.stderr);

  child.on('error', (error) => {
    console.error(`${prefix}could not start "${command}": ${error.message}`);
    if (command === 'dotnet') console.error(`${prefix}is the .NET 8 SDK installed and on PATH?`);
    children.delete(name);
    shutdown(1);
  });

  child.on('exit', (code, signal) => {
    children.delete(name);
    if (shuttingDown) return;
    console.log(`${prefix}exited (${signal ?? `code ${code ?? 0}`}); stopping the rest.`);
    shutdown(code ?? (signal ? 1 : 0));
  });

  children.set(name, child);
  return child;
}

function shutdown(exitCode) {
  if (shuttingDown) return;
  shuttingDown = true;
  for (const child of children.values()) {
    if (isWindows) {
      // SIGINT does not reach a Windows console process tree; taskkill ends dotnet + Kestrel and node + vite.
      spawn('taskkill', ['/pid', String(child.pid), '/t', '/f'], { stdio: 'ignore', shell: true });
    } else {
      child.kill('SIGINT');
    }
  }
  setTimeout(() => process.exit(exitCode), 500).unref();
}

process.on('SIGINT', () => shutdown(0));
process.on('SIGTERM', () => shutdown(0));

const dotnetArgs = ['run', '--project', apiProject];
if (process.env.API_PROFILE) dotnetArgs.push('--launch-profile', process.env.API_PROFILE);

console.log(`[start] api: dotnet ${dotnetArgs.join(' ')}`);
console.log('[start] web: vite dev server (http://localhost:5173, /api -> http://localhost:5180)');

run('api', 'dotnet', dotnetArgs, {
  env: { ASPNETCORE_ENVIRONMENT: process.env.ASPNETCORE_ENVIRONMENT ?? 'Development' },
});
run('web', 'npm', ['run', 'dev']);
