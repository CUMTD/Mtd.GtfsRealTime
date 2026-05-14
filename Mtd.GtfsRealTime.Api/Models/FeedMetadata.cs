namespace Mtd.GtfsRealTime.Api.Models;


/// <summary>
/// Metadata about the vehicle positions feed.
/// </summary>
public record VehiclePositionsMetadata
{
	/// <summary>
	/// All vehicle IDs currently in the feed.
	/// </summary>
	public required string[] VehicleIds { get; init; }
	/// <summary>
	/// The total number of vehicles currently in the feed.
	/// </summary>
	public required int VehicleCount { get; init; }
	/// <summary>
	/// The timestamp of the last update to the vehicle positions feed.
	/// </summary>
	public required DateTimeOffset LastUpdate { get; init; }
}

/// <summary>
/// A breakdown of trip update entries by schedule relationship type.
/// </summary>
public record TripUpdateTypes
{
	/// <summary>
	/// The number of trips currently showing as scheduled in the feed.
	/// </summary>
	public required int Scheduled { get; init; }
	/// <summary>
	/// The number of trips currently showing as unscheduled in the feed.
	/// </summary>
	public required int Unscheduled { get; init; }
	/// <summary>
	/// The number of trips currently showing as canceled in the feed.
	/// </summary>
	public required int Canceled { get; init; }
	/// <summary>
	/// The number of trips currently showing as duplicated in the feed.
	/// </summary>
	public required int Duplicated { get; init; }
}

/// <summary>
/// Metadata about the trip updates feed.
/// </summary>
public record TripUpdatesMetadata
{
	/// <summary>
	/// All trip IDs currently in the feed.
	/// </summary>
	public required string[] TripIds { get; init; }
	/// <summary>
	/// The total number of trips currently in the feed.
	/// </summary>
	public required int TripCount { get; init; }
	/// <summary>
	/// A breakdown of trips by schedule relationship type.
	/// </summary>
	public required TripUpdateTypes UpdateTypes { get; init; }
	/// <summary>
	/// The timestamp of the last update to the trip updates feed.
	/// </summary>
	public required DateTimeOffset LastUpdate { get; init; }
}

/// <summary>
/// Metadata about the service alerts feed.
/// </summary>
public record ServiceAlertMetadata
{
	/// <summary>
	/// The total number of active service alerts in the feed.
	/// </summary>
	public required int ServiceAlertCount { get; init; }
	/// <summary>
	/// Active service alert IDs in the feed.
	/// </summary>
	public required string[] ServiceAlertIds { get; init; }
}

/// <summary>
/// Aggregated metadata for all three GTFS-Realtime feeds.
/// </summary>
public record FeedMetadata
{
	/// <summary>
	/// Metadata for the vehicle positions feed.
	/// </summary>
	public VehiclePositionsMetadata? VehiclePositions { get; init; }
	/// <summary>
	/// Metadata for the trip updates feed.
	/// </summary>
	public TripUpdatesMetadata? TripUpdates { get; init; }
	/// <summary>
	/// Metadata for the service alerts feed.
	/// </summary>
	public ServiceAlertMetadata? ServiceAlerts { get; init; }
}
