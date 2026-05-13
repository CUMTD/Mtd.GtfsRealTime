using System.Text;

using HtmlAgilityPack;

namespace Mtd.GtfsRealTime.Proto.Helpers.Helpers;

public static class RerouteHelpers
{
	private static readonly string[] _blockElements = [
		"address",
		"article",
		"aside",
		"blockquote",
		"details",
		"dialog",
		"dd",
		"div",
		"dl",
		"dt",
		"fieldset",
		"figcaption",
		"figure",
		"footer",
		"form",
		"h1",
		"h2",
		"h3",
		"h4",
		"h5",
		"h6",
		"header",
		"hgroup",
		"hr",
		"li",
		"main",
		"nav",
		"ol",
		"p",
		"pre",
		"section",
		"table",
		"ul",
	];

	public static string StripHtml(this string input)
	{
		var htmlDocument = new HtmlDocument();
		htmlDocument.LoadHtml(input);
		var html = htmlDocument.DocumentNode;

		var blocks = ParseHtmlNode(html)
			.Select(block => block.Replace("&nbsp;", " ").Trim());

		return string.Join('\n', blocks);
	}

	private static List<string> ParseHtmlNode(HtmlNode node)
	{
		var blocks = new List<string>();
		var sb = new StringBuilder();
		foreach (var element in node.ChildNodes)
		{
			if (_blockElements.Contains(element.Name))
			{
				blocks.AddRange(ParseHtmlNode(element));
			}
			else
			{
				_ = sb.Append(element.InnerText);
			}
		}

		if (sb.Length > 0)
		{
			if (node.Name == "li")
			{
				blocks.Add($"\t* {sb}");
			}
			else
			{
				blocks.Add(sb.ToString());
			}
		}

		return blocks;
	}

}
