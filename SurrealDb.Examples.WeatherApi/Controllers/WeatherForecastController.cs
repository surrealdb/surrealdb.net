using Microsoft.AspNetCore.Mvc;
using SurrealDb.Examples.WeatherApi.Models;
using SurrealDb.Net;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.WeatherApi.Controllers;

/// <summary>
/// Api controller for WeatherForecast.
/// </summary>
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
	private const string Table = "weatherForecast";

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
	[Route("/")]
	public Task<List<WeatherForecast>> GetAll(CancellationToken cancellationToken)
	{
		return _surrealDbClient.Select<WeatherForecast>(Table, cancellationToken);
	}

	/// <summary>
	/// Get a weather forecast by id.
	/// </summary>
	[HttpGet]
	[Route("/:id")]
	public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
	{
		var weatherForecast = await _surrealDbClient.Select<WeatherForecast>(Table, id, cancellationToken);

		if (weatherForecast is null)
			return NotFound();

		return Ok(weatherForecast);
	}

	/// <summary>
	/// Creates a new weather forecast.
	/// </summary>
	[HttpPost]
	[Route("/")]
	public Task<WeatherForecast> Create(CreateWeatherForecast data, CancellationToken cancellationToken)
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
	[Route("/")]
	public Task<WeatherForecast> Update(WeatherForecast data, CancellationToken cancellationToken)
	{
		return _surrealDbClient.Upsert(data, cancellationToken);
	}

	/// <summary>
	/// Patches an existing weather forecast.
	/// </summary>
	[HttpPatch]
	[Route("/:id")]
	public Task<WeatherForecast> Patch(string id, Dictionary<string, object> data, CancellationToken cancellationToken)
	{
		var thing = new Thing(Table, id);

		return _surrealDbClient.Merge<WeatherForecast>(thing, data, cancellationToken);
	}

	/// <summary>
	/// Deletes all weather forecasts.
	/// </summary>
	[HttpDelete]
	[Route("/")]
	public Task DeleteAll(CancellationToken cancellationToken)
	{
		return _surrealDbClient.Delete(Table, cancellationToken);
	}

	/// <summary>
	/// Deletes a weather forecast by id.
	/// </summary>
	[HttpDelete]
	[Route("/:id")]
	public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
	{
		bool success = await _surrealDbClient.Delete(Table, id, cancellationToken);

		if (!success)
			return NotFound();

		return Ok();
	}
}
