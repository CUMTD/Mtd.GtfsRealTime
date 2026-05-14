using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mtd.GtfsRealTime.Api.Config;

/// <summary>
/// Configuration for the upstream GTFS-Realtime feed URLs.
/// </summary>
public class GtfsRealTimeConfig
{
	/// <summary>The configuration section name.</summary>
	public const string SectionName = "GtfsRealTime";

	/// <summary>
	/// The absolute URL of the upstream GTFS-RT Trip Updates feed.
	/// </summary>
	[Required]
	[Description("GTFS-RT TripUpdate feed URL")]
	public required Uri TripUpdateFeedUrl { get; init; }

	/// <summary>
	/// The absolute URL of the upstream GTFS-RT Vehicle Positions feed.
	/// </summary>
	[Required]
	[Description("GTFS-RT VehiclePosition feed URL")]
	public required Uri VehiclePositionFeedUrl { get; init; }
}
