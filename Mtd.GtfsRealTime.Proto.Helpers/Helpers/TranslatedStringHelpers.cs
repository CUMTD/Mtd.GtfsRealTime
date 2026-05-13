using TransitRealtime;

namespace Mtd.GtfsRealTime.Proto.Helpers.Helpers;

public static class TranslatedStringHelpers
{
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
