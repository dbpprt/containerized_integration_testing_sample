using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using AvatarService.API.IntegrationTests.TestInfrastructure.Containers;
using Docker.DotNet;
using FluentAssertions.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace AvatarService.API.IntegrationTests.TestInfrastructure
{
    [UsedImplicitly]
    public class TestServerFixture : IDisposable
    {
        private const string RelativePathToHostProject = @"..\..\..\..\AvatarService.API";

        private readonly BlobStorageEmulatorContainer _blobStorageEmulatorContainer;
        private readonly DockerClient _dockerClient;
        private readonly SqlServerContainer _sqlServerContainer;

        private readonly TestServer _testServer;

        public TestServerFixture()
        {
            _dockerClient = new DockerClientConfiguration(
                    // TODO: This needs to be configurable in order to execute tests in CI
                    new Uri("npipe://./pipe/docker_engine"))
                .CreateClient();

            var testProjectPath = PlatformServices.Default.Application.ApplicationBasePath;
            var contentRoot = Path.Combine(testProjectPath, RelativePathToHostProject);

            DockerContainerBase.CleanupOrphanedContainers(_dockerClient).Wait(300.Seconds());

            _sqlServerContainer = new SqlServerContainer();
            _sqlServerContainer.Start(_dockerClient).Wait(300.Seconds());

            _blobStorageEmulatorContainer = new BlobStorageEmulatorContainer();
            _blobStorageEmulatorContainer.Start(_dockerClient).Wait(300.Seconds());

            var builder = WebHost.CreateDefaultBuilder()
                .UseContentRoot(contentRoot)
                .UseEnvironment("Development")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // we need to replace a bunch of settings to bind the API to our Testing environment
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {"ServiceConfiguration:DatabaseConnectionString", SqlServerContainer.ConnectionString},
                        {
                            "ServiceConfiguration:BlobStorageConnectionString",
                            BlobStorageEmulatorContainer.ConnectionString
                        }
                    });
                })
                .ConfigureServices(Startup.ConfigureConfigurationSections)
                .UseStartup<TestStartup>()
                .ConfigureTestServices(ModifyServicesForTesting)
                // thanks to https://github.com/aspnet/Hosting/issues/903
                .UseSetting(WebHostDefaults.ApplicationKey,
                    typeof(Program).GetTypeInfo().Assembly
                        .FullName); // Ignore the startup class assembly as the "entry point" and instead point it to this app

            _testServer = new TestServer(builder);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     This is the "RegisterServices" method of the Test project. Usually we need to replace a bunch of services here
        /// </summary>
        /// <param name="services">The IServiceCollection for the target application</param>
        private void ModifyServicesForTesting(IServiceCollection services)
        {
            // we need to replace mailjet to prevent random messages going out while performing integration tests
            
            //var mailjetClientMock = new Mock<IMailjetClient>();
            //mailjetClientMock.Setup(_ => _.PostAsync(It.IsAny<MailjetRequest>())).ReturnsAsync(
            //    new MailjetResponse(true, 200, null)
            //);
        }

        /// <summary>
        ///     This method returns a HttpClient which targets the TestServer instance, please not use BaseUrl
        /// </summary>
        /// <returns>Anonymous HttpClient instance bound to the TestServer</returns>
        public HttpClient CreateHttpClient()
        {
            return _testServer.CreateClient();
        }

        /// <summary>
        ///     Obtains a new dependency scope which needs to be disposed after usage
        /// </summary>
        /// <returns>IServiceScope which contains all registrations of the API</returns>
        public IServiceScope CreateScope()
        {
            return _testServer.Host.Services.CreateScope();
        }

        private void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (!itIsSafeToAlsoFreeManagedObjects) return;

            // remove containers
            _sqlServerContainer.Remove(_dockerClient).Wait(300.Seconds());
            _blobStorageEmulatorContainer.Remove(_dockerClient).Wait(300.Seconds());

            _testServer.Dispose();
            _dockerClient.Dispose();
        }

        ~TestServerFixture()
        {
            Dispose(false);
        }
    }
}