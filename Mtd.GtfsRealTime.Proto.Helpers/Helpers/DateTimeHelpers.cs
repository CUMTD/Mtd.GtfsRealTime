namespace Mtd.GtfsRealTime.Proto.Helpers.Helpers;

public static class DateTimeHelpers
{
	/// <summary>
	/// Converts a given <see cref="DateTime"/> into a Unix (POSIX) timestamp in seconds.
	/// </summary>
	/// <remarks>The input is treated as UTC; if its <see cref="DateTime.Kind"/> is
	/// <see cref="DateTimeKind.Unspecified"/> it is assumed to be UTC.</remarks>
	/// <param name="value">Any <see cref="DateTime"/>.</param>
	/// <returns>The given <see cref="DateTime"/> as a POSIX timestamp.</returns>
	public static ulong ToPosixTime(this DateTime value)
	{
		var utc = value.Kind switch
		{
			DateTimeKind.Utc => value,
			DateTimeKind.Local => value.ToUniversalTime(),
			_ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
		};
		return new DateTimeOffset(utc, TimeSpan.Zero).ToPosixTime();
	}

	/// <summary>
	/// Converts the specified date and time to the number of seconds that have elapsed since the Unix epoch (January 1,
	/// 1970, 00:00:00 UTC).
	/// </summary>
	/// <remarks>The conversion is performed in UTC. Fractional seconds are truncated. This method is useful for
	/// interoperability with systems and protocols that use POSIX time.</remarks>
	/// <param name="value">The date and time to convert to POSIX time, represented as a <see cref="DateTimeOffset"/>.</param>
	/// <returns>The number of seconds since the Unix epoch as an unsigned 64-bit integer.</returns>
	public static ulong ToPosixTime(this DateTimeOffset value) => (ulong)(value.ToUniversalTime() - DateTimeOffset.UnixEpoch).TotalSeconds;
}
