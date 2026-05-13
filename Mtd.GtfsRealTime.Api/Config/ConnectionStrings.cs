using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mtd.GtfsRealTime.Api.Config;

public class ConnectionStrings
{
	public const string SectionName = "ConnectionStrings";

	[Required]
	[Description("Database connection string")]
	public required string StopwatchConnectionString { get; init; }
}
