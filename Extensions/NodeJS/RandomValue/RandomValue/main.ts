import { ExtensionHost, Context } from '@beckhoff/tchmiextensionapi';
import { Command } from '@beckhoff/tchmiclient';

const CFG_MAX_RANDOM = 'maxRandom';

/**
 * NodeJSRandomValue Extension
 * Simple random value generator with proper error handling and logging
 */
export class NodeJSRandomValue extends ExtensionHost {
    private configValues: Record<string, any> = { maxRandom: 1000 };

    constructor() {
        super();
    }

    /**
     * Initialize the extension
     */
    async init(domain: string, settings: any): Promise<void> {
        try {
            if (settings && settings.maxRandom !== undefined) {
                this.configValues.maxRandom = settings.maxRandom;
            }
            this.logger.info('RandomValue extension initialized');
        } catch (error) {
            this.logger.error(`Failed to initialize RandomValue extension: ${error}`);
            throw error;
        }
    }

    /**
     * Get configuration value
     */
    protected getConfigValue(path: string): any {
        return this.configValues[path];
    }

    /**
     * Localize a message
     */
    protected localize(context: Context, name: string, ...parameters: string[]): string {
        const formatStrings: Record<string, string> = {
            'errorInit': 'Initializing extension "NodeJSRandomValue" failed. Additional information: {0}',
            'errorCallCommand': 'Calling command "{0}" failed! Additional information: {1}'
        };
        
        const formatString = formatStrings[name] || name;
        return this.formatString(formatString, parameters);
    }

    /**
     * Format a string with parameters
     */
    private formatString(template: string, parameters: string[]): string {
        return template.replace(/{(\d+)}/g, (match, index) => {
            return typeof parameters[index] !== 'undefined' ? parameters[index] : match;
        });
    }

    /**
     * Handle symbol requests 
     */
    async onRequest(context: Context, commands: Command[]): Promise<void> {
        for (const command of commands) {
            try {
                if (command.symbol === 'RandomValue') {
                    this.nextRandomValue(command);
                }
            } catch (error) {
                (command as any).extensionResult = 1; // InternalError
                (command as any).resultString = this.localize(context, 'errorCallCommand', command.symbol, String(error));
            }
        }
    }

    /**
     * Validate configuration changes before they are applied
     */
    protected beforeChange(context: Context, path: string, value: any): void {
        if (path === CFG_MAX_RANDOM) {
            if (value === null || value === undefined || typeof value !== 'number' || value < 0) {
                throw new Error('Max random value must not be less than zero.');
            }
        }
    }

    /**
     * Handle configuration changes
     */
    async onConfigChange(context: Context, config: any): Promise<void> {
        // Validate and apply configuration changes
        for (const key in config) {
            this.beforeChange(context, key, config[key]);
            this.configValues[key] = config[key];
        }
    }

    /**
     * Generate a random value
     */
    private nextRandomValue(command: Command): void {
        const maxRandom = this.getConfigValue(CFG_MAX_RANDOM) || 1000;
        
        if (maxRandom < 0) {
            throw new Error(`maxRandom must not be negative, but was ${maxRandom}`);
        }
        
        const randomValue = Math.floor(Math.random() * (maxRandom + 1));
        command.readValue = randomValue;
        (command as any).extensionResult = 0; // Success
        this.logger.debug(`Random value generated: ${randomValue}`);
    }

    /**
     * Cleanup on shutdown
     */
    async shutdown(): Promise<void> {
        this.logger.info('RandomValue extension shutdown');
    }
}

// Create and start the extension only if not in test mode
if (process.env.NODE_ENV !== 'test') {
    const extension = new NodeJSRandomValue();
    extension.run().catch(err => {
        console.error('Extension failed:', err);
        process.exit(1);
    });
}