using Microsoft.AspNetCore.Mvc;
using SurrealDb.Examples.WeatherApi.Models;
using SurrealDb.Net;
using SystemTextJsonPatch;

namespace SurrealDb.Examples.WeatherApi.Controllers;

/// <summary>
/// Api controller for WeatherForecast.
/// </summary>
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    internal const string Table = "weatherForecast";

    private readonly ISurrealDbClient _surrealDbClient;

    /// <summary>
    /// Constructor
    /// </summary>
    public WeatherForecastController(ISurrealDbClient surrealDbClient)
    {
        _surrealDbClient = surrealDbClient;
    }

    /// <summary>
    /// Get all weather forecasts.
    /// </summary>
    [HttpGet]
    public Task<IEnumerable<WeatherForecast>> GetAll(CancellationToken cancellationToken)
    {
        return _surrealDbClient.Select<WeatherForecast>(Table, cancellationToken);
    }

    /// <summary>
    /// Get a weather forecast by id.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var weatherForecast = await _surrealDbClient.Select<WeatherForecast>(
            (Table, id),
            cancellationToken
        );

        if (weatherForecast is null)
            return NotFound();

        return Ok(weatherForecast);
    }

    /// <summary>
    /// Creates a new weather forecast.
    /// </summary>
    [HttpPost]
    public Task<WeatherForecast> Create(
        CreateWeatherForecast data,
        CancellationToken cancellationToken
    )
    {
        var weatherForecast = new WeatherForecast
        {
            Date = data.Date,
            Country = data.Country,
            TemperatureC = data.TemperatureC,
            Summary = data.Summary
        };

        return _surrealDbClient.Create(Table, weatherForecast, cancellationToken);
    }

    /// <summary>
    /// Updates an existing weather forecast.
    /// </summary>
    [HttpPut]
    public Task<WeatherForecast> Update(WeatherForecast data, CancellationToken cancellationToken)
    {
        return _surrealDbClient.Upsert(data, cancellationToken);
    }

    /// <summary>
    /// Patches all weather forecasts.
    /// </summary>
    [HttpPatch]
    public Task<IEnumerable<WeatherForecast>> PatchAll(
        JsonPatchDocument<WeatherForecast> patches,
        CancellationToken cancellationToken
    )
    {
        return _surrealDbClient.Patch(Table, patches, cancellationToken);
    }

    /// <summary>
    /// Patches an existing weather forecast.
    /// </summary>
    [HttpPatch("{id}")]
    public Task<WeatherForecast> Patch(
        string id,
        JsonPatchDocument<WeatherForecast> patches,
        CancellationToken cancellationToken
    )
    {
        return _surrealDbClient.Patch((Table, id), patches, cancellationToken);
    }

    /// <summary>
    /// Deletes all weather forecasts.
    /// </summary>
    [HttpDelete]
    public Task DeleteAll(CancellationToken cancellationToken)
    {
        return _surrealDbClient.Delete(Table, cancellationToken);
    }

    /// <summary>
    /// Deletes a weather forecast by id.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        bool success = await _surrealDbClient.Delete((Table, id), cancellationToken);

        if (!success)
            return NotFound();

        return Ok();
    }
}
