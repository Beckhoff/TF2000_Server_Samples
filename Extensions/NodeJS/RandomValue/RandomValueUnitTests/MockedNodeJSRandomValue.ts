import { Context } from '@beckhoff/tchmiextensionapi';
import { NodeJSRandomValue } from '../RandomValue/main';

/**
 * Mocked version of NodeJSRandomValue for unit testing
 */
export class MockedNodeJSRandomValue extends NodeJSRandomValue {
    private testDomain: string;
    public maxRandom: number = 10;
    public logMessages: Array<{ level: string; message: string }> = [];

    constructor(domain: string) {
        super();
        if (!domain) {
            throw new Error('domain must not be empty');
        }
        this.testDomain = domain;
        this.initMockLogger();
    }

    /**
     * Override getConfigValue to return test values
     */
    protected getConfigValue(path: string): any {
        switch (path) {
            case 'maxRandom':
                return this.maxRandom;
            default:
                throw new Error(`Unknown path: ${path}`);
        }
    }

    /**
     * Override localize to return formatted strings for testing
     */
    protected localize(context: Context, name: string, ...parameters: string[]): string {
        if (!context) {
            throw new Error('context must not be null');
        }
        const contextDomain = (context as any).domain;
        if (contextDomain && contextDomain !== this.testDomain) {
            throw new Error(`Expected domain ${this.testDomain}, but got ${contextDomain}`);
        }

        const formatStrings: Record<string, string> = {
            'errorInit': 'Initializing extension "NodeJSRandomValue" failed. Additional information: {0}',
            'errorCallCommand': 'Calling command "{0}" failed! Additional information: {1}'
        };

        const formatString = formatStrings[name];
        if (!formatString) {
            throw new Error(`Unknown name: ${name}`);
        }

        return formatString.replace(/{(\d+)}/g, (match, index) => {
            return typeof parameters[index] !== 'undefined' ? parameters[index] : match;
        });
    }

    /**
     * Initialize mock logger
     */
    private initMockLogger() {
        (this as any).logger = {
            info: (message: string) => {
                this.logMessages.push({ level: 'info', message });
            },
            error: (message: string) => {
                this.logMessages.push({ level: 'error', message });
                throw new Error(message);
            },
            warn: (message: string) => {
                this.logMessages.push({ level: 'warn', message });
            },
            debug: (message: string) => {
                this.logMessages.push({ level: 'debug', message });
            }
        };
    }

    /**
     * Expose beforeChange for testing
     */
    public testBeforeChange(context: Context, path: string, value: any): void {
        this.beforeChange(context, path, value);
    }
}
