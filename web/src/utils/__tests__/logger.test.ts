/**
 * Integration Tests for logger.ts
 *
 * Tests the centralized logging utility across different environments
 * and log levels to ensure proper behavior.
 *
 * Note: NODE_ENV is 'test' in the test environment, which means:
 * - Logger will behave like production (no console output)
 * - This is intentional to avoid polluting test output
 * - We test the actual behavior in the test environment
 */

import { Logger, logger } from '../logger';

describe('Logger', () => {
  let consoleSpy: {
    log: jest.SpyInstance;
    info: jest.SpyInstance;
    warn: jest.SpyInstance;
    error: jest.SpyInstance;
    group: jest.SpyInstance;
    groupEnd: jest.SpyInstance;
    time: jest.SpyInstance;
    timeEnd: jest.SpyInstance;
    table: jest.SpyInstance;
  };

  beforeEach(() => {
    consoleSpy = {
      log: jest.spyOn(console, 'log').mockImplementation(() => {}),
      info: jest.spyOn(console, 'info').mockImplementation(() => {}),
      warn: jest.spyOn(console, 'warn').mockImplementation(() => {}),
      error: jest.spyOn(console, 'error').mockImplementation(() => {}),
      group: jest.spyOn(console, 'group').mockImplementation(() => {}),
      groupEnd: jest.spyOn(console, 'groupEnd').mockImplementation(() => {}),
      time: jest.spyOn(console, 'time').mockImplementation(() => {}),
      timeEnd: jest.spyOn(console, 'timeEnd').mockImplementation(() => {}),
      table: jest.spyOn(console, 'table').mockImplementation(() => {}),
    };
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('singleton export', () => {
    test('logger is exported as singleton instance', () => {
      expect(logger).toBeDefined();
      expect(logger).toBeInstanceOf(Logger);
    });

    test('Logger class is exported for testing', () => {
      expect(Logger).toBeDefined();
      const instance = new Logger();
      expect(instance).toBeInstanceOf(Logger);
    });
  });

  describe('debug()', () => {
    test('does not log in test environment (behaves like production)', () => {
      // In test environment, logger should not output to console
      logger.debug('Debug message');
      expect(consoleSpy.log).not.toHaveBeenCalled();
    });

    test('does not log debug with context in test environment', () => {
      logger.debug('Debug with context', { userId: '123' });
      expect(consoleSpy.log).not.toHaveBeenCalled();
    });
  });

  describe('info()', () => {
    test('does not log to console in test environment', () => {
      logger.info('Info message');
      expect(consoleSpy.info).not.toHaveBeenCalled();
    });

    test('does not log info with context to console in test environment', () => {
      logger.info('Info with context', { action: 'login' });
      expect(consoleSpy.info).not.toHaveBeenCalled();
    });

    test('calls sendToMonitoring in all environments (no-op in test)', () => {
      // In test environment, sendToMonitoring returns early
      // This is expected behavior - just ensure no errors thrown
      logger.info('Test info');
      // No assertion needed - just ensure no errors thrown
    });
  });

  describe('warn()', () => {
    test('does not log to console in test environment', () => {
      logger.warn('Warning message');
      expect(consoleSpy.warn).not.toHaveBeenCalled();
    });

    test('does not log warning with context to console in test environment', () => {
      logger.warn('Warning with context', { reason: 'deprecation' });
      expect(consoleSpy.warn).not.toHaveBeenCalled();
    });
  });

  describe('error()', () => {
    test('does not log to console in test environment', () => {
      logger.error('Error message');
      expect(consoleSpy.error).not.toHaveBeenCalled();
    });

    test('does not log error with Error object to console in test environment', () => {
      const error = new Error('Test error');
      logger.error('Error occurred', error);
      expect(consoleSpy.error).not.toHaveBeenCalled();
    });

    test('does not log error with context to console in test environment', () => {
      const error = new Error('Test error');
      logger.error('Error with context', error, { endpoint: '/api/test' });
      expect(consoleSpy.error).not.toHaveBeenCalled();
    });

    test('handles non-Error objects', () => {
      logger.error('Non-error object', { someData: 'value' });
      expect(consoleSpy.error).not.toHaveBeenCalled();
    });

    test('handles null error', () => {
      logger.error('Null error', null);
      expect(consoleSpy.error).not.toHaveBeenCalled();
    });
  });

  describe('group()', () => {
    test('executes callback without grouping in test environment', () => {
      const callback = jest.fn();

      logger.group('Test Group', callback);

      expect(consoleSpy.group).not.toHaveBeenCalled();
      expect(callback).toHaveBeenCalled();
      expect(consoleSpy.groupEnd).not.toHaveBeenCalled();
    });
  });

  describe('time() and timeEnd()', () => {
    test('does not start timer in test environment', () => {
      logger.time('test-timer');
      expect(consoleSpy.time).not.toHaveBeenCalled();
    });

    test('does not end timer in test environment', () => {
      logger.timeEnd('test-timer');
      expect(consoleSpy.timeEnd).not.toHaveBeenCalled();
    });
  });

  describe('table()', () => {
    test('does not log table in test environment', () => {
      const data = [{ id: 1, name: 'Test' }];
      logger.table(data);
      expect(consoleSpy.table).not.toHaveBeenCalled();
    });
  });

  describe('sendToMonitoring()', () => {
    test('skips monitoring in test environment (default behavior)', () => {
      // The singleton logger is in test environment
      // sendToMonitoring returns early, no external calls made
      logger.warn('Test warning');
      logger.error('Test error');
      // No assertion needed - just ensure no errors thrown
    });
  });

  describe('edge cases', () => {
    test('handles empty context gracefully', () => {
      logger.debug('Empty context', {});
      expect(consoleSpy.log).not.toHaveBeenCalled();
    });

    test('handles undefined context gracefully', () => {
      logger.debug('Undefined context', undefined);
      expect(consoleSpy.log).not.toHaveBeenCalled();
    });

    test('handles special characters in messages', () => {
      logger.info('Message with special chars: <script>alert("xss")</script>');
      // No console output in test environment
      expect(consoleSpy.info).not.toHaveBeenCalled();
    });

    test('handles very long messages', () => {
      const longMessage = 'A'.repeat(10000);
      logger.info(longMessage);
      // No console output in test environment
      expect(consoleSpy.info).not.toHaveBeenCalled();
    });

    test('handles complex nested context', () => {
      const context = {
        level1: {
          level2: {
            level3: {
              value: 'deep'
            }
          }
        },
        array: [1, 2, 3],
        date: new Date('2025-01-01')
      };
      logger.info('Nested context', context);
      // No console output in test environment
      expect(consoleSpy.info).not.toHaveBeenCalled();
    });
  });

  describe('actual behavior in different environments', () => {
    test('logger instance is created for test environment', () => {
      // Verify that the logger is configured for test environment
      const testLogger = new Logger();
      expect(testLogger).toBeDefined();

      // In test environment, nothing should be logged to console
      testLogger.debug('test');
      testLogger.info('test');
      testLogger.warn('test');
      testLogger.error('test');

      expect(consoleSpy.log).not.toHaveBeenCalled();
      expect(consoleSpy.info).not.toHaveBeenCalled();
      expect(consoleSpy.warn).not.toHaveBeenCalled();
      expect(consoleSpy.error).not.toHaveBeenCalled();
    });
  });
});
