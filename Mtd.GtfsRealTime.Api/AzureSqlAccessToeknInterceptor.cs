using System.Data.Common;

using Azure.Core;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mtd.GtfsRealTime.Api;


/// <summary>
/// Intercepts SQL connections to add an access token for Azure SQL authentication.
/// </summary>
/// <param name="credential">Token credential used to obtain access tokens.</param>
internal sealed class AzureSqlAccessTokenInterceptor(TokenCredential credential) : DbConnectionInterceptor
{
	private static readonly string[] _scopes = ["https://database.windows.net/.default"];

	/// <summary>
	/// Adds an access token when a connection is opening.
	/// </summary>
	public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
	{
		if (connection is SqlConnection sqlConnection)
		{
			var token = credential.GetToken(new TokenRequestContext(_scopes), default);
			sqlConnection.AccessToken = token.Token;
		}

		return result;
	}

	/// <summary>
	/// Asynchronously adds an access token when a connection is opening.
	/// </summary>
	public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
		DbConnection connection,
		ConnectionEventData eventData,
		InterceptionResult result,
		CancellationToken cancellationToken = default)
	{
		if (connection is SqlConnection sqlConnection)
		{
			var token = await credential.GetTokenAsync(new TokenRequestContext(_scopes), cancellationToken);
			sqlConnection.AccessToken = token.Token;
		}

		return result;
	}
}
