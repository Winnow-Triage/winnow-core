export interface LogEntry {
    timestamp: string;
    level: 'info' | 'warn' | 'error';
    message: string;
}

export interface RecorderContext {
    userAgent: string;
    platform: string;
    language: string;
    url: string;
    resolution: string;
    logs: LogEntry[];
}

const LOG_BUFFER_SIZE = 20;
const logBuffer: LogEntry[] = [];

export function startRecording() {
    // 1. Monkey-patch console.error and console.warn
    const originalError = console.error;
    const originalWarn = console.warn;


    console.error = (...args: any[]) => {
        addToBuffer('error', args);
        originalError.apply(console, args);
    };

    console.warn = (...args: any[]) => {
        addToBuffer('warn', args);
        originalWarn.apply(console, args);
    };

    // Optional: Capture info logs too if useful, but requirements said error/warn
    // console.info = (...args: any[]) => {
    //     addToBuffer('info', args);
    //     originalInfo.apply(console, args);
    // };
}

function addToBuffer(level: 'info' | 'warn' | 'error', args: any[]) {
    try {
        const message = args.map(arg => {
            if (typeof arg === 'object') {
                try {
                    return JSON.stringify(arg);
                } catch (e) {
                    return String(arg);
                }
            }
            return String(arg);
        }).join(' ');

        logBuffer.push({
            timestamp: new Date().toISOString(),
            level,
            message
        });

        if (logBuffer.length > LOG_BUFFER_SIZE) {
            logBuffer.shift();
        }
    } catch (e) {
        // Fail silently during recording to avoid infinite loops if logging fails
    }
}

export function getContext(): RecorderContext {
    return {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language,
        url: window.location.href,
        resolution: `${window.innerWidth}x${window.innerHeight}`,
        logs: [...logBuffer]
    };
}
