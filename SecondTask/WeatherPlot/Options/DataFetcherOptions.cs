namespace WeatherPlot.Options;

public class Location
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string City { get; set; }

    public string Country { get; set; }
}

public class DataFetcherOptions
{
    public Dictionary<string, Location> Cities { get; set; }
}

