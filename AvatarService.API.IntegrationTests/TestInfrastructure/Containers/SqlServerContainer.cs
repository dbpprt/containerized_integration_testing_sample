using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace AvatarService.API.IntegrationTests.TestInfrastructure.Containers
{
    public class SqlServerContainer : DockerContainerBase
    {
        public static readonly string ConnectionString = "Server=127.0.0.1,21434;User Id=sa;Password=P@55w0rd;Timeout=5";

        public SqlServerContainer() 
            : base("microsoft/mssql-server-linux:latest", 
                $"{ContainerPrefix}{Guid.NewGuid().ToString()}")
        {
        }

        protected override async Task<bool> IsReady()
        {
            try
            {
                using (var conn =
                    new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    return true;
                }
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
                        "1433/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostPort = "21434",
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
                Env = new List<string> { "ACCEPT_EULA=Y", "SA_PASSWORD=P@55w0rd", "MSSQL_PID=Developer" }
            };
        }
    }
}