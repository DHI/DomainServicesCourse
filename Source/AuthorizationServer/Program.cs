using System.Security.Claims;
using DHI.Services;
using DHI.Services.Accounts;
using DHI.Services.Authentication;
using DHI.Services.Authorization;
using DHI.Services.Mails;
using DHI.Services.WebApiCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Polly;
using Swashbuckle.AspNetCore.SwaggerUI;
using ILogger = DHI.Services.Logging.ILogger;

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
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Security.WebApi.xml"));
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

// Pwned passwords
builder.Services
    .AddPwnedPasswordHttpClient(minimumFrequencyToConsiderPwned: 1)
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync(3))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2)));

// Password Policy
builder.Services.AddScoped(_ => new PasswordPolicy
{
    RequiredLength = 10,
    RequiredUniqueChars = 5,
    RequireNonAlphanumeric = false
});

// Configure the necessary services for constructor injection into controllers of DHI Domain Services Web APIs
builder.Services.AddScoped<IMailTemplateRepository>(_ => new DHI.Services.Security.WebApi.MailTemplateRepository("mail-templates.json"));
#warning TODO: Use PostgreSQL as a service?
var postgreSqlConnectionString = "[env:CoursePostgreSqlConnectionString]".Resolve();
builder.Services.AddScoped<IAccountRepository>(_ => new DHI.Services.Provider.PostgreSQL.AccountRepository(postgreSqlConnectionString));
builder.Services.AddScoped<IUserGroupRepository>(_ => new DHI.Services.Provider.PostgreSQL.UserGroupRepository(postgreSqlConnectionString));
builder.Services.AddScoped<IRefreshTokenRepository>(_ => new DHI.Services.Provider.PostgreSQL.RefreshTokenRepository(postgreSqlConnectionString));
builder.Services.AddScoped<IAuthenticationProvider>(_ => new DHI.Services.Provider.PostgreSQL.AccountRepository(postgreSqlConnectionString));
builder.Services.AddScoped<ILogger>(_ => new DHI.Services.Provider.PostgreSQL.Logger(postgreSqlConnectionString));

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
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseExceptionHandling();
app.UseResponseCompression();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Set the data directory (App_Data folder) for DHI Domain Services
var contentRootPath = app.Configuration.GetValue("AppConfiguration:ContentRootPath", app.Environment.ContentRootPath);
AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(contentRootPath, "App_Data"));

app.Run();