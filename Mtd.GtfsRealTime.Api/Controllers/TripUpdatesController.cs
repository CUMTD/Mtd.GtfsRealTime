using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

using Mtd.GtfsRealTime.Api.Config;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Api.Controllers;

/// <summary>
/// Provides the GTFS-Realtime <em>Trip Updates</em> feed by proxying the configured
/// upstream feed server. Responses are cached for
/// <see cref="RealTimeDataCacheProfile.DURATION_IN_SECONDS"/> seconds.
/// </summary>
[Route("trip-updates")]
[OutputCache(PolicyName = RealTimeDataCacheProfile.NAME)]
public class TripUpdatesController : ProtoController<FeedMessage>
{
	private readonly GtfsRealTimeConfig _gtfsRealTimeConfig;

	public TripUpdatesController(IOptions<GtfsRealTimeConfig> gtfsRealTimeConfig, HttpClient httpClient, ILogger<TripUpdatesController> logger) : base(FeedMessage.Parser, httpClient, logger)
	{
		ArgumentNullException.ThrowIfNull(gtfsRealTimeConfig?.Value, nameof(gtfsRealTimeConfig));
		_gtfsRealTimeConfig = gtfsRealTimeConfig.Value;
	}

	/// <summary>
	/// Returns the current GTFS-RT Trip Updates feed, proxied from the configured upstream server.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	[HttpGet, HttpHead, HttpOptions]
	[Route("")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeedMessage))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> TripUpdates(CancellationToken cancellationToken) => await GetProtoResponseFromDownstreamServer(_gtfsRealTimeConfig.TripUpdateFeedUrl, cancellationToken);
}
