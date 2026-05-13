using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

using Mtd.GtfsRealTime.Proto.Helpers;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Api.Formatter;

public class ProtoOutputFormatter : OutputFormatter
{
	public ProtoOutputFormatter()
	{
		SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/x-protobuf"));
		SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/protobuf"));
	}
	public override bool CanWriteResult(OutputFormatterCanWriteContext context)
	{
		// we can handle feed messages or byte[]
		if (typeof(FeedMessage).IsAssignableFrom(context.ObjectType) || typeof(byte[]).IsAssignableFrom(context.ObjectType))
		{
			return base.CanWriteResult(context);
		}
		return false;
	}
	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
	{
		var httpContext = context.HttpContext;

		byte[] dto;
		if (context.Object is byte[] bytes)
		{
			dto = bytes;
		}
		else if (context.Object is ISerializeDTO gtfsObj)
		{
			dto = gtfsObj.Serialize();
		}
		else
		{
			throw new ArgumentException("Object was not a Component.");
		}

		var acceptHeader = httpContext.Request.Headers.Accept.FirstOrDefault();
		var contentType = "application/protobuf";

		if (!string.IsNullOrWhiteSpace(acceptHeader) && MediaTypeHeaderValue.TryParse(acceptHeader, out var parsedMediaType))
		{
		    var mediaTypeString = parsedMediaType.MediaType.Value;
		    if (mediaTypeString is "application/x-protobuf" or "application/protobuf")
		    {
		        contentType = mediaTypeString;
		    }
		}

		httpContext.Response.ContentType = contentType;
		httpContext.Response.ContentLength = dto.Length;
		await httpContext.Response.Body.WriteAsync(dto);
	}
}
