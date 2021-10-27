# About the "ConfigListening" sample

The configuration of this sample extension contains an array of palindromes,
as well as a map that contains anagrams. Definitions:

- A **palindrome** is a word, number, phrase, or other sequence of characters
  which reads the same backward as forward
- An **anagram** is a word or phrase formed by rearranging the letters of a
  different word or phrase, typically using all the original letters exactly once.

The default configuration looks like this:

```json
{
    "palindromes": [
        "kayak",
        "Eva, can I see bees in a cave?"
    ],
    "anagrams": {
        "Madam Curie": "Radium came",
        "dormitory": "dirty room",
        "earth": "heart",
        "tea": "eat"
    }
}
```

The extension uses the "ConfigListener" interface to validate new palindromes
and anagrams before they are added. Changes to existing palindromes and
anagrams are also validated.

The extension raises a `CONFIGURATION_HINT` alarm, if the configuration
contains a palindrome that consists of more than one word. When that palindrome
is removed, the alarm is reset automatically.
Active configuration hints are displayed on the configuration page of the
extension if `TcHmiSrv.Config::SHOW_CONFIGURATION_HINTS` is enabled.

**First steps:**

- [Working with server extensions](../../resources/WorkingWithServerExtensions.md)
- [Interacting with a server extension](../../resources/InteractingWithServerExtensions.md)

## Example requests

> Instead of trying out the following example requests, you can also add,
  change, delete, and rename settings on the configuration page of this
  sample extension.

1. Read the current configuration of the sample extension.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ConfigListening.Config",
                "commandOptions": [ "SendErrorMessage" ],

            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ConfigListening.Config",
                "readValue": {
                    "anagrams": {
                        "Madam Curie": "Radium came",
                        "dormitory": "dirty room",
                        "earth": "heart",
                        "tea": "eat"
                    },
                    "palindromes": [
                        "kayak",
                        "Eva, can I see bees in a cave?"
                    ]
                }
            }
        ]
    }
    ```

1. Remove the second palindrome from the array.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ConfigListening.Config::palindromes[1]",
                "commandOptions": [ "SendErrorMessage", "Delete" ]
            }
        ]
    }
    ```

1. Add a new palindrome to the array.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ConfigListening.Config::palindromes",
                "writeValue": [ "radar" ],
                "commandOptions": [ "SendErrorMessage", "Add" ]
            }
        ]
    }
    ```

1. It is not possible to add invalid palindromes to the array.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ConfigListening.Config::palindromes",
                "writeValue": [ "hello" ],
                "commandOptions": [ "SendErrorMessage", "Add" ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ConfigListening.Config::palindromes",
                "error": {
                    "domain": "TcHmiSrv",
                    "code": 1539,
                    "message": "INVALID_FIELD",
                    "reason": "Change attempt rejected. 'hello' is not palindrome.\r\nResult: HMI_E_INVALID_FIELD"
                }
            }
        ]
    }
    ```

1. Add a new anagram to the map.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ConfigListening.Config::anagrams",
                "writeValue": {
                    "a gentleman": "elegant man"
                },
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

1. It is not possible to add invalid anagrams to the object.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ConfigListening.Config::anagrams",
                "writeValue": {
                    "donkey": "mule"
                },
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ConfigListening.Config::anagrams",
                "error": {
                    "domain": "TcHmiSrv",
                    "code": 1539,
                    "message": "INVALID_FIELD",
                    "reason": "Change attempt rejected. 'mule' is not an anagram of 'donkey'.\r\nResult: HMI_E_INVALID_FIELD"
                }
            }
        ]
    }
    ```

1. Change the key of an existing anagram:

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "Rename",
                "writeValue": {
                    "domain": "ConfigListening",
                    "old": "anagrams::tea",
                    "new": "anagrams::ate"
                },
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

1. It is not possible to break an existing anagram by renaming its key.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "Rename",
                "writeValue": {
                    "domain": "ConfigListening",
                    "old": "anagrams::tea",
                    "new": "anagrams::hello"
                },
                "commandOptions": [ "SendErrorMessage" ]
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "Rename",
                "error": {
                    "domain": "TcHmiSrv",
                    "code": 1539,
                    "message": "INVALID_FIELD",
                    "reason": "Invalid field [Path: 'Rename attempt rejected. 'hello' is not an anagram of 'eat'.\r\nResult: HMI_E_INVALID_FIELD']"
                }
            }
        ]
    }
    ```

1. Check for active configuration hints that were raised by the
   sample extension.

   **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "ListEvents",
                "filter": "domain==\"TcHmiSrv\" && name==\"CONFIGURATION_HINT\" && payload::domain==\"ConfigListening\"",
                "orderBy": "timeReceived DESC"
            }
        ]
    }
    ```

    **Response:**

    ```json
    {
        "commands": [
            {
                "symbol": "ListEvents",
                "readValue": [
                    {
                        "domain": "TcHmiSrv",
                        "localizedString": "At least one palindrome consists of multiple words.",
                        "name": "CONFIGURATION_HINT",
                        "payload": {
                            "name": "strictPalindromeValidationFailed",
                            "domain": "ConfigListening",
                            "params": {},
                            "severity": 2,
                            "id": 1,
                            "timeRaised": "2021-09-14T11:49:22.1322758Z",
                            "timeCleared": null,
                            "timeConfirmed": null,
                            "alarmState": 0,
                            "confirmationState": 2
                        },
                        "payloadType": 1,
                        "timeReceived": "2021-09-13T14:16:21.6136086Z"
                    }
                ],
                "orderBy": "timeReceived DESC",
                "filter": [
                    {
                        "comparator": "==",
                        "path": "domain",
                        "value": "TcHmiSrv"
                    },
                    {
                        "logic": "AND"
                    },
                    {
                        "comparator": "==",
                        "path": "name",
                        "value": "CONFIGURATION_HINT"
                    },
                    {
                        "logic": "AND"
                    },
                    {
                        "comparator": "==",
                        "path": "payload::domain",
                        "value": "ConfigListening"
                    }
                ],
                "commandOptions": [ "PagingHandled" ]
            }
        ]
    }
    ```
