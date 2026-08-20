/**
 * Centralized logging utility for SkillLedger frontend
 * Replaces direct console.* usage for better control and production safety
 *
 * ESLint note: console.* usage is intentional in this logger implementation
 * This file wraps console methods for controlled logging with environment checks
 */

/* eslint-disable no-console */
type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LogEntry {
  level: LogLevel;
  message: string;
  context?: Record<string, any>;
  timestamp: Date;
}

class Logger {
  private isDevelopment = process.env.NODE_ENV === 'development';
  private isTest = process.env.NODE_ENV === 'test';

  /**
   * Debug-level logging - only in development
   * Use for detailed debugging information
   */
  debug(message: string, context?: Record<string, any>): void {
    if (this.isDevelopment) {
      console.log('[DEBUG]', message, context || '');
    }
  }

  /**
   * Info-level logging - development and production
   * Use for significant events (user actions, state changes)
   */
  info(message: string, context?: Record<string, any>): void {
    if (this.isDevelopment) {
      console.info('[INFO]', message, context || '');
    }
    // In production, could send to analytics/monitoring service
    this.sendToMonitoring('info', message, context);
  }

  /**
   * Warning-level logging - all environments
   * Use for recoverable errors and unexpected conditions
   */
  warn(message: string, context?: Record<string, any>): void {
    // BUG-HIGH-015 FIX: Only log to console in development
    if (this.isDevelopment) {
      console.warn('[WARN]', message, context || '');
    }
    this.sendToMonitoring('warn', message, context);
  }

  /**
   * Error-level logging - all environments
   * Use for errors that affect functionality
   */
  error(message: string, error?: Error | unknown, context?: Record<string, any>): void {
    // BUG-HIGH-015 FIX: Only log to console in development
    if (this.isDevelopment) {
      console.error('[ERROR]', message, error || '', context || '');
    }

    // Extract error details for monitoring
    const errorDetails = error instanceof Error
      ? { message: error.message, stack: error.stack, name: error.name }
      : { raw: error };

    this.sendToMonitoring('error', message, { ...context, error: errorDetails });
  }

  /**
   * Send logs to monitoring service
   */
  private sendToMonitoring(_level: LogLevel, _message: string, _context?: Record<string, any>): void {
    // Sentry disabled for CF Workers compatibility — logs go to console only
  }

  /**
   * Group multiple log entries (useful for debugging complex operations)
   */
  group(label: string, callback: () => void): void {
    if (this.isDevelopment) {
      console.group(label);
      callback();
      console.groupEnd();
    } else {
      callback();
    }
  }

  /**
   * Performance timing utility
   */
  time(label: string): void {
    if (this.isDevelopment) {
      console.time(label);
    }
  }

  timeEnd(label: string): void {
    if (this.isDevelopment) {
      console.timeEnd(label);
    }
  }

  /**
   * Table logging for arrays/objects (development only)
   */
  table(data: any[]): void {
    if (this.isDevelopment) {
      console.table(data);
    }
  }
}

// Export singleton instance
export const logger = new Logger();

// Export for testing
export { Logger };
