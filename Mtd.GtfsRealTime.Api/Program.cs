using System.Text.Json;

using Azure.Identity;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Mtd.GtfsRealTime.Api;
using Mtd.GtfsRealTime.Api.Config;
using Mtd.GtfsRealTime.Api.Formatter;
using Mtd.GtfsRealTime.Api.Health;
using Mtd.Stopwatch.Core.Entities.Schedule;
using Mtd.Stopwatch.Core.Repositories.Schedule;
using Mtd.Stopwatch.Infrastructure.EFCore;
using Mtd.Stopwatch.Infrastructure.EFCore.Repositories.Schedule;

using Serilog;

// Bootstrap logger to log to console in case app crashes before Serilog is configured
// Will be overwritten when Serilog is fully configured.
Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Console()
	.CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Clear any existing SQL connection pools once at startup to ensure a clean state.
SqlConnection.ClearAllPools();

// Handles unhandled exceptions thrown on background threads or other non-task-based operations.
// This is a last-resort global exception handler triggered when an exception escapes all try/catch blocks
// and is about to terminate the application. The process will still terminate after this fires.
AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
{
	Log.Fatal(eventArgs.ExceptionObject as Exception, "An unhandled exception occurred.");
	Log.CloseAndFlush();
};

// Handles unobserved exceptions thrown in Tasks that were not awaited or whose exceptions were never accessed.
// This event is raised when the Task is garbage collected and the exception remains unhandled.
// Calling SetObserved() prevents the process from being affected and suppresses the default behavior.
TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
{
	Log.Fatal(eventArgs.Exception, "An unobserved task exception occurred.");
	eventArgs.SetObserved();
};

// Add appsettings.json and other default configurations
builder.Configuration
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
	builder.Configuration.AddUserSecrets<Program>();
}

var keyVaultUrl = builder.Configuration["KeyVaultUrl"];

if (string.IsNullOrEmpty(keyVaultUrl))
{
	throw new InvalidOperationException("KeyVaultUrl configuration is missing.");
}

var keyVaultUri = new Uri(keyVaultUrl);
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());

builder.Services
			.AddOptions<ConnectionStrings>()
			.BindConfiguration(ConnectionStrings.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();

builder.Services
			.AddOptions<GtfsRealTimeConfig>()
			.BindConfiguration(GtfsRealTimeConfig.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();

builder.Services
			.AddOptions<Health>()
			.BindConfiguration(Health.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();

// Configure EF Core to use Azure SQL Database with access token authentication
// For stopwatch data
builder.Services.AddDbContextPool<StopwatchContext>((sp, options) =>
{
	var config = sp.GetRequiredService<IOptions<ConnectionStrings>>().Value;
	var connectionString = config.StopwatchConnectionString;

	// Authenticate using Azure AD (managed identity or dev credentials)
	var credential = new DefaultAzureCredential();

	// Configure EF Core behavior and retry policy
	options.AddInterceptors(new AzureSqlAccessTokenInterceptor(credential));
	options.UseSqlServer(connectionString, sql =>
	{
		sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
		sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
	});
});

// DI
builder.Services.AddScoped<IRerouteRepository<IReadOnlyCollection<Reroute>>, RerouteRepository>();

builder.Services.AddHttpClient(DownstreamHeadHealthCheck.CLIENT_NAME, client => client.Timeout = TimeSpan.FromSeconds(2));

builder.Services.AddControllers(options =>
{
	options.RespectBrowserAcceptHeader = true;

	// Remove unused output formatters
	// we only want to return JSON or Protobufs
	options.OutputFormatters.RemoveType<StringOutputFormatter>();
	options.OutputFormatters.RemoveType<StreamOutputFormatter>();

	var allowJson = bool.TryParse(builder.Configuration["AllowJson"], out var allowJsonConfigValue) && allowJsonConfigValue;
	if (allowJson)
	{
		options.Filters.Add(new ProducesAttribute("application/json", "application/protobuf", "application/x-protobuf"));
	}
	else
	{
		options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
		options.Filters.Add(new ProducesAttribute("application/protobuf", "application/x-protobuf"));
	}

	// Add our Protobuf formatter
	// It is first so that it is used if the request does not specify an 'Accepts' header
	options.OutputFormatters.Add(new ProtoOutputFormatter());
});

builder.Services.AddRouting(options =>
{
	options.LowercaseUrls = true;
	options.AppendTrailingSlash = true;
});

if (builder.Environment.IsProduction())
{
	builder.Services.AddHsts(options =>
	{
		options.Preload = true;
		options.IncludeSubDomains = true;
		options.MaxAge = TimeSpan.FromDays(365 * 2);
	});
}

builder.Services.AddOpenApi();

// Setup Serilog for structured logging using config from appsettings.json
builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
	   .ReadFrom.Configuration(context.Configuration)
	   .ReadFrom.Services(services)
	   .Enrich.WithProperty("SiteName", Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"))
	   .Enrich.WithProperty("InstanceId", Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"))
	   .Enrich.WithProperty("SlotName", Environment.GetEnvironmentVariable("WEBSITE_SLOT_NAME")));

// Configure CORS to allow unrestricted public access
var corsPolicyName = builder.Configuration["Cors:PolicyName"] ?? throw new InvalidOperationException("Cors:PolicyName not defined");

builder.Services.AddCors(options => options.AddPolicy(
	corsPolicyName,
	policy => policy
	.AllowAnyOrigin()
	.AllowAnyHeader()
	.AllowAnyMethod()
));

var hc = builder.Services.AddHealthChecks();

hc.AddCheck(
	name: "self",
	check: () => HealthCheckResult.Healthy("The API is healthy and responsive."),
	tags: ["live", "healthy"]
);

hc.AddDbContextCheck<StopwatchContext>(
	name: "db",
	failureStatus: HealthStatus.Unhealthy,
	tags: ["ready", "healthy", "db"]
);

hc.AddCheck<DownstreamHeadHealthCheck>(DownstreamHeadHealthCheck.TRIP_UPDATES_NAME, tags: ["ready", "healthy", "network"]);
hc.AddCheck<DownstreamHeadHealthCheck>(DownstreamHeadHealthCheck.VEHICLE_POSITIONS_NAME, tags: ["ready", "healthy", "network"]);

// We also use the output cache to cache API responses where appropriate.
builder.Services.AddOutputCache(options =>
{
	options
		.AddPolicy(StaticDataCacheProfile.NAME, policy => policy
			.Expire(TimeSpan.FromSeconds(StaticDataCacheProfile.DURATION_IN_SECONDS))
			.Tag(StaticDataCacheProfile.NAME)
			.SetVaryByQuery("*")
			.SetVaryByRouteValue("*")
		);

	options
		.AddPolicy(RealTimeDataCacheProfile.NAME, policy => policy
			.Expire(TimeSpan.FromSeconds(RealTimeDataCacheProfile.DURATION_IN_SECONDS))
			.Tag(RealTimeDataCacheProfile.NAME)
			.SetVaryByQuery("*")
			.SetVaryByRouteValue("*")
		);

	options.UseCaseSensitivePaths = false;

});

var app = builder.Build();

if (app.Environment.IsProduction())
{
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Enables CORS (Cross-Origin Resource Sharing) using the named policy.
// This allows public or browser-based clients to access the API across domains.
app.UseCors(corsPolicyName);

app.UseDefaultFiles();

app.UseStaticFiles();

app.MapOpenApi("/openapi/{documentName}.yaml");

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
	Predicate = r => r.Tags.Contains("live"),
	ResponseWriter = WriteJsonResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
	Predicate = r => r.Tags.Contains("ready"),
	ResponseWriter = WriteJsonResponse
});

app.MapHealthChecks("/health/healthy", new HealthCheckOptions
{
	Predicate = r => r.Tags.Contains("healthy"),
	ResponseWriter = WriteJsonResponse
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
	Predicate = r => r.Tags.Contains("db"),
	ResponseWriter = WriteJsonResponse
});

app.MapHealthChecks("/health/logs", new HealthCheckOptions
{
	Predicate = r => r.Tags.Contains("logs"),
	ResponseWriter = WriteJsonResponse
});

app.MapHealthChecks("/health/network", new HealthCheckOptions
{
	Predicate = r => r.Tags.Contains("network"),
	ResponseWriter = WriteJsonResponse
});

try
{
	// Start the web application
	Log.Information("Application started successfully in {Environment}", app.Environment.EnvironmentName);
	Log.Information("Build info: {Version}", typeof(Program).Assembly.FullName);
	await app.RunAsync();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Application terminated unexpectedly");
	throw;
}
finally
{
	Log.CloseAndFlush();
}



static Task WriteJsonResponse(HttpContext context, HealthReport report)
{
	context.Response.ContentType = "application/json";

	var payload = new
	{
		status = report.Status.ToString(),
		checks = report.Entries.Select(e => new
		{
			name = e.Key,
			status = e.Value.Status.ToString(),
			description = e.Value.Description,
			durationMs = e.Value.Duration.TotalMilliseconds,
			error = e.Value.Exception?.Message
		})
	};

	return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
