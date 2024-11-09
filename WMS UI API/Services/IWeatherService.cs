 
namespace WMS_UI_API.Services
{
    public interface IWeatherService
    {
        Task<WeatherForecast> GetWeatherForecast(string cityName, bool isAirQualityNeeded);
    }
}
