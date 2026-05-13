using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using Mtd.GtfsRealTime.Proto.Helpers;
using Mtd.Stopwatch.Core.Entities.Schedule;
using Mtd.Stopwatch.Core.Repositories.Schedule;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Api.Controllers;

[Route("service-alerts")]
[OutputCache(PolicyName = StaticDataCacheProfile.NAME)]
public class ServiceAlertsController : ProtoController<FeedMessage>
{
	private readonly IRerouteRepository<IReadOnlyCollection<Reroute>> _rerouteRepository;

	public ServiceAlertsController(IRerouteRepository<IReadOnlyCollection<Reroute>> rerouteRepository, HttpClient httpClient, ILogger<ServiceAlertsController> logger) : base(FeedMessage.Parser, httpClient, logger)
	{
		ArgumentNullException.ThrowIfNull(rerouteRepository, nameof(rerouteRepository));
		_rerouteRepository = rerouteRepository;
	}

	[HttpGet, HttpHead, HttpOptions]
	[Route("")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeedMessage))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> ServiceAlerts(CancellationToken cancellationToken)
	{
		IReadOnlyCollection<Reroute> reroutes;
		try
		{
			reroutes = await _rerouteRepository.GetAllActiveWithRoutesAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch reroutes from DB");
			return Problem(title: "Error", statusCode: StatusCodes.Status500InternalServerError, detail: "Failed to fetch reroutes.");
		}

		var converted = RerouteConverter.ConvertToFeedMessage(reroutes);

		return Ok(converted);
	}
}
