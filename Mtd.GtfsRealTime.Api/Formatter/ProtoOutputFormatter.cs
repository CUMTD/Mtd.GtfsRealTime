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

		// Swagger UI's ResponseBody component decides whether to render a "Download file"
		// link (vs. "Unrecognized response type; displaying content as text.") based on
		// these response headers, not on the OpenAPI schema. Setting them here gives a
		// good default UX in Swagger UI and any other client that honors them.
		var fileName = GetDownloadFileName(httpContext.Request.Path);
		httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
		httpContext.Response.Headers["Content-Description"] = "File Transfer";

		await httpContext.Response.Body.WriteAsync(dto);
	}

	private static string GetDownloadFileName(PathString path)
	{
		var pathValue = path.HasValue ? path.Value!.Trim('/') : string.Empty;
		if (string.IsNullOrEmpty(pathValue))
		{
			return "response.pb";
		}

		var lastSegment = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
		return string.IsNullOrEmpty(lastSegment) ? "response.pb" : $"{lastSegment}.pb";
	}
}
