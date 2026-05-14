using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mtd.GtfsRealTime.Api.Filters;

/// <summary>
/// Result filter that ensures <see cref="ProblemDetails"/> error responses are always
/// serialized as JSON, regardless of the global <c>ProducesAttribute</c> content-type
/// restriction that normally restricts responses to protobuf in production.
/// </summary>
/// <remarks>
/// <para>
/// The global <see cref="ProducesAttribute"/> (Order = -2000) applied in
/// <c>AddControllers</c> restricts all <see cref="ObjectResult"/> content types to
/// <c>application/protobuf</c>. This works correctly for successful feed responses.
/// However, <c>Problem()</c> and <c>ValidationProblem()</c> also return
/// <see cref="ObjectResult"/>, so their content types are also restricted to protobuf.
/// Since <c>ProtoOutputFormatter</c> cannot serialize <see cref="ProblemDetails"/>, the
/// result is a correct HTTP status code but an empty response body.
/// </para>
/// <para>
/// This filter runs at Order = 0 (after the <see cref="ProducesAttribute"/>) and clears
/// the content-type restriction on any <see cref="ObjectResult"/> whose value is a
/// <see cref="ProblemDetails"/>. This allows <c>SystemTextJsonOutputFormatter</c> to handle
/// the error response normally.
/// </para>
/// </remarks>
internal sealed class ProblemDetailsJsonFilter : IResultFilter, IOrderedFilter
{
	/// <inheritdoc/>
	/// <remarks>Runs after <see cref="ProducesAttribute"/> (Order = -2000).</remarks>
	public int Order => 0;

	/// <inheritdoc/>
	public void OnResultExecuting(ResultExecutingContext context)
	{
		if (context.Result is ObjectResult { Value: ProblemDetails } objectResult)
		{
			objectResult.ContentTypes.Clear();
		}
	}

	/// <inheritdoc/>
	public void OnResultExecuted(ResultExecutedContext context) { }
}
