using Microsoft.AspNetCore.Mvc;

namespace Mtd.GtfsRealTime.Api.Controllers;


[ApiController]
public abstract class ProtoPassThroughController : ControllerBase
{

	protected readonly HttpClient _httpClient;
	protected readonly ILogger<ProtoPassThroughController> _logger;

	protected ProtoPassThroughController(HttpClient httpClient, ILogger<ProtoPassThroughController> logger)
	{
		ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

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

		return Ok(responseBytes);
	}

}
