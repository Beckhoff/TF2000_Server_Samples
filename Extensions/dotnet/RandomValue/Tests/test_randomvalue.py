"""Integration tests for the CSharpRandomValue sample extension."""

from collections.abc import AsyncGenerator

import pytest
import pytest_asyncio
from beckhoff.tchmi.tests.serverfixture import Command, HmiError, HmiServerFixture
from conftest import EXTENSION_NAME, active_session

pytestmark = pytest.mark.asyncio(loop_scope="session")

RANDOM_VALUE = f"{EXTENSION_NAME}.RandomValue"
MAX_RANDOM = f"{EXTENSION_NAME}.Config::maxRandom"
DEFAULT_MAX_RANDOM = 1000


@pytest_asyncio.fixture(loop_scope="session")
async def max_random(hmi_server: HmiServerFixture) -> AsyncGenerator[None, None]:
    """Restore maxRandom after a test that changes it."""
    original = await hmi_server.read_symbol(MAX_RANDOM)
    yield
    await hmi_server.write_symbol(MAX_RANDOM, original)


async def read_random_values(hmi_server: HmiServerFixture, count: int) -> list[int]:
    return [await hmi_server.read_symbol(RANDOM_VALUE) for _ in range(count)]


async def test_extension_is_loaded(hmi_server: HmiServerFixture) -> None:
    extensions = await hmi_server.get_loaded_extensions()

    assert extensions[EXTENSION_NAME]["state"] == "Initialized"


async def test_max_random_defaults_to_1000(hmi_server: HmiServerFixture) -> None:
    assert await hmi_server.read_symbol(MAX_RANDOM) == DEFAULT_MAX_RANDOM


async def test_random_values_are_below_max_random(hmi_server: HmiServerFixture) -> None:
    values = await read_random_values(hmi_server, 20)

    assert all(isinstance(value, int) and 0 <= value < DEFAULT_MAX_RANDOM for value in values)


@pytest.mark.usefixtures("max_random")
async def test_changed_max_random_limits_the_values(hmi_server: HmiServerFixture) -> None:
    await hmi_server.write_symbol(MAX_RANDOM, 3)

    values = await read_random_values(hmi_server, 50)

    assert set(values) <= {0, 1, 2}


@pytest.mark.usefixtures("max_random")
async def test_negative_max_random_is_rejected(hmi_server: HmiServerFixture) -> None:
    command = Command(MAX_RANDOM)
    command.writeValue = -1
    # Without SendErrorMessage the server drops the error and the request appears to succeed.
    command.commandOptions = ["SendErrorMessage"]

    with pytest.raises(HmiError):
        await active_session(hmi_server).request(command)
    assert await hmi_server.read_symbol(MAX_RANDOM) == DEFAULT_MAX_RANDOM
