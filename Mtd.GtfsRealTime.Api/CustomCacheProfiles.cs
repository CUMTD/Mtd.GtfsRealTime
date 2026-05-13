namespace Mtd.GtfsRealTime.Api;

internal static class StaticDataCacheProfile
{
	public const string NAME = "StaticDataCache";
	public const int DURATION_IN_SECONDS = 300; // 5 minutes
}

internal static class RealTimeDataCacheProfile
{
	public const string NAME = "RealTimeDataCache";
	public const int DURATION_IN_SECONDS = 5;
}
