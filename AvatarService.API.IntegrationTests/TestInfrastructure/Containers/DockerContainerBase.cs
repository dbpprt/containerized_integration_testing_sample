using System;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions.Extensions;

namespace AvatarService.API.IntegrationTests.TestInfrastructure.Containers
{
    // credits to https://www.meziantou.net/2018/10/08/integration-testing-using-a-docker-container
    public abstract class DockerContainerBase
    {
        private enum ContainerStartAction
        {
            None,
            Started,
            External
        }

        protected const string ContainerPrefix = "integration-testing-";

        protected DockerContainerBase(string imageName, string containerName)
        {
            ImageName = imageName;
            ContainerName = containerName;
        }

        private string ImageName { get; }

        private string ContainerName { get; }

        private ContainerStartAction StartAction { get; set; } = ContainerStartAction.None;

        public async Task Start(IDockerClient client)
        {
            if (StartAction != ContainerStartAction.None) return;

            var images =
                await client.Images.ListImagesAsync(new ImagesListParameters { MatchName = ImageName });

            if (images.Count == 0)
            {
                await client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = ImageName, Tag = "latest" }, null,
                    new Progress<JSONMessage>());
            }

            var list = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var container = list.FirstOrDefault(x => x.Names.Contains("/" + ContainerName));

            if (container == null)
            {
                await CreateContainer(client);
            }
            else
            {
                if (container.State == "running")
                {
                    StartAction = ContainerStartAction.External;
                    return;
                }
            }


            var started = await client.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters());
            if (!started) throw new InvalidOperationException($"Container '{ContainerName}' did not start!!!!");

            var i = 0;
            while (!await IsReady())
            {
                i++;

                if (i > 20)
                    throw new TimeoutException(
                        $"Container {ContainerName} does not seem to be responding in a timely manner");

                await Task.Delay(5.Seconds());
            }

            StartAction = ContainerStartAction.Started;
        }

        private async Task CreateContainer(IDockerClient client)
        {
            var hostConfig = ToHostConfig();
            var config = ToConfig();

            await client.Containers.CreateContainerAsync(new CreateContainerParameters(config)
            {
                Image = ImageName,
                Name = ContainerName,
                Tty = true,
                HostConfig = hostConfig
            });
        }

        public Task Remove(IDockerClient client)
        {
            return client.Containers.RemoveContainerAsync(ContainerName,
                new ContainerRemoveParameters { Force = true, RemoveVolumes = true });
        }

        protected abstract Task<bool> IsReady();

        protected abstract HostConfig ToHostConfig();

        protected abstract Config ToConfig();

        public override string ToString()
        {
            return $"{nameof(ImageName)}: {ImageName}, {nameof(ContainerName)}: {ContainerName}";
        }

        public static async Task CleanupOrphanedContainers(DockerClient dockerClient)
        {
            var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var orphanedContainers = containers.Where(_ => _.Names.Any(__ => __.Contains(ContainerPrefix)));

            foreach (var container in orphanedContainers)
            {
                await dockerClient.Containers.RemoveContainerAsync(container.ID,
                    new ContainerRemoveParameters { Force = true, RemoveVolumes = true });
            }
        }
    }
}