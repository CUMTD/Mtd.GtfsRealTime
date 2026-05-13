using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using Mtd.GtfsRealTime.Proto.Helpers;
using Mtd.Stopwatch.Core.Entities.Schedule;
using Mtd.Stopwatch.Core.Repositories.Schedule;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Api.Controllers;

[Route("service-alerts")]
[OutputCache(PolicyName = StaticDataCacheProfile.NAME)]
public class ServiceAlertsController : ControllerBase
{
	private readonly IRerouteRepository<IReadOnlyCollection<Reroute>> _rerouteRepository;
	private readonly ILogger<ServiceAlertsController> _logger;

	public ServiceAlertsController(IRerouteRepository<IReadOnlyCollection<Reroute>> rerouteRepository, ILogger<ServiceAlertsController> logger)
	{
		ArgumentNullException.ThrowIfNull(rerouteRepository, nameof(rerouteRepository));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_rerouteRepository = rerouteRepository;
		_logger = logger;
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

		var converted = reroutes.ConvertToFeedMessage();

		return Ok(converted);
	}
}
