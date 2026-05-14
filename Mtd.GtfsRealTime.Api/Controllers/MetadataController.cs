using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

using Mtd.GtfsRealTime.Api.Config;
using Mtd.GtfsRealTime.Api.Models;
using Mtd.Stopwatch.Core.Entities.Schedule;
using Mtd.Stopwatch.Core.Repositories.Schedule;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Api.Controllers;

/// <summary>
/// Provides metadata about the three GTFS-RT feeds.
/// This endpoint always returns JSON regardless of the <c>AllowJson</c> configuration.
/// </summary>
[Route("metadata")]
[OutputCache(PolicyName = RealTimeDataCacheProfile.NAME)]
[Produces("application/json")]
public class MetadataController : ProtoController<FeedMessage>
{
	private readonly IRerouteRepository<IReadOnlyCollection<Reroute>> _rerouteRepository;
	private readonly GtfsRealTimeConfig _gtfsRealTimeConfig;

	/// <summary>
	/// Initializes a new instance of <see cref="MetadataController"/>.
	/// </summary>
	public MetadataController(IRerouteRepository<IReadOnlyCollection<Reroute>> rerouteRepository, IOptions<GtfsRealTimeConfig> gtfsRealTimeConfig, HttpClient httpClient, ILogger<TripUpdatesController> logger) : base(FeedMessage.Parser, httpClient, logger)
	{
		ArgumentNullException.ThrowIfNull(rerouteRepository, nameof(rerouteRepository));
		ArgumentNullException.ThrowIfNull(gtfsRealTimeConfig?.Value, nameof(gtfsRealTimeConfig));
		_rerouteRepository = rerouteRepository;
		_gtfsRealTimeConfig = gtfsRealTimeConfig.Value;
	}

	/// <summary>
	/// Returns metadata about the GTFS-RT feeds.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	[HttpGet]
	[Route("")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeedMetadata))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
	public async Task<IActionResult> FeedMetadata(CancellationToken cancellationToken)
	{
		var tripUpdates = await FetchTripUpdatesMetadataAsync(cancellationToken);
		var vehiclePositions = await FetchVehiclePositionsMetadataAsync(cancellationToken);
		var serviceAlerts = await FetchServiceAlertsMetadataAsync(cancellationToken);

		var metadata = new FeedMetadata
		{
			TripUpdates = tripUpdates,
			VehiclePositions = vehiclePositions,
			ServiceAlerts = serviceAlerts,
		};

		var json = JsonSerializer.Serialize(metadata);
		return Content(json, "application/json");
	}

	/// <summary>
	/// Fetches and extracts metadata from the trip updates feed.
	/// Returns <see langword="null"/> if the feed is unavailable or cannot be parsed.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	private async Task<TripUpdatesMetadata?> FetchTripUpdatesMetadataAsync(CancellationToken cancellationToken)
	{
		var (feed, _) = await FetchAndParseFromDownstreamServer(_gtfsRealTimeConfig.TripUpdateFeedUrl, cancellationToken);
		if (feed is null)
		{
			return null;
		}

		return new TripUpdatesMetadata
		{
			TripIds = [.. feed.Entity.Select(e => e.TripUpdate.Trip.TripId)],
			TripCount = feed.Entity.Count,
			UpdateTypes = new TripUpdateTypes
			{
				Scheduled = feed.Entity.Count(e => e.TripUpdate.Trip.ScheduleRelationship == TripDescriptor.Types.ScheduleRelationship.Scheduled),
				Unscheduled = feed.Entity.Count(e => e.TripUpdate.Trip.ScheduleRelationship == TripDescriptor.Types.ScheduleRelationship.Unscheduled),
				Canceled = feed.Entity.Count(e => e.TripUpdate.Trip.ScheduleRelationship == TripDescriptor.Types.ScheduleRelationship.Canceled),
				Duplicated = feed.Entity.Count(e => e.TripUpdate.Trip.ScheduleRelationship == TripDescriptor.Types.ScheduleRelationship.Duplicated),
			},
			LastUpdate = DateTimeOffset.FromUnixTimeSeconds((long)feed.Header.Timestamp).ToLocalTime(),
		};
	}

	/// <summary>
	/// Fetches and extracts metadata from the vehicle positions feed.
	/// Returns <see langword="null"/> if the feed is unavailable or cannot be parsed.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	private async Task<VehiclePositionsMetadata?> FetchVehiclePositionsMetadataAsync(CancellationToken cancellationToken)
	{
		var (feed, _) = await FetchAndParseFromDownstreamServer(_gtfsRealTimeConfig.VehiclePositionFeedUrl, cancellationToken);
		if (feed is null)
		{
			return null;
		}

		return new VehiclePositionsMetadata
		{
			VehicleIds = [.. feed.Entity.Select(e => e.Vehicle.Vehicle.Id)],
			VehicleCount = feed.Entity.Count,
			LastUpdate = DateTimeOffset.FromUnixTimeSeconds((long)feed.Header.Timestamp).ToLocalTime(),
		};
	}

	/// <summary>
	/// Fetches active reroutes from the repository and extracts service alert metadata.
	/// Returns <see langword="null"/> if the repository is unavailable.
	/// </summary>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	private async Task<ServiceAlertMetadata?> FetchServiceAlertsMetadataAsync(CancellationToken cancellationToken)
	{
		try
		{
			var reroutes = await _rerouteRepository.GetAllActiveWithRoutesAsync(cancellationToken);
			return new ServiceAlertMetadata
			{
				ServiceAlertCount = reroutes.Count,
				ServiceAlertIds = [.. reroutes.Select(r => r.Id.ToString())],
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching reroutes from repository");
			return null;
		}
	}
}
