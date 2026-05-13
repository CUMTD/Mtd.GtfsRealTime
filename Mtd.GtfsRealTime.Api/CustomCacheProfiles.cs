namespace Mtd.GtfsRealTime.Api;

internal record StaticDataCacheProfile
{
	public const string NAME = "StaticDataCache";
	public const int DURATION_IN_SECONDS = 300; // 5 minutes
}

internal record RealTimeDataCacheProfile
{
	public const string NAME = "RealTimeDataCache";
	public const int DURATION_IN_SECONDS = 5;
}
