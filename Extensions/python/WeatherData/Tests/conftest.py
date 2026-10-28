"""Session fixtures: one HMI server with the WeatherData extension for all tests."""

import asyncio
import os
import secrets
from collections.abc import AsyncGenerator
from pathlib import Path
from typing import Any

import pytest_asyncio
from beckhoff.tchmi.tests.serverfixture import Command, HmiServerFixture, Session

EXTENSION_NAME = "WeatherData"
TESTS_DIR = Path(__file__).resolve().parent
EXTENSION_PATH = os.environ.get("TCHMI_EXTENSION_PATH") or str(TESTS_DIR.parent / EXTENSION_NAME)

# The server creates the extension's virtual environment and installs its requirements.txt on
# first start, which can take a few minutes.
INIT_TIMEOUT = float(os.environ.get("TCHMI_EXTENSION_INIT_TIMEOUT", "300"))


def active_session(tf: HmiServerFixture) -> Session:
    """Return the current session; open_session() replaces it, so never cache it."""
    if tf.session is None:
        raise RuntimeError("Session is not initialized")
    return tf.session


async def wait_for_domain(tf: HmiServerFixture, domain: str, timeout: float) -> dict[str, Any]:
    """Poll the domain until the server reports it initialized."""
    info: Any = None
    deadline = asyncio.get_running_loop().time() + timeout
    while asyncio.get_running_loop().time() < deadline:
        info = await active_session(tf).request(Command(f"ListDomains::{domain}"))
        if isinstance(info, dict) and info.get("state") == "Initialized":
            return info
        await asyncio.sleep(1)
    raise TimeoutError(f"Domain {domain} not initialized after {timeout:.0f} s, last state: {info}")


@pytest_asyncio.fixture(scope="session", loop_scope="session")
async def hmi_server() -> AsyncGenerator[HmiServerFixture, None]:
    """Start a throwaway HMI server and load the WeatherData extension into it."""
    # The server instance only lives for this test session, so any password will do.
    password = os.environ.get("HMI_PASSWORD") or secrets.token_urlsafe(16)
    async with HmiServerFixture(working_dir=str(TESTS_DIR), password=password) as tf:
        assert await tf.start_hmi_server(), tf.startup_error
        await tf.register_extension(EXTENSION_PATH, name=EXTENSION_NAME)
        await wait_for_domain(tf, EXTENSION_NAME, INIT_TIMEOUT)
        yield tf
