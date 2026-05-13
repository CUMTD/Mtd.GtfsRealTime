namespace Mtd.GtfsRealTime.Api;

/// <summary>
/// Output-cache policy for relatively static data (e.g. service alerts).
/// Responses are cached for <see cref="DURATION_IN_SECONDS"/> seconds and vary by
/// <c>Accept</c> header, query string, and route values.
/// </summary>
internal static class StaticDataCacheProfile
{
	/// <summary>The name used to reference this policy in <c>[OutputCache]</c> attributes.</summary>
	public const string NAME = "StaticDataCache";

	/// <summary>Cache duration in seconds (5 minutes).</summary>
	public const int DURATION_IN_SECONDS = 300; // 5 minutes
}

/// <summary>
/// Output-cache policy for real-time data (e.g. trip updates, vehicle positions).
/// Responses are cached for <see cref="DURATION_IN_SECONDS"/> seconds and vary by
/// <c>Accept</c> header, query string, and route values.
/// </summary>
internal static class RealTimeDataCacheProfile
{
	/// <summary>The name used to reference this policy in <c>[OutputCache]</c> attributes.</summary>
	public const string NAME = "RealTimeDataCache";

	/// <summary>Cache duration in seconds.</summary>
	public const int DURATION_IN_SECONDS = 5;
}
