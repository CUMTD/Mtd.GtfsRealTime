using Google.Protobuf;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Mtd.GtfsRealTime.Api.Controllers;

[ApiController]
public abstract class ProtoPassThroughController<TMessage> : ControllerBase
	where TMessage : IMessage<TMessage>
{
	private const string JsonMediaType = "application/json";
	private const string ProtobufMediaType = "application/protobuf";
	private const string XProtobufMediaType = "application/x-protobuf";

	private static readonly JsonFormatter _jsonFormatter = JsonFormatter.Default;

	protected readonly HttpClient _httpClient;
	protected readonly ILogger _logger;
	protected readonly MessageParser<TMessage> _messageParser;

	protected ProtoPassThroughController(MessageParser<TMessage> messageParser, HttpClient httpClient, ILogger logger)
	{
		ArgumentNullException.ThrowIfNull(messageParser, nameof(messageParser));
		ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_messageParser = messageParser;
		_httpClient = httpClient;
		_logger = logger;
	}

	protected async Task<IActionResult> GetProtoResponseFromDownstreamServer(Uri downstreamServer, CancellationToken cancellationToken)
	{
		HttpResponseMessage httpResponseMessage;
		try
		{
			httpResponseMessage = await _httpClient.GetAsync(downstreamServer, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch from downstream server: {uri}.", downstreamServer);
			throw;
		}

		// ensure fetch was successful
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			_logger.LogError("Got error status code ({statusCode}) from downstream server: {uri}.", httpResponseMessage.StatusCode, downstreamServer);
			return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Downstream Server Error", detail: "The downstream server returned an error");
		}

		// convert to byte[]
		byte[] responseBytes;
		try
		{
			responseBytes = await httpResponseMessage.Content.ReadAsByteArrayAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to decode responseBytes from downstream server: {uri}.", downstreamServer);
			return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Response Decoding Error", detail: "Failed to decode the responseBytes from the downstream server");
		}

		if (ClientPrefersJson(Request))
		{
			TMessage parsed;
			try
			{
				parsed = _messageParser.ParseFrom(responseBytes);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to parse protobuf response from downstream server: {uri}.", downstreamServer);
				return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Response Parsing Error", detail: "Failed to parse the protobuf response from the downstream server");
			}

			var json = _jsonFormatter.Format(parsed);
			return Content(json, JsonMediaType);
		}

		return Ok(responseBytes);
	}

	/// <summary>
	/// Returns <c>true</c> when the client's Accept header indicates JSON is preferred over
	/// our protobuf media types (taking q-values into account). When no Accept header is
	/// present, or it is wildcard-only, protobuf is preferred (matches existing behavior).
	/// </summary>
	private static bool ClientPrefersJson(HttpRequest request)
	{
		var acceptHeaders = request.Headers.Accept;
		if (acceptHeaders.Count == 0)
		{
			return false;
		}

		IList<MediaTypeHeaderValue> parsed;
		try
		{
			parsed = MediaTypeHeaderValue.ParseList(acceptHeaders);
		}
		catch (FormatException)
		{
			return false;
		}

		var ordered = parsed
			.OrderByDescending(m => m.Quality ?? 1.0)
			.ToList();

		foreach (var mediaType in ordered)
		{
			var value = mediaType.MediaType.Value;
			if (value is null)
			{
				continue;
			}

			if (string.Equals(value, JsonMediaType, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (string.Equals(value, ProtobufMediaType, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(value, XProtobufMediaType, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		return false;
	}
}
