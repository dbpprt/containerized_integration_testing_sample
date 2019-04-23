using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarService.API.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AvatarService.API
{
    public class Startup
    {
        private readonly IOptions<ServiceConfiguration> _serviceConfiguration;

        public Startup(IOptions<ServiceConfiguration> serviceConfiguration)
        {
            _serviceConfiguration = serviceConfiguration;
        }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DatabaseContext>(
                options => options.UseSqlServer(_serviceConfiguration.Value.DatabaseConnectionString));

            services.AddScoped(_ => CloudStorageAccount
                .Parse(_serviceConfiguration.Value.BlobStorageConnectionString)
                .CreateCloudBlobClient()
                .GetContainerReference(_serviceConfiguration.Value.BlobStorageContainerName));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public static void ConfigureConfigurationSections(WebHostBuilderContext context, IServiceCollection services)
        {
            services.Configure<ServiceConfiguration>(context.Configuration.GetSection("ServiceConfiguration"));
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (_serviceConfiguration.Value.ApplyMigrationsOnStartup)
            {
                using (var serviceScope =
                    app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var databaseContext = serviceScope.ServiceProvider.GetService<DatabaseContext>())
                {
                    databaseContext.Database.MigrateAsync().GetAwaiter().GetResult();
                }
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                // it is very important to not use the https redirection middleware when executing
                // integration tests -> it will fail!
                app.UseHttpsRedirection();
            }

            app.UseMvc();
        }
    }
}
