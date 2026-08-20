#!/usr/bin/env node
/**
 * Kill processes on specific ports before starting E2E tests
 * Works cross-platform (Windows, Linux, macOS)
 */

const { execSync } = require('child_process');
const os = require('os');

const PORTS = [8030, 8031, 3030];

function killPort(port) {
  const platform = os.platform();
  
  try {
    if (platform === 'win32') {
      // Windows
      const findCmd = `netstat -ano | findstr :${port}`;
      const output = execSync(findCmd, { encoding: 'utf8' });
      
      const lines = output.split('\n').filter(line => line.trim());
      const pids = new Set();
      
      lines.forEach(line => {
        const parts = line.trim().split(/\s+/);
        const pid = parts[parts.length - 1];
        if (pid && !isNaN(pid) && pid !== '0') {
          pids.add(pid);
        }
      });
      
      pids.forEach(pid => {
        try {
          execSync(`taskkill /F /PID ${pid}`, { stdio: 'ignore' });
          console.log(`✅ Killed process ${pid} on port ${port}`);
        } catch (e) {
          // Process might have already died
        }
      });
      
    } else {
      // Unix-like (Linux, macOS)
      const findCmd = `lsof -ti:${port}`;
      const pids = execSync(findCmd, { encoding: 'utf8' }).trim().split('\n');
      
      pids.forEach(pid => {
        if (pid) {
          try {
            execSync(`kill -9 ${pid}`, { stdio: 'ignore' });
            console.log(`✅ Killed process ${pid} on port ${port}`);
          } catch (e) {
            // Process might have already died
          }
        }
      });
    }
  } catch (error) {
    // No process on this port, which is fine
    console.log(`ℹ️  No process found on port ${port}`);
  }
}

console.log('🧹 Cleaning up ports for E2E tests...\n');

PORTS.forEach(port => {
  console.log(`Checking port ${port}...`);
  killPort(port);
});

console.log('\n✅ Port cleanup complete!\n');


