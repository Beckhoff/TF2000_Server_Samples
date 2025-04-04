import asyncio
import aiohttp
from typing import Any, Dict, List, Optional
from beckhoff.tchmi.extensionapi import Command, Context, ExtensionHost


class WeatherData(ExtensionHost):
    def __init__(self) -> None:
        self.domain = ''
        self.apply_task: Optional[asyncio.Task] = None
        self.daily_temperature_forecast: list[float] = []
        self.current_wind_speed = 0.0
        self.current_temperature = 0.0
        self.current_rainfall = 0.0
        super().__init__()

    async def fetch_weather_data(self):
        url = 'https://api.open-meteo.com/v1/forecast?latitude=51.87895&longitude=8.4727&daily=temperature_2m_max&current_weather=true&timezone=Europe%2FBerlin'
        async with aiohttp.ClientSession() as session:
            async with session.get(url) as response:
                return await response.json()

    async def apply_weather_data(self):
        while True:
            weather_data = await self.fetch_weather_data()
            self.daily_temperature_forecast = weather_data['daily']['temperature_2m_max']
            self.current_wind_speed = weather_data['current_weather']['windspeed']
            self.current_temperature = weather_data['current_weather']['temperature']
            self.current_rainfall = weather_data['current_weather'].get('rain', 0.0)
            await asyncio.sleep(300)

    async def init(self, domain: str, settings: Dict[str, Any]) -> None:
        self.domain = domain
        self.apply_task = asyncio.get_running_loop().create_task(self.apply_weather_data())
        print(f'Initialized with domain: {domain}')

    async def on_request(self, context: Context, commands: List[Command]) -> None:
        for command in commands:
            if command.symbol == 'DailyTemperatureForecast':
                command.readValue = self.daily_temperature_forecast
            elif command.symbol == 'CurrentWindSpeed':
                command.readValue = self.current_wind_speed
            elif command.symbol == 'CurrentTemperature':
                command.readValue = self.current_temperature
            elif command.symbol == 'CurrentRainfall':
                command.readValue = self.current_rainfall

    async def shutdown(self) -> None:
        if self.apply_task:
            self.apply_task.cancel()

            try:
                await self.apply_task
            except asyncio.CancelledError:
                pass


if __name__ == '__main__':
    main = WeatherData()
    asyncio.run(main.run())
