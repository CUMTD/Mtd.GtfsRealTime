using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Mtd.GtfsRealTime.Api.Health;


public sealed class DownstreamHeadHealthCheck : IHealthCheck
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IOptionsMonitor<Config.Health> _healthOptions;

	public const string TRIP_UPDATES_NAME = "downstream-trip-updates";
	public const string VEHICLE_POSITIONS_NAME = "downstream-vehicle-positions";
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

	private static string GetUrlForRegistrationName(string registrationName, Config.Health options) =>
		// Keep this mapping explicit so it’s obvious and safe.
		registrationName switch
		{
			TRIP_UPDATES_NAME => options.TripUpdatesCheckUrl,
			VEHICLE_POSITIONS_NAME => options.VehiclePositionsCheckUrl,
			_ => throw new InvalidOperationException("Unknown health check registration name: " + registrationName)
		};
}
