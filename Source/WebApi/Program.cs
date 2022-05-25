using System.Security.Claims;
using DHI.Services;
using DHI.Services.Filters;
using DHI.Services.Jobs;
using DHI.Services.Jobs.Workflows;
using DHI.Services.Logging;
using DHI.Services.Provider.MIKECloud;
using DHI.Services.TimeSeries;
using DHI.Services.TimeSeries.Converters;
using DHI.Services.WebApiCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.SwaggerUI;
using HostRepository = DHI.Services.Jobs.WebApi.HostRepository;
using ILogger = DHI.Services.Logging.ILogger;
using Logger = DHI.Services.Provider.PostgreSQL.Logger;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var configuration = builder.Configuration;

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Tokens:Issuer"],
            ValidAudience = configuration["Tokens:Audience"],
            IssuerSigningKey = RSA.BuildSigningKey(configuration["Tokens:PublicRSAKey"].Resolve())
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationhub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorsOnly", policy => policy.RequireClaim(ClaimTypes.GroupSid, "Administrators"));
    options.AddPolicy("EditorsOnly", policy => policy.RequireClaim(ClaimTypes.GroupSid, "Editors"));
});

// API versioning
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version", "version", "ver"),
        new HeaderApiVersionReader("api-version"));
});

// MVC
builder.Services
    .AddResponseCompression(options => { options.EnableForHttps = true; })
    .AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        options.SerializerSettings.Converters.Add(new IsoDateTimeConverter());
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        options.SerializerSettings.Converters.Add(new KeyValuePairConverter());
        options.SerializerSettings.Converters.Add(new DataPointConverter<double, int?>());
        options.SerializerSettings.Converters.Add(new TimeSeriesDataWFlagConverter<double, Dictionary<string, object>>());
        options.SerializerSettings.Converters.Add(new TimeSeriesDataWFlagConverter<double, int?>());
        options.SerializerSettings.Converters.Add(new TimeSeriesDataWFlagConverter<Vector<double>, int?>());
        options.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<double>());
        options.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<Vector<double>>());
    });

// HSTS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.MaxAge = TimeSpan.FromDays(configuration.GetValue<double>("AppConfiguration:HstsMaxAgeInDays"));
});

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(configuration["Swagger:SpecificationName"], new OpenApiInfo
    {
        Title = configuration["Swagger:DocumentTitle"],
        Version = "1",
        Description = File.ReadAllText(configuration["Swagger:DocumentDescription"].Resolve())
    });

    options.EnableAnnotations();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Logging.WebApi.xml"));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Jobs.WebApi.xml"));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.TimeSeries.WebApi.xml"));
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter the word 'Bearer' followed by a space and the JWT.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
builder.Services.AddSwaggerGenNewtonsoftSupport();

// SignalR
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
});

// Configure the necessary services for constructor injection into controllers of DHI Domain Services Web APIs
builder.Services.AddScoped<IHostRepository>(_ => new HostRepository("hosts.json"));
var postgreSqlConnectionString = "[env:PostgreSqlConnectionString]".Resolve();
builder.Services.AddScoped<ILogger>(_ => new Logger(postgreSqlConnectionString));
builder.Services.AddSingleton<IFilterRepository>(_ => new DHI.Services.Provider.PostgreSQL.FilterRepository(postgreSqlConnectionString));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var specificationName = configuration["Swagger:SpecificationName"];
        options.SwaggerEndpoint($"../swagger/{specificationName}/swagger.json", configuration["Swagger:DocumentName"]);
        options.DocExpansion(DocExpansion.None);
        options.DefaultModelsExpandDepth(-1);
    });
}
else
{
    app.UseHsts();
}

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseExceptionHandling();
app.UseResponseCompression();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<NotificationHub>("/notificationhub");
});

// DHI Domain Services
// Set the data directory (App_Data folder)
var contentRootPath = configuration.GetValue("AppConfiguration:ContentRootPath", app.Environment.ContentRootPath);
AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(contentRootPath, "App_Data"));

// Register services
ServiceLocator.Register(new LogService(new Logger(postgreSqlConnectionString)), "pg-logger");

var workflowRepository = new CodeWorkflowRepository("[AppData]workflows.json".Resolve());
var workflowService = new CodeWorkflowService(workflowRepository);
ServiceLocator.Register(workflowService, "wf-tasks");

var jobRepository = new JobRepository<Guid, string>("[AppData]jobs.json".Resolve());
var jobService = new JobService<CodeWorkflow, string>(jobRepository, workflowService);
ServiceLocator.Register(jobService, "wf-jobs");

var apiKey = new Guid("90892a37-668b-4be8-bd7c-cc33eea4eda1");  // API key has to be renewed April 2023
var projectId = new Guid("536f2d5c-988f-423d-84c4-f1a2a0e07afe");  // https://dataadmin.mike-cloud.com/project/536f2d5c-988f-423d-84c4-f1a2a0e07afe
var timeSeriesRepository = new GroupedTimeSeriesRepository(apiKey, projectId);
var timeSeriesService = new GroupedUpdatableTimeSeriesService(timeSeriesRepository);
ServiceLocator.Register(timeSeriesService, "mikecloud-ts");

app.Run();