using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
namespace MiniApp.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    //private readonly ILogger<WeatherForecastController> _logger;

    private readonly IConfiguration _config;

    private readonly SqlServerContext _db;

    /*
    public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration config, SqlServerContext db)
    {
        _logger = logger;
        _config = config;
        _db = db;
    }
    */
    public WeatherForecastController(IConfiguration config, SqlServerContext db)
    {
        _config = config;
        _db = db;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}

