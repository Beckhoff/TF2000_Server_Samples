import asyncio
from random import randint
from typing import Any, Dict, List
from beckhoff.tchmi.extensionapi import Command, Context, ExtensionHost


class RandomValue(ExtensionHost):
    def __init__(self) -> None:
        self.domain = ''
        super().__init__()

    async def get_max_random_from_config(self):
        request = Command(f'{self.domain}.Config::maxRandom')
        response = await self.execute([request])
        return response[0].readValue

    # Called when the extension is initialized
    async def init(self, domain: str, settings: Dict[str, Any]) -> None:
        self.domain = domain

    # Called for each symbol request
    async def on_request(self, context: Context, commands: List[Command]) -> None:
        for command in commands:
            if command.symbol == 'RandomValue':
                command.readValue = randint(0, await self.get_max_random_from_config())


if __name__ == '__main__':
    main = RandomValue()
    asyncio.run(main.run())
