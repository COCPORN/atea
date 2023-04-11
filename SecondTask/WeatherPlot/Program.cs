using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using WeatherPlot;
using WeatherPlot.Options;

var builder = WebApplication.CreateBuilder(args);

// Consider using something like Hangfire for this, using PeriodicTimer
// was introduced in .NET 6 so we're using that for now
builder.Services.AddHostedService<DataFetcher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("datafetcher", c => c.BaseAddress = new Uri("https://api.open-meteo.com"));

builder.Services.AddOptions<DataFetcherOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        configuration.GetSection("DataFetcher").Bind(options);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Get the minimum temperature for all available cities

// NOTE: This is not how I would implement this, as in, if there was a real requirement
//       for this kind of data in real time, I would store the minimal values separately
//       instead of querying the whole dataset using something that is suitable for that
//       kind of task, like an Orleans grain
app.MapGet("/mintempmaxwind", (IOptions<DataFetcherOptions> options) =>
{
    using var db = new WeatherContext();

    // Group the entries by city
    var entriesGroupedByCity = db.WeatherPlots.GroupBy(x => x.City);

    // Find ordered entries (this can probably be made way more effective, see above)
    var orderedEntries = entriesGroupedByCity.Select(g => new
    {
        City = g.Key,
        Country = options.Value.Cities[g.Key!].Country,
        MinTemperature = new
        {
            Temperature = g.OrderBy(t => t.Temperature).First().Temperature,
            Timestamp = g.OrderBy(t => t.Temperature).First().Timestamp,
        },
        HighestWindSpeed = new
        {
            WindSpeed = g.OrderBy(t => t.WindSpeed).First().WindSpeed,
            Timestamp = g.OrderBy(t => t.WindSpeed).First().Timestamp,
        }
    });

    return orderedEntries.ToList();
})
.WithName("GetWeatherForecast");

app.MapGet("/last2hours", () =>
{

}).WithName("GetLast2Hours");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}