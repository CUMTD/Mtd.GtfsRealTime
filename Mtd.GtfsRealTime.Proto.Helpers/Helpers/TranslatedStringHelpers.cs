using TransitRealtime;

namespace Mtd.GtfsRealTime.Proto.Helpers.Helpers;

/// <summary>
/// Extension methods for building GTFS-RT <see cref="TranslatedString"/> objects from
/// plain .NET strings.
/// </summary>
public static class TranslatedStringHelpers
{
	/// <summary>
	/// Wraps a plain string in a single-translation <see cref="TranslatedString"/>.
	/// </summary>
	/// <param name="src">The text content.</param>
	/// <param name="lang">BCP 47 language tag (defaults to <c>"en"</c>).</param>
	/// <returns>A <see cref="TranslatedString"/> with one <c>translation</c> entry.</returns>
	public static TranslatedString ToTranslatedString(this string src, string lang = "en")
	{
		var translated = new TranslatedString();

		translated.Translation.Add(new TranslatedString.Types.Translation
		{
			Language = lang,
			Text = src
		});

		return translated;
	}
}
