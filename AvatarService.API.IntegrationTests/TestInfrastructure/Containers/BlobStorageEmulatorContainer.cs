using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace AvatarService.API.IntegrationTests.TestInfrastructure.Containers
{
    public class BlobStorageEmulatorContainer : DockerContainerBase
    {
        // ReSharper disable StringLiteralTypo
        public static readonly string ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:21000/devstoreaccount1;TableEndpoint=http://127.0.0.1:21002/devstoreaccount1;QueueEndpoint=http://127.0.0.1:21001/devstoreaccount1;";
        // ReSharper restore StringLiteralTypo

        // ReSharper disable StringLiteralTypo
        public BlobStorageEmulatorContainer() 
            : base("arafato/azurite:latest", $"{ContainerPrefix}{Guid.NewGuid().ToString()}")
        // ReSharper restore StringLiteralTypo
        {
        }

        protected override async Task<bool> IsReady()
        {
            try
            {
                var container = CloudStorageAccount
                    .Parse(ConnectionString)
                    .CreateCloudBlobClient()
                    .GetContainerReference("isReady");
                await container.CreateIfNotExistsAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override HostConfig ToHostConfig()
        {
            return new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        "10000/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostPort = "21000",
                                HostIP = "127.0.0.1"
                            }
                        }
                    },
                    {
                        "10001/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostPort = "21001",
                                HostIP = "127.0.0.1"
                            }
                        }
                    },
                    {
                        "10002/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostPort = "21002",
                                HostIP = "127.0.0.1"
                            }
                        }
                    }
                }
            };
        }

        protected override Config ToConfig()
        {
            return new Config
            {
                Env = new List<string>()
            };
        }
    }
}