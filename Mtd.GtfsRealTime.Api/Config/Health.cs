using System.ComponentModel.DataAnnotations;

namespace Mtd.GtfsRealTime.Api.Config;

/// <summary>
/// Config options for health checks.
/// </summary>
public class Health
{
	public const string SectionName = "Health";

	/// <summary>
	/// Downstream URL to point Trip Update health check head request to.
	/// </summary>
	[Required]
	[Url]
	[System.ComponentModel.Description("Downstream URL to point Trip Update health check head request to.")]
	public required string TripUpdatesCheckUrl { get; init; }

	/// <summary>
	/// Downstream URL to check vehicle positions.
	/// </summary>
	[Required]
	[Url]
	[System.ComponentModel.Description("Downstream URL to check vehicle positions.")]
	public required string VehiclePositionsCheckUrl { get; init; }

}
