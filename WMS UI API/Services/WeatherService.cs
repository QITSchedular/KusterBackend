 
using System.Text.Json;

namespace WMS_UI_API.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherService> _logger;
        public WeatherService(ILogger<WeatherService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<WeatherForecast> GetWeatherForecast(string cityName, bool isAirQualityNeeded)
        {
            string airQuality = isAirQualityNeeded ? "yes" : "no";
            string apiUri = $"?key={_configuration["AppSettings:WeatherApiKey"]}&q={cityName}&aqi={airQuality}";

            //Logging can be done as a group using Begin Scope
            //This helps in searching particular logs related to a functionality in any log platforms
            using (_logger.BeginScope(new Dictionary<string, object> { { "WeatherService", 1 } }))
            {
                _logger.LogInformation(2001, "Custom part of the URI has set to: {apiUri}", apiUri);
                _logger.LogInformation(2002, "Base URI remains as set in configuration file");
            }

            HttpResponseMessage httpResponseMessage = await _httpClient.GetAsync(apiUri);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.LogTrace(2003, "Request is successful");
                return JsonSerializer.Deserialize<WeatherForecast>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            else
            {
                _logger.LogWarning(2004, "Request is un-successful");
                return null;
            }
        }

    }
}
