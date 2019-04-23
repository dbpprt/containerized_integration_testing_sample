namespace AvatarService.API.Common
{
    public class ServiceConfiguration
    {
        public string DatabaseConnectionString { get; set; }

        public string BlobStorageConnectionString { get; set; }

        public string BlobStorageContainerName { get; set; }

        public bool ApplyMigrationsOnStartup { get; set; }
    }
}
