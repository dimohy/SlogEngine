using SlogEngine.Server.Models;

namespace SlogEngine.Server.Interfaces;

public interface IWeatherService
{
    IEnumerable<WeatherForecast> GetWeatherForecast();
}
