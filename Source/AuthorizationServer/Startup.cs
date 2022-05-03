namespace AuthorizationServer;

using DHI.Services.Accounts;
using DHI.Services.Authentication;
using DHI.Services.Authorization;
using DHI.Services.Logging;
using DHI.Services.Mails;
using DHI.Services.WebApiCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Polly;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Tokens:Issuer"],
                    ValidAudience = Configuration["Tokens:Audience"],
                    IssuerSigningKey = RSA.BuildSigningKey(Configuration["Tokens:PublicRSAKey"].Resolve())
                };
            });

        // Authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdministratorsOnly", policy => policy.RequireClaim(ClaimTypes.GroupSid, "Administrators"));
            options.AddPolicy("EditorsOnly", policy => policy.RequireClaim(ClaimTypes.GroupSid, "Editors"));
        });

        // API versioning
        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("api-version", "version", "ver"),
                new HeaderApiVersionReader("api-version"));
        });

        // MVC
        services
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
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.MaxAge = TimeSpan.FromDays(Configuration.GetValue<double>("AppConfiguration:HstsMaxAgeInDays"));
        });

        // Swagger
        services.AddSwaggerGen(setupAction =>
        {
            setupAction.SwaggerDoc(Configuration["Swagger:SpecificationName"], new OpenApiInfo
            {
                Title = Configuration["Swagger:DocumentTitle"],
                Version = "1",
                Description = File.ReadAllText(Configuration["Swagger:DocumentDescription"].Resolve())

            });

            setupAction.EnableAnnotations();
            setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Security.WebApi.xml"));
            setupAction.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter the word 'Bearer' followed by a space and the JWT.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement()
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
                        In = ParameterLocation.Header,

                    },
                    new List<string>()
                }
            });
        });
        services.AddSwaggerGenNewtonsoftSupport();

        // Pwned passwords
        services
            .AddPwnedPasswordHttpClient(minimumFrequencyToConsiderPwned: 1)
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync(3))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2)));

        // DHI Domain Services
            
        services.AddScoped<IMailTemplateRepository>(_ => new DHI.Services.Security.WebApi.MailTemplateRepository("mail-templates.json"));

#warning TODO: Use PostgreSQL as a service?
        const string postgreSqlConnectionString = "Server=localhost;Port=5432;Database=DomainServicesCourse;User Id=postgres;Password=Solutions!";
        services.AddScoped<IAccountRepository>(_ => new DHI.Services.Provider.PostgreSQL.AccountRepository(postgreSqlConnectionString));
        services.AddScoped<IUserGroupRepository>(_ => new DHI.Services.Provider.PostgreSQL.UserGroupRepository(postgreSqlConnectionString));
        services.AddScoped<IRefreshTokenRepository>(_ => new DHI.Services.Provider.PostgreSQL.RefreshTokenRepository(postgreSqlConnectionString));
        services.AddScoped<IAuthenticationProvider>(_ => new DHI.Services.Provider.PostgreSQL.AccountRepository(postgreSqlConnectionString));
        services.AddScoped<ILogger>(_ => new DHI.Services.Provider.PostgreSQL.Logger(postgreSqlConnectionString));
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHsts();
        }

        app.UseAuthentication();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.UseSwagger();
        app.UseSwaggerUI(setupAction =>
        {
            var specificationName = Configuration["Swagger:SpecificationName"];
            setupAction.SwaggerEndpoint($"../swagger/{specificationName}/swagger.json", Configuration["Swagger:DocumentName"]);
            setupAction.DocExpansion(DocExpansion.None);
            setupAction.DefaultModelsExpandDepth(-1);
        });
        app.UseExceptionHandling();
        app.UseResponseCompression();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        // Set the data directory (App_Data folder) for DHI Domain Services
        var contentRootPath = Configuration.GetValue("AppConfiguration:ContentRootPath", env.ContentRootPath);
        AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(contentRootPath, "App_Data"));
    }
}