using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WeatherPlot.DataFetcherModels;
using WeatherPlot.Options;

namespace WeatherPlot
{
    public class DataFetcher : BackgroundService
    {
        private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));
        private readonly HttpClient _httpClient;
        private readonly DataFetcherOptions _options;
        private readonly ILogger _logger;

        public DataFetcher(
            IHttpClientFactory clientFactory,
            ILogger<DataFetcher> logger,
            IOptions<DataFetcherOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _httpClient = clientFactory.CreateClient("datafetcher");
        }

        public async Task FetchWeatherData()
        {
            _logger.LogInformation($"Fetching data from {_options.Cities.Count} cities");

            // To speed up the fetching of the data, do a fan out
            var fetchTasks = new List<(Task<HttpResponseMessage>, string)>();

            foreach (var city in _options.Cities)
            {
                // Get current weather
                var fetchTask = _httpClient.GetAsync(FormattableString.Invariant($"/v1/forecast?latitude={city.Value.Latitude}&longitude={city.Value.Longitude}&current_weather=true"));
                fetchTasks.Add((fetchTask, city.Key));
            }
            try
            {
                await Task.WhenAll(fetchTasks.Select(x => x.Item1));
            }
            catch (Exception)
            {
                // Manually iterate to find faulted tasks
            }

            using var db = new WeatherContext();

            // Iterate the results and create an EF changeset that can be stored in a single go
            bool reportedError = false;
            foreach (var fetchTask in fetchTasks)
            {
                // It is OK to access `fetchTask.Result` here as the task has been awaited
                if (fetchTask.Item1.IsFaulted == true
                    || fetchTask.Item1.Result.IsSuccessStatusCode == false)
                {
                    if (reportedError == false)
                    {
                        // Logging this as "information", because failing to poll weather
                        // data is not really an error, services go down
                        _logger.LogInformation("Failed to fetch data");
                        reportedError = true;
                    }
                    continue;
                }

                // This looks clunky, but Parse can throw. This is expected, you cannot trust
                // external services, it swallows intentionally
                try
                {
                    var nextPlot = await Parse(fetchTask.Item1.Result, fetchTask.Item2);
                    db.Add(nextPlot);
                }
                catch { }
            }
            await db.SaveChangesAsync();
        }

        private async Task<WeatherPlot> Parse(HttpResponseMessage result, string city)
        {
            var content = await result.Content.ReadAsStringAsync();
            var meteoData = JsonSerializer.Deserialize<Meteo>(content);

            try
            {
                return new WeatherPlot
                {
                    City = city,
                    Temperature = meteoData!.CurrentWeather!.Temperature,
                    Timestamp = meteoData.CurrentWeather.Time,
                    WindSpeed = meteoData.CurrentWeather.WindSpeed
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected format of return data for {City}", city);
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                await FetchWeatherData();
            }
        }
    }
}
