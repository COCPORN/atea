using System.Text.Json.Serialization;

namespace WeatherPlot.DataFetcherModels
{
    public class MeteoCurrentWeather
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("windspeed")]
        public float WindSpeed { get; set; }

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }
    }

    public class Meteo
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("current_weather")]
        public MeteoCurrentWeather? CurrentWeather { get; set; }
    }
}
