import { FullConfig } from '@playwright/test';
import { execSync } from 'child_process';

async function globalTeardown(config: FullConfig) {
  console.log('\n🧹 [E2E Teardown] Cleaning up...');

  // Kill any remaining server processes started by Playwright's webServer config
  // Playwright should ideally do this, but sometimes processes can linger.
  // This is a fallback to ensure ports are free for next run.
  const ports = [3030, 8030, 8031]; // Frontend, Backend HTTP, Backend HTTPS

  if (process.platform === 'win32') {
    // Windows specific command to find and kill processes by port
    ports.forEach(port => {
      try {
        // Find PID using netstat and then kill it
        const findPidCommand = `netstat -ano | findstr :${port}`;
        const output = execSync(findPidCommand, { encoding: 'utf8' });
        const lines = output.split('\n').filter(line => line.includes(`:${port}`));

        lines.forEach(line => {
          const parts = line.trim().split(/\s+/);
          const pid = parts[parts.length - 1];
          if (pid && !isNaN(parseInt(pid)) && parseInt(pid) > 0) {
            try {
              console.log(`   Killing process ${pid} on port ${port}...`);
              execSync(`taskkill /PID ${pid} /F`, { encoding: 'utf8' });
            } catch (killError: any) {
              // Ignore "process not found" errors as the process might have already ended
              if (!killError.message.includes('not found') && !killError.message.includes('could not be terminated')) {
                console.warn(`   Could not kill process ${pid} on port ${port}: ${killError.message.trim()}`);
              }
            }
          }
        });
      } catch (error: any) {
        // Ignore "no process found" errors as this is normal for cleanup
        if (!error.message.includes('No process found') && !error.status) {
          console.warn(`   Could not find process on port ${port}: ${error.message.trim()}`);
        }
      }
    });
  } else {
    // Linux/macOS specific command
    ports.forEach(port => {
      try {
        const command = `lsof -t -i:${port} | xargs -r kill`;
        execSync(command);
        console.log(`   Killed processes on port ${port}`);
      } catch (error: any) {
        if (!error.message.includes('No such process')) {
          console.warn(`   Could not kill process on port ${port}: ${error.message.trim()}`);
        }
      }
    });
  }

  console.log('✅ [E2E Teardown] Cleanup complete!');
}

export default globalTeardown;
