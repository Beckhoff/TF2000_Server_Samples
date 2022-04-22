# About the "LetsEncrypt" sample

The "LetsEncrypt" sample is a server extension that generates a TLS
certificate for the hmi server with [Let's Encrypt](https://letsencrypt.org/).

## Let's Encrypt

Let's Encrypt provides an API to generate TLS certificates for a given domain. The client has to complete a challenge on the device, on which the HMI-Server runs, to ensure the control over it. The certificate can be generated with two different APIs. To test Let's Encrypt and the server extension it's possible to use the `Staging Environment (https://letsencrypt.org/docs/staging-environment/)`.

## Requirements

The HMI server must be reachable about a public domain and the endpoints `http://0.0.0.0:80` and `https://0.0.0.0:443` must be configured.

## Example requests

1. Add the public domain of the device you want to generate a certificate for.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::domain",
                "writeValue": "example.com"
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::domain",
                "readValue": "example.com"
            }
        ]
    }
    ````

1. Add email address to Let's Encrypt to get notified if certificate expires.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::contacts",
                "writeValue": [
                    "example@domain.com"
                ]
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::contacts",
                "readValue": [
                    "example@domain.com"
                ]
            }
        ]
    }
    ````

1. Set [staging environment](https://letsencrypt.org/docs/staging-environment/) as current api.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::api",
                "writeValue": 1
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::api",
                "readValue": 1
            }
        ]
    }
    ````

1. Set [acme-v02](https://acme-v02.api.letsencrypt.org/directory) as current api.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::api",
                "writeValue": 0
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::api",
                "readValue": 0
            }
        ]
    }
    ````

1. Set certificate information.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::certificateInformation",
                "writeValue": {
                    "countryName": "country",
                    "state": "state",
                    "locality": "locality",
                    "organization": "organization",
                    "organizationUnit": "organizationUnit"
                }
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::data::certificateInformation",
                "readValue": {
                    "countryName": "country",
                    "state": "state",
                    "locality": "locality",
                    "organization": "organization",
                    "organizationUnit": "organizationUnit"
                }
            }
        ]
    }
    ````

1. Start certificate generation.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::generateCertificate",
                "writeValue": true
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::generateCertificate",
                "readValue": true
            }
        ]
    }
    ````

1. Define interval of certificate generation.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::interval",
                "writeValue": "P30D"
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::interval",
                "readValue": "P30D"
            }
        ]
    }
    ````

1. Define interval of certificate generation if [staging environment](https://letsencrypt.org/docs/staging-environment/) is used.

    **Request:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::intervalStaging",
                "writeValue": "P30D"
            }
        ]
    }
    ````

    **Response:**

    ````json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "LetsEncrypt.Config::intervalStaging",
                "readValue": "P30D"
            }
        ]
    }
    ````

## State information returned by LetsEncrypt extension

1. **Example response of `LetsEncrypt.Diagnostics`**

    ````json
    {
        "symbol": "LetsEncrypt.Diagnostics",
        "readValue": {
            "currentCertificate": {
                "valid": true,
                "validTo": "2022-06-11T16:24:42Z",
                "validFrom": "2022-03-13T16:24:43Z"
            },
            "nextCertificateGeneration": "2022-03-15T16:24:43Z"
        }
    }
    ````
