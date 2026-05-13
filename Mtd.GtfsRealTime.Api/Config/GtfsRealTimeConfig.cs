using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mtd.GtfsRealTime.Api.Config;

public class GtfsRealTimeConfig
{
	public const string SectionName = "GtfsRealTime";

	[Required]
	[Description("GTFS-RT TripUpdate feed URL")]
	public required Uri TripUpdateFeedUrl { get; init; }

	[Required]
	[Description("GTFS-RT VehiclePosition feed URL")]
	public required Uri VehiclePositionFeedUrl { get; init; }
}
