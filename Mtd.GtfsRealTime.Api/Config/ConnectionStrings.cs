using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mtd.GtfsRealTime.Api.Config;

/// <summary>
/// Connection string configuration for the Stopwatch database.
/// </summary>
public class ConnectionStrings
{
	/// <summary>The configuration section name.</summary>
	public const string SectionName = "ConnectionStrings";

	/// <summary>
	/// Azure SQL connection string for the Stopwatch database.
	/// Authentication is handled by managed identity — no password required.
	/// </summary>
	[Required]
	[Description("Database connection string")]
	public required string StopwatchConnectionString { get; init; }
}
