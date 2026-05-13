using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Mtd.GtfsRealTime.Api.OpenApi;

/// <summary>
/// Replaces the schema of any <c>application/protobuf</c> or <c>application/x-protobuf</c>
/// response media type with a binary string schema. This causes Swagger UI to render a
/// "Download file" link for protobuf responses instead of "Unrecognized response type;
/// displaying content as text."
/// </summary>
internal sealed class ProtobufBinaryMediaTypeTransformer : IOpenApiOperationTransformer
{
	private static readonly string[] _protobufMediaTypes =
	[
		"application/protobuf",
		"application/x-protobuf",
	];

	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
	{
		if (operation.Responses is null)
		{
			return Task.CompletedTask;
		}

		foreach (var response in operation.Responses.Values)
		{
			if (response is not OpenApiResponse concreteResponse || concreteResponse.Content is null)
			{
				continue;
			}

			foreach (var mediaType in _protobufMediaTypes)
			{
				if (concreteResponse.Content.TryGetValue(mediaType, out var mediaTypeObject) && mediaTypeObject is not null)
				{
					mediaTypeObject.Schema = new OpenApiSchema
					{
						Type = JsonSchemaType.String,
						Format = "binary",
					};
				}
			}
		}

		return Task.CompletedTask;
	}
}
