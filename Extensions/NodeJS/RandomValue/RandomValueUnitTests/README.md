# NodeJS RandomValue Unit Tests

This project contains unit tests for the NodeJS RandomValue TwinCAT HMI Server Extension.

## Setup

Install dependencies:

```bash
npm install
```

## Running Tests

Run all tests:

```bash
npm test
```

Run tests in watch mode:

```bash
npm run test:watch
```

Run tests with coverage:

```bash
npm run test:coverage
```

## Test Structure

- `NodeJSRandomValue.test.ts` - Main test file with test cases
- `MockedNodeJSRandomValue.ts` - Mocked version of the extension for testing
- `.mocharc.json` - Mocha configuration

## Test Cases

### OnRequest Tests

- Validates random value generation with various maxRandom values
- Tests error handling for invalid configurations
- Verifies multiple commands are handled correctly

### BeforeChange Tests

- Validates configuration changes before they are applied
- Tests rejection of invalid values (negative, null, undefined, non-number)
- Ensures valid configurations are accepted
