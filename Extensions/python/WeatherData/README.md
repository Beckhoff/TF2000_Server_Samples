# About the "WeatherData" sample

The "WeatherData" sample extension provides current weather data for a specific
location using a REST API to query an online weather service.

The location for which to retrieve current weather data is specified by the
latitude and longitude in the URL of the
[Open-Meteo Free Weather API](https://open-meteo.com/) in
[main.py](WeatherData/main.py). Find the location of your choice by
right-clicking on any point in [Google Maps](https://www.google.com/maps) and
copying the coordinates. The default coordinates of this extension specify the
Beckhoff Headquarters Germany in Verl.

## Example requests

1. Get the daily temperature forecast for the next seven days:

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "WeatherData.DailyTemperatureForecast",
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
                "symbol": "WeatherData.DailyTemperatureForecast",
                "readValue": [
                    4.7,
                    4.2,
                    7.5,
                    6.5,
                    6.4,
                    9.6,
                    10.2
                ]
            }
        ]
    }
    ```

    Temperatures will rise soon. 😎

1. Get the current wind speed:

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "WeatherData.CurrentWindSpeed",
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
                "symbol": "WeatherData.CurrentWindSpeed",
                "readValue": 13.2
            }
        ]
    }
    ```

    Not particularly windy in Verl. 🍃

1. Get the current temperature:

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "WeatherData.CurrentTemperature",
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
                "symbol": "WeatherData.CurrentTemperature",
                "readValue": 0.4
            }
        ]
    }
    ```

    It's cold outside. 🥶

1. Get the current rainfall:

    **Request:**

    ```json
    {
        "requestType": "ReadWrite",
        "commands": [
            {
                "symbol": "WeatherData.CurrentRainfall",
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
                "symbol": "WeatherData.CurrentRainfall",
                "readValue": 0
            }
        ]
    }
    ```

    It's always sunny in Verl. 🌞
