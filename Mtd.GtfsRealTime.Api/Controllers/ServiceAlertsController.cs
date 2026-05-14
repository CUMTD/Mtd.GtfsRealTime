using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

using Mtd.GtfsRealTime.Proto.Helpers;
using Mtd.Stopwatch.Core.Entities.Schedule;
using Mtd.Stopwatch.Core.Repositories.Schedule;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Api.Controllers;

/// <summary>
/// Provides the GTFS-Realtime <em>Service Alerts</em> feed, built from active reroutes stored
/// in the Stopwatch database. Responses are cached for
/// <see cref="StaticDataCacheProfile.DURATION_IN_SECONDS"/> seconds.
/// </summary>
[ApiController]
[Route("service-alerts")]
[OutputCache(PolicyName = StaticDataCacheProfile.NAME)]
public class ServiceAlertsController : ControllerBase
{
	private readonly IRerouteRepository<IReadOnlyCollection<Reroute>> _rerouteRepository;
	private readonly ILogger<ServiceAlertsController> _logger;

	/// <summary>
	/// Initializes a new instance of <see cref="ServiceAlertsController"/>.
	/// </summary>
	public ServiceAlertsController(IRerouteRepository<IReadOnlyCollection<Reroute>> rerouteRepository, ILogger<ServiceAlertsController> logger)
	{
		ArgumentNullException.ThrowIfNull(rerouteRepository, nameof(rerouteRepository));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_rerouteRepository = rerouteRepository;
		_logger = logger;
	}

	/// <summary>
	/// Returns a GTFS-RT Service Alerts feed constructed from all currently active reroutes.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	[HttpGet, HttpHead, HttpOptions]
	[Route("")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeedMessage))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
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
