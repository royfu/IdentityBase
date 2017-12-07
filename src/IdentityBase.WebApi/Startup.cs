// Copyright (c) Russlan Akiev. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace IdentityBase.WebApi
{
    using System;
    using IdentityBase.Configuration;
    using IdentityBase.Crypto;
    using IdentityBase.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using ServiceBase;
    using ServiceBase.Modules;

    public class Startup : IStartup
    {
        private readonly ILogger<Startup> _logger;
        private readonly IHostingEnvironment _environment;
        private readonly ModulesStartup _modulesStartup;
        private readonly IConfiguration _configuration;
        private readonly WebApiOptions _webApiOptions;
        private readonly ApplicationOptions _applicationOptions;

        public Startup(
            IConfiguration configuration,
            IHostingEnvironment environment,
            ILoggerFactory loggerFactory)
        {
            this._logger = loggerFactory.CreateLogger<Startup>();
            this._environment = environment;
            this._configuration = configuration;
            this._modulesStartup = new ModulesStartup(configuration);

            this._applicationOptions = this._configuration.GetSection("App")
                .Get<ApplicationOptions>() ?? new ApplicationOptions();

            this._webApiOptions = this._configuration.GetSection("WebApi")
                .Get<WebApiOptions>() ?? new WebApiOptions();
        }
        
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            this._logger.LogInformation("Configure WebAPI services.");

            services.AddSingleton(this._configuration);
            services.AddSingleton(this._applicationOptions);
            services.AddSingleton(this._webApiOptions);

            services.AddTransient<ICrypto, DefaultCrypto>();
            services.AddTransient<UserAccountService>();
            services.AddTransient<ClientService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IDateTimeAccessor, DateTimeAccessor>();
            
            services.AddCors(corsOpts =>
            {
                corsOpts.AddPolicy("CorsPolicy",
                    corsBuilder => corsBuilder.WithOrigins(
                        this._configuration.GetValue<string>("Host:Cors")));
            });

            services.AddMvc(this._webApiOptions);
            services.AddAuthentication(this._webApiOptions);

            this._modulesStartup.ConfigureServices(services);

            services.ValidateDataLayerServices(this._logger);
            services.ValidateEmailSenderServices(this._logger);
            services.ValidateEventServices(this._logger);

            this._logger.LogInformation("WebAPI services configured.");

            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            this._logger.LogInformation("Configure WebAPI.");
            
            app.UseMiddleware<RequestIdMiddleware>();
            app.UseLogging();
            app.UseCors("CorsPolicy");            
            

            app.UseMvcWithDefaultRoute();

            this._modulesStartup.Configure(app);

            this._logger.LogInformation("WebAPI configured.");
        }
    }
}
