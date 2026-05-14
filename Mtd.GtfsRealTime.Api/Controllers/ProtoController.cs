using Google.Protobuf;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Mtd.GtfsRealTime.Api.Controllers;

/// <summary>
/// Abstract base controller for endpoints that return GTFS-Realtime
/// <see cref="IMessage{T}"/> data, either passed through from a downstream feed server or
/// built locally.
/// </summary>
/// <typeparam name="TMessage">
/// The concrete proto message type (e.g. <c>FeedMessage</c>).
/// </typeparam>
/// <remarks>
/// Subclasses that proxy a downstream feed should call
/// <see cref="GetProtoResponseFromDownstreamServer"/>.
/// Subclasses that build their own feed (e.g. <c>ServiceAlertsController</c>) should
/// inherit <see cref="ControllerBase"/> directly instead.
/// </remarks>
[ApiController]
public abstract class ProtoController<TMessage> : ControllerBase
	where TMessage : IMessage<TMessage>
{
	private const string JsonMediaType = "application/json";
	private const string ProtobufMediaType = "application/protobuf";
	private const string XProtobufMediaType = "application/x-protobuf";

	private static readonly JsonFormatter _jsonFormatter = JsonFormatter.Default;

	protected readonly HttpClient _httpClient;
	protected readonly ILogger _logger;
	protected readonly MessageParser<TMessage> _messageParser;

	protected ProtoController(MessageParser<TMessage> messageParser, HttpClient httpClient, ILogger logger)
	{
		ArgumentNullException.ThrowIfNull(messageParser, nameof(messageParser));
		ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_messageParser = messageParser;
		_httpClient = httpClient;
		_logger = logger;
	}

	/// <summary>
	/// Fetches raw bytes from the downstream GTFS-RT feed server without parsing.
	/// </summary>
	/// <param name="downstreamServer">Absolute URI of the upstream GTFS-RT feed endpoint.</param>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	/// <returns>
	/// A tuple where <c>ResponseBytes</c> contains the raw protobuf bytes on success,
	/// or <c>Error</c> contains an appropriate <see cref="IActionResult"/> on failure.
	/// Exactly one of the two will be non-null.
	/// </returns>
	protected async Task<(byte[]? ResponseBytes, IActionResult? Error)> FetchBytesFromDownstreamServer(Uri downstreamServer, CancellationToken cancellationToken)
	{
		HttpResponseMessage httpResponseMessage;
		try
		{
			httpResponseMessage = await _httpClient.GetAsync(downstreamServer, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch from downstream server: {uri}.", downstreamServer);
			return (null, Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Downstream Fetch Error", detail: "Failed to connect to the downstream server."));
		}

		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			_logger.LogError("Got error status code ({statusCode}) from downstream server: {uri}.", httpResponseMessage.StatusCode, downstreamServer);
			return (null, Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Downstream Server Error", detail: "The downstream server returned an error"));
		}

		byte[] responseBytes;
		try
		{
			responseBytes = await httpResponseMessage.Content.ReadAsByteArrayAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to decode responseBytes from downstream server: {uri}.", downstreamServer);
			return (null, Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Response Decoding Error", detail: "Failed to decode the responseBytes from the downstream server"));
		}

		return (responseBytes, null);
	}

	/// <summary>
	/// Fetches the GTFS-RT feed from <paramref name="downstreamServer"/> and parses it into
	/// a <typeparamref name="TMessage"/> instance.
	/// </summary>
	/// <param name="downstreamServer">Absolute URI of the upstream GTFS-RT feed endpoint.</param>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	/// <returns>
	/// A tuple where <c>Message</c> contains the parsed protobuf message on success,
	/// or <c>Error</c> contains an appropriate <see cref="IActionResult"/> on failure.
	/// Exactly one of the two will be non-null.
	/// </returns>
	protected async Task<(TMessage? Message, IActionResult? Error)> FetchAndParseFromDownstreamServer(Uri downstreamServer, CancellationToken cancellationToken)
	{
		var (responseBytes, fetchError) = await FetchBytesFromDownstreamServer(downstreamServer, cancellationToken);
		if (fetchError is not null)
		{
			return (default, fetchError);
		}

		TMessage parsed;
		try
		{
			parsed = _messageParser.ParseFrom(responseBytes);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to parse protobuf response from downstream server: {uri}.", downstreamServer);
			return (default, Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Response Parsing Error", detail: "Failed to parse the protobuf response from the downstream server"));
		}

		return (parsed, null);
	}

	/// <summary>
	/// Fetches the GTFS-RT feed from <paramref name="downstreamServer"/>, then returns it as
	/// either a protobuf binary response or canonical proto3 JSON, depending on the client's
	/// <c>Accept</c> header.
	/// </summary>
	/// <param name="downstreamServer">Absolute URI of the upstream GTFS-RT feed endpoint.</param>
	/// <param name="cancellationToken">Propagates notification that the request has been cancelled.</param>
	/// <returns>
	/// <list type="bullet">
	///   <item>
	///     <description>
	///       <c>200 OK</c> with a protobuf binary body (or proto3 JSON when <c>Accept: application/json</c>).
	///     </description>
	///   </item>
	///   <item><description><c>500 Internal Server Error</c> if the downstream request fails or the response cannot be decoded/parsed.</description></item>
	/// </list>
	/// </returns>
	protected async Task<IActionResult> GetProtoResponseFromDownstreamServer(Uri downstreamServer, CancellationToken cancellationToken)
	{
		var (responseBytes, fetchError) = await FetchBytesFromDownstreamServer(downstreamServer, cancellationToken);
		if (fetchError is not null)
		{
			return fetchError;
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
