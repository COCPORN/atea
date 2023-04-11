using Microsoft.EntityFrameworkCore;

namespace WeatherPlot;

/// <summary>
/// For simplicity, I am just using SQLite with this, code is
/// obviously virtually the same with SQL Server
/// </summary>
public class WeatherContext : DbContext
{
    public DbSet<WeatherPlot> WeatherPlots { get; set; }

    public string DbPath { get; }

    public WeatherContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "weatherplots.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}

public class WeatherPlot
{
    public int WeatherPlotId { get; set; }

    public string? City { get; set; }

    /// <summary>
    /// Assumes temperature in celcius
    /// </summary>
    public float Temperature { get; set; }

    /// <summary>
    /// Assumes wind speed in meters/sec
    /// </summary>
    public float WindSpeed { get; set; }

    /// <summary>
    /// Assumes cloud cover as a percentage, currently not implemented
    /// </summary>
    public float? CloudCover { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
