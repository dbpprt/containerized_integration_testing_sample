using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AvatarService.API.IntegrationTests.TestInfrastructure;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AvatarService.API.IntegrationTests
{
    public class AvatarControllerTests : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _testServer;

        public AvatarControllerTests(TestServerFixture testServer)
        {
            _testServer = testServer;
        }

        /// <summary>
        /// Very simple method to return a image from adorable.io - we need some samples for testing
        /// </summary>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        private static async Task<Stream> GetSampleImage(int dimensions)
        {
            // this is kind of bad practice to not use the client as a singleton but bear with me, its just a test
            using (var client = new HttpClient())
            {
                using (var responseStream =
                    await client.GetStreamAsync($"https://api.adorable.io/avatars/{dimensions}/{Guid.NewGuid()}"))
                {
                    var memoryStream = new MemoryStream();
                    // we copy the stream into a MemoryStream to make it seekable
                    await responseStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    return memoryStream;
                }
            }
        }

        /// <summary>
        /// Simple method to compare 2 streams bit by bit
        /// Thanks: https://gist.github.com/sebingel/447e2b86be27f6172bfe395b52d05c96
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool CompareStreams(Stream a, Stream b)
        {
            if (a == null &&
                b == null)
                return true;
            if (a == null ||
                b == null)
            {
                throw new ArgumentNullException(
                    a == null ? "a" : "b");
            }

            if (a.Length < b.Length)
                return false;
            if (a.Length > b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                int aByte = a.ReadByte();
                int bByte = b.ReadByte();
                if (aByte.CompareTo(bByte) != 0)
                    return false;
            }

            return true;
        }

        [Fact]
        public async Task uploading_an_image_should_create_a_blob_and_database_entry()
        {
            using (var client = _testServer.CreateHttpClient())
            using (var sampleImage = await GetSampleImage(2048))
            {
                var userId = Guid.NewGuid();

                var fileStreamContent = new StreamContent(sampleImage);
                fileStreamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "file", FileName = "file" };
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(fileStreamContent);
                    var response = await client.PostAsync($"/api/v1/avatars/{userId}", formData);
                    response.EnsureSuccessStatusCode();

                    response.StatusCode.Should().Be(HttpStatusCode.NoContent, "the api should return 204");

                    // we do not have a request scope so we create our own scope
                    using (var scope = _testServer.CreateScope())
                    // we want to reuse the database context to look whether the API call did what we expect
                    using (var databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>())
                    {
                        var blobContainer = scope.ServiceProvider.GetRequiredService<CloudBlobContainer>();

                        databaseContext.Avatars.Any(_ => _.UserId == userId).Should()
                            .BeTrue("the user should be generated");

                        var entity = await databaseContext.Avatars
                            .SingleAsync(_ => _.UserId == userId);

                        entity.OriginalSize.Should().Be(sampleImage.Length, "the file size should be set correctly");
                        entity.BlobReference.Should().NotBeNullOrEmpty("the blobReference should be set");

                        var blob = blobContainer.GetBlobReference(entity.BlobReference);

                        using (var memoryStream = new MemoryStream())
                        {
                            await blob.DownloadToStreamAsync(memoryStream);

                            // we need to set both stream positions to 0 in order to compare them
                            sampleImage.Position = 0;
                            memoryStream.Position = 0;

                            CompareStreams(sampleImage, memoryStream).Should().BeTrue("the file should not be changed");
                        }

                        var avatarBlob = blobContainer.GetBlobReference($"{entity.BlobReference}_avatar");
                        (await avatarBlob.ExistsAsync()).Should().BeTrue("the avatar should be stored in the blob storage");
                    }
                }
            }
        }
    }
}
