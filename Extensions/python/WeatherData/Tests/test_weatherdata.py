"""Integration tests for the WeatherData sample extension."""

import asyncio
import urllib.error
import urllib.request

import pytest
from beckhoff.tchmi.tests.serverfixture import HmiServerFixture
from conftest import EXTENSION_NAME

pytestmark = pytest.mark.asyncio(loop_scope="session")

WEATHER_API = "https://api.open-meteo.com/v1/forecast?latitude=51.87895&longitude=8.4727&current_weather=true"
# main.py requests the daily forecast without forecast_days, for which Open-Meteo answers 7 days.
FORECAST_DAYS = 7


def is_number(value: object) -> bool:
    return isinstance(value, (int, float)) and not isinstance(value, bool)


def weather_api_reachable() -> bool:
    try:
        with urllib.request.urlopen(WEATHER_API, timeout=10):
            return True
    except (urllib.error.URLError, TimeoutError):
        return False


async def test_extension_is_loaded(hmi_server: HmiServerFixture) -> None:
    extensions = await hmi_server.get_loaded_extensions()

    assert extensions[EXTENSION_NAME]["state"] == "Initialized"


@pytest.mark.parametrize("symbol", ["CurrentWindSpeed", "CurrentTemperature", "CurrentRainfall"])
async def test_current_values_are_numbers(hmi_server: HmiServerFixture, symbol: str) -> None:
    value = await hmi_server.read_symbol(f"{EXTENSION_NAME}.{symbol}")

    assert is_number(value)


async def test_forecast_is_a_list_of_numbers(hmi_server: HmiServerFixture) -> None:
    forecast = await hmi_server.read_symbol(f"{EXTENSION_NAME}.DailyTemperatureForecast")

    assert isinstance(forecast, list)
    assert all(is_number(temperature) for temperature in forecast)


async def test_forecast_is_fetched_from_the_weather_service(hmi_server: HmiServerFixture) -> None:
    if not await asyncio.to_thread(weather_api_reachable):
        pytest.skip(f"{WEATHER_API} is not reachable from this machine")

    # The extension fetches the data in the background right after its start.
    forecast: list[float] = []
    for _ in range(30):
        forecast = await hmi_server.read_symbol(f"{EXTENSION_NAME}.DailyTemperatureForecast")
        if forecast:
            break
        await asyncio.sleep(1)

    assert len(forecast) == FORECAST_DAYS
    assert all(-90 < temperature < 60 for temperature in forecast)
    assert -90 < await hmi_server.read_symbol(f"{EXTENSION_NAME}.CurrentTemperature") < 60
    assert await hmi_server.read_symbol(f"{EXTENSION_NAME}.CurrentWindSpeed") >= 0
