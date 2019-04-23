using AvatarService.API.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace AvatarService.API.IntegrationTests.TestInfrastructure
{
    public sealed class TestStartup : Startup
    {
        public TestStartup(IOptions<ServiceConfiguration> settings)
            : base(settings)
        {
        }

        // You can easily modify the application startup by overriding Configure or ConfigureServices
        public override void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            base.Configure(app, env);
        }
    }
}