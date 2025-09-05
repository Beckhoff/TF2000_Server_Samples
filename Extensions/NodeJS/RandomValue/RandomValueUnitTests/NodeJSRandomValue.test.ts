/// <reference types="mocha" />
/// <reference types="chai" />

import { expect } from 'chai';
import { Context } from '@beckhoff/tchmiextensionapi';
import { Command } from '@beckhoff/tchmiclient';
import { MockedNodeJSRandomValue } from './MockedNodeJSRandomValue.js';

const DEFAULT_DOMAIN = 'NodeJSRandomValue';
const CFG_MAX_RANDOM = 'maxRandom';

describe('NodeJSRandomValue Unit Tests', () => {
    let serverExtension: MockedNodeJSRandomValue;

    before(() => {
        serverExtension = new MockedNodeJSRandomValue(DEFAULT_DOMAIN);
    });

    beforeEach(() => {
        serverExtension.logMessages = [];
    });

    describe('OnRequest - Valid Tests', () => {
        [1, 10, 100].forEach((maxRandom) => {
            it(`should generate random value with maxRandom=${maxRandom}`, async () => {
                serverExtension.maxRandom = maxRandom;

                const context: Context = { domain: DEFAULT_DOMAIN } as any;
                const requestCommand: Command = {
                    symbol: 'RandomValue'
                } as any;

                await serverExtension.onRequest(context, [requestCommand]);

                expect(requestCommand.readValue).to.exist;
                expect(requestCommand.readValue).to.be.a('number');
                expect(requestCommand.readValue).to.be.at.least(0);
                expect(requestCommand.readValue).to.be.at.most(maxRandom);
            });
        });

        it('should generate different random values on multiple calls', async () => {
            serverExtension.maxRandom = 1000;

            const context: Context = { domain: DEFAULT_DOMAIN } as any;
            const values: number[] = [];

            // Generate 10 random values
            for (let i = 0; i < 10; i++) {
                const requestCommand: Command = {
                    symbol: 'RandomValue'
                } as any;

                await serverExtension.onRequest(context, [requestCommand]);
                values.push(requestCommand.readValue as number);
            }

            // Check that we got at least some different values (very unlikely to get 10 identical values)
            const uniqueValues = new Set(values);
            expect(uniqueValues.size).to.be.greaterThan(1);
        });
    });

    describe('OnRequest - Invalid Tests', () => {
        [-1, -42].forEach((maxRandom) => {
            it(`should handle error when maxRandom=${maxRandom}`, async () => {
                serverExtension.maxRandom = maxRandom;

                const context: Context = { domain: DEFAULT_DOMAIN } as any;
                const requestCommand: Command = {
                    symbol: 'RandomValue'
                } as any;

                await serverExtension.onRequest(context, [requestCommand]);

                expect((requestCommand as any).extensionResult).to.equal(1);
                expect((requestCommand as any).resultString).to.include('Calling command "RandomValue" failed!');
            });
        });
    });

    describe('BeforeChange - Valid Tests', () => {
        [0, 1, 10, 100].forEach((maxRandom) => {
            it(`should accept valid maxRandom=${maxRandom}`, () => {
                const context: Context = { domain: DEFAULT_DOMAIN } as any;

                expect(() => {
                    serverExtension.testBeforeChange(context, CFG_MAX_RANDOM, maxRandom);
                }).to.not.throw();
            });
        });
    });

    describe('BeforeChange - Invalid Tests', () => {
        [-1, -42].forEach((maxRandom) => {
            it(`should reject negative maxRandom=${maxRandom}`, () => {
                const context: Context = { domain: DEFAULT_DOMAIN } as any;

                expect(() => {
                    serverExtension.testBeforeChange(context, CFG_MAX_RANDOM, maxRandom);
                }).to.throw('Max random value must not be less than zero.');
            });
        });

        it('should reject null maxRandom', () => {
            const context: Context = { domain: DEFAULT_DOMAIN } as any;

            expect(() => {
                serverExtension.testBeforeChange(context, CFG_MAX_RANDOM, null);
            }).to.throw('Max random value must not be less than zero.');
        });

        it('should reject undefined maxRandom', () => {
            const context: Context = { domain: DEFAULT_DOMAIN } as any;

            expect(() => {
                serverExtension.testBeforeChange(context, CFG_MAX_RANDOM, undefined);
            }).to.throw('Max random value must not be less than zero.');
        });

        it('should reject non-number maxRandom', () => {
            const context: Context = { domain: DEFAULT_DOMAIN } as any;

            expect(() => {
                serverExtension.testBeforeChange(context, CFG_MAX_RANDOM, 'invalid' as any);
            }).to.throw('Max random value must not be less than zero.');
        });
    });

    describe('Multiple Commands', () => {
        it('should handle multiple commands in one request', async () => {
            serverExtension.maxRandom = 100;

            const context: Context = { domain: DEFAULT_DOMAIN } as any;
            const commands: Command[] = [
                { symbol: 'RandomValue' } as any,
                { symbol: 'RandomValue' } as any,
                { symbol: 'RandomValue' } as any
            ];

            await serverExtension.onRequest(context, commands);

            commands.forEach(cmd => {
                expect(cmd.readValue).to.exist;
                expect(cmd.readValue).to.be.a('number');
                expect(cmd.readValue).to.be.at.least(0);
                expect(cmd.readValue).to.be.at.most(100);
            });
        });
    });
});
