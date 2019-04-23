using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AvatarService.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(Startup.ConfigureConfigurationSections)
                .UseStartup<Startup>();
    }
}
