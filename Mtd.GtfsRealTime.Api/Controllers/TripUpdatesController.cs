using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

using Mtd.GtfsRealTime.Api.Config;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Api.Controllers;

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

	[HttpGet, HttpHead, HttpOptions]
	[Route("")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeedMessage))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> TripUpdates(CancellationToken cancellationToken) => await GetProtoResponseFromDownstreamServer(_gtfsRealTimeConfig.TripUpdateFeedUrl, cancellationToken);
}
