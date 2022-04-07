namespace WebApi
{
    using DHI.Services;
    using DHI.Services.Logging;
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
    using Swashbuckle.AspNetCore.SwaggerUI;
    using System;
    using System.Collections.Generic;
    using System.IO;
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

#warning Enable if using SignalR capabilities
                //options.Events = new JwtBearerEvents
                //{
                //    OnMessageReceived = context =>
                //    {
                //        var accessToken = context.Request.Query["access_token"];
                //        var path = context.HttpContext.Request.Path;
                //        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationhub"))
                //        {
                //            context.Token = accessToken;
                //        }
                //        return Task.CompletedTask;
                //    }
                //};
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
#warning Depending on which Web API packages you install in this project, you need to register domain-specific JSON converters for these packages
                // --> GIS service JSON converters. Install NuGet package DHI.Spatial.GeoJson
                //options.SerializerSettings.Converters.Add(new PositionConverter());
                //options.SerializerSettings.Converters.Add(new GeometryConverter());
                //options.SerializerSettings.Converters.Add(new AttributeConverter());
                //options.SerializerSettings.Converters.Add(new FeatureConverter());
                //options.SerializerSettings.Converters.Add(new FeatureCollectionConverter());
                //options.SerializerSettings.Converters.Add(new GeometryCollectionConverter());

                // --> Timeseries services JSON converters
                //options.SerializerSettings.Converters.Add(new DataPointConverter<double, int?>());
                //options.SerializerSettings.Converters.Add(new TimeSeriesDataWFlagConverter<double, Dictionary<string, object>>());
                //options.SerializerSettings.Converters.Add(new TimeSeriesDataWFlagConverter<double, int?>());
                //options.SerializerSettings.Converters.Add(new TimeSeriesDataWFlagConverter<Vector<double>, int?>());
                //options.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<double>());
                //options.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<Vector<double>>());
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
                setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Connections.WebApi.xml"));
                setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Logging.WebApi.xml"));
#warning Depending on which Web API packages you install in this project, you need to register the XML-files from these packages for descriptions in Swagger UI
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Documents.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.GIS.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Jobs.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.JsonDocuments.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Meshes.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Models.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Places.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Rasters.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Scalars.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Spreadsheets.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Tables.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.TimeSeries.WebApi.xml"));
            //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.TimeSteps.WebApi.xml"));
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
                        In = ParameterLocation.Header

                    },
                    new List<string>()
                }
                });
            });
            services.AddSwaggerGenNewtonsoftSupport();

            // SignalR
#warning Enable if using SignalR capabilities
            //services.AddSignalR();

            // DHI Domain Services
#warning In production code, you should use replace the JSON-file based repostiories with for example the PostgreSQL repositories
            services.AddScoped<ILogger>(_ => new JsonLogger("[AppData]log.json".Resolve()));
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
#warning Enable if using SignalR capabilities
            //endpoints.MapHub<NotificationHub>("/notificationhub");
        });

            // DHI Domain Services
            // Set the data directory (App_Data folder)
            var contentRootPath = Configuration.GetValue("AppConfiguration:ContentRootPath", env.ContentRootPath);
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(contentRootPath, "App_Data"));

            // Register services
            ServiceLocator.Register(new LogService(new JsonLogger("[AppData]log.json".Resolve())), "json-logger");
        }
    }
}