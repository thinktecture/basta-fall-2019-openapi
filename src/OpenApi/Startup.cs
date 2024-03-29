using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenApi.Services;
using OpenApi.Swagger;
using Swashbuckle.AspNetCore.Filters;

namespace OpenApi
{
#pragma warning disable 1591 // Missing XML Doc
	public class Startup
	{
		private static readonly int[] ApiVersions = new[] { 1, 2, 3, };
		private static readonly int DefaultApiVersion = ApiVersions.Last();

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<ArticleService>(); // Dummy service for some runtime-persistence

			// Enable Mvc/Web Api Controllers
			services.AddControllers(config =>
			{
				#region Content negotiation
				//config.RespectBrowserAcceptHeader = true;
				//config.InputFormatters.Add(new XmlSerializerInputFormatter(config));
				//config.OutputFormatters.Add(new XmlSerializerOutputFormatter());
				#endregion
			});

			#region AuthN & AuthZ
			services
				.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
				.AddIdentityServerAuthentication(options =>
				{
					options.Authority = "https://demo.identityserver.io";
					options.ApiName = "api";
				});

			services
				.AddAuthorization(config =>
				{
					config.AddPolicy("api", builder =>
					{
						builder.RequireAuthenticatedUser();
						builder.RequireScope("api");
					});
				});
			#endregion

			#region Api Versioning
			services.AddApiVersioning(options =>
			{
				options.DefaultApiVersion = new ApiVersion(DefaultApiVersion, 0);
				options.AssumeDefaultVersionWhenUnspecified = true;
			});

			services.AddVersionedApiExplorer(options =>
			{
				// Swagger uses GroupName to sort endpoints into certain documents, so make sure ApiExplorer
				// sets a group name that corresponds to the version url part
				options.GroupNameFormat = "'v'VVV";

				// Make sure we do not have the {version} part of the Url as parameter in the swagger urls
				options.SubstituteApiVersionInUrl = true;
			});
			#endregion

			#region Swagger
			services
				.AddSwaggerGen(c =>
				{
					#region Multiple versioned documents

					foreach (var apiVersion in ApiVersions)
					{
						var versionString = $"v{apiVersion}";

						c.SwaggerDoc(versionString, new OpenApiInfo()
						{
							Title = $"OpenAPI Sample API {versionString}",
							Version = versionString,
							Description = "Sample API for an OpenAPI conference talk.",
							Contact = new SampleApiContact(),
							License = new SampleApiLicense(),
						});
					}
					#endregion


					#region AuthN & AuthZ

					c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
					{
						Type = SecuritySchemeType.OAuth2,
						Name = "Authorization",
						In = ParameterLocation.Header,
						Scheme = "Bearer",
						OpenIdConnectUrl = new Uri("https://demo.identityserver.io"),
						Flows = new OpenApiOAuthFlows()
						{
							Implicit = new OpenApiOAuthFlow()
							{
								AuthorizationUrl = new Uri("https://demo.identityserver.io/connect/authorize"),
								Scopes = new Dictionary<string, string>()
								{
									{ "api", "API Access" },
								},
							},
						},
					});

					#endregion

					#region Customization
					c.EnableAnnotations();
					c.IncludeXmlComments("./OpenApi.xml");

					c.OperationFilter<AddCorrelationIdHeaderOperationFilter>();
					c.OperationFilter<AddDeletionIdHeaderOperationFilter>();
					c.DocumentFilter<ApiInfoDocumentFilter>();
					#endregion

				});
			#endregion
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			// Activate new endpoint routing in AspNet Core 3.0
			app.UseRouting();

			#region AuthN & AuthZ
			// Order is important: Authorization middleware requires routing information
			// from middleware above, to retrieve required policy information from the
			// (now resolved) controller.action metadata.
			app.UseAuthentication();
			app.UseAuthorization();
			#endregion

			app.UseEndpoints(endpoints =>
			{
				// Dispatch to actual controllers
				endpoints.MapControllers();
			});

			#region Swagger
			// Maybe Swagger / SwaggerUI integration will provide endpoint routing for
			// AspNet Core 3.0 in a later release; for now, use conventional middlewares
			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.RoutePrefix = "docs";

				#region Multiple documents
				foreach (var version in ApiVersions)
				{
					c.SwaggerEndpoint($"/swagger/v{version}/swagger.json", $"OpenAPI Sample API v{version}");
				}
				#endregion

				#region AuthN & AuthZ
				c.OAuthClientId("implicit");
				#endregion

				#region Customization
				c.InjectStylesheet("/swagger_custom.css");
				#endregion
			});
			#endregion

			app.UseStaticFiles();
		}
	}

#pragma warning restore 1591
}
