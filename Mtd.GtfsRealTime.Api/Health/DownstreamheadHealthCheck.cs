using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Mtd.GtfsRealTime.Api.Health;


/// <summary>
/// Health check that sends an HTTP HEAD request to a configured downstream URL and reports
/// healthy only when the response status is <c>200 OK</c>.
/// </summary>
/// <remarks>
/// A single instance of this class is registered under two different names
/// (<see cref="TRIP_UPDATES_NAME"/> and <see cref="VEHICLE_POSITIONS_NAME"/>). The
/// <see cref="CheckHealthAsync"/> method uses the registration name to resolve the correct
/// URL from <see cref="Config.Health"/> options.
/// </remarks>
public sealed class DownstreamHeadHealthCheck : IHealthCheck
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IOptionsMonitor<Config.Health> _healthOptions;

	/// <summary>Registration name for the trip-updates downstream check.</summary>
	public const string TRIP_UPDATES_NAME = "downstream-trip-updates";

	/// <summary>Registration name for the vehicle-positions downstream check.</summary>
	public const string VEHICLE_POSITIONS_NAME = "downstream-vehicle-positions";

	/// <summary>Named <see cref="HttpClient"/> used for HEAD requests (2-second timeout).</summary>
	public const string CLIENT_NAME = "downstream-health";

	public DownstreamHeadHealthCheck(
		IHttpClientFactory httpClientFactory,
		IOptionsMonitor<Config.Health> healthOptions)
	{
		ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
		ArgumentNullException.ThrowIfNull(healthOptions?.CurrentValue, nameof(healthOptions));

		_httpClientFactory = httpClientFactory;
		_healthOptions = healthOptions;
	}

	/// <inheritdoc/>
	/// <remarks>
	/// Resolves the target URL from the registration name, sends an HTTP HEAD request, and
	/// returns <see cref="HealthCheckResult.Healthy"/> only when the response is <c>200 OK</c>.
	/// </remarks>
	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		var url = GetUrlForRegistrationName(context.Registration.Name, _healthOptions.CurrentValue);

		if (string.IsNullOrWhiteSpace(url))
		{
			return HealthCheckResult.Unhealthy($"No URL configured for {context.Registration.Name}.");
		}

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			return HealthCheckResult.Unhealthy($"Invalid URL configured for {context.Registration.Name}: '{url}'.");
		}

		try
		{
			var client = _httpClientFactory.CreateClient(CLIENT_NAME);

			using var request = new HttpRequestMessage(HttpMethod.Head, uri);
			using var response = await client.SendAsync(request, cancellationToken);

			// You said these should return 200. If you truly want 200-only, keep this strict.
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				return HealthCheckResult.Healthy($"{context.Registration.Name} HEAD ok (200).");
			}

			return HealthCheckResult.Unhealthy($"{context.Registration.Name} HEAD returned {(int)response.StatusCode}.");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy($"{context.Registration.Name} HEAD failed.", ex);
		}
	}

	/// <summary>
	/// Maps a health-check registration name to its configured downstream URL.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <paramref name="registrationName"/> is not a known registration name.
	/// </exception>
	private static string GetUrlForRegistrationName(string registrationName, Config.Health options) =>
		// Keep this mapping explicit so it’s obvious and safe.
		registrationName switch
		{
			TRIP_UPDATES_NAME => options.TripUpdatesCheckUrl,
			VEHICLE_POSITIONS_NAME => options.VehiclePositionsCheckUrl,
			_ => throw new InvalidOperationException("Unknown health check registration name: " + registrationName)
		};
}
