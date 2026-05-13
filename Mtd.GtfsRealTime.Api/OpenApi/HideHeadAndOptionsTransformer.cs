using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Mtd.GtfsRealTime.Api.OpenApi;

/// <summary>
/// Removes <c>HEAD</c> and <c>OPTIONS</c> operations from every path in the OpenAPI
/// document. The endpoints continue to respond to those verbs at runtime — they are
/// only hidden from Swagger UI / the published spec.
/// </summary>
internal sealed class HideHeadAndOptionsTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (document.Paths is null)
		{
			return Task.CompletedTask;
		}

		var emptyPaths = new List<string>();

		foreach (var (pathKey, pathItem) in document.Paths)
		{
			if (pathItem is null)
			{
				continue;
			}

			pathItem.Operations?.Remove(HttpMethod.Head);
			pathItem.Operations?.Remove(HttpMethod.Options);

			if (pathItem.Operations is null || pathItem.Operations.Count == 0)
			{
				emptyPaths.Add(pathKey);
			}
		}

		foreach (var pathKey in emptyPaths)
		{
			document.Paths.Remove(pathKey);
		}

		return Task.CompletedTask;
	}
}
