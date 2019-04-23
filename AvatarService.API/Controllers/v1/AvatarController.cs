using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AvatarService.API.Common;
using AvatarService.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;

namespace AvatarService.API.Controllers.v1
{
    [ApiController]
    [Route("api/v1/avatars")]
    public class AvatarController : ControllerBase
    {
        private readonly DatabaseContext _databaseContext;
        private readonly CloudBlobContainer _blobContainer;

        public AvatarController(DatabaseContext databaseContext, CloudBlobContainer blobContainer)
        {
            // this is just an example, never place business logic in your controllers ;)
            _databaseContext = databaseContext;
            _blobContainer = blobContainer;
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> Post([FromForm] IFormFile file, Guid userId, CancellationToken cancellationToken)
        {
            if (file == null || !ModelState.IsValid)
            {
                return BadRequest();
            }

            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                memoryStream.Position = 0;

                await _blobContainer.CreateIfNotExistsAsync(cancellationToken);

                var blobName = Guid.NewGuid().ToString();

                // the logic is quite simple: we're saving the original file and the processed file
                var originalImageBlob = _blobContainer.GetBlockBlobReference(blobName);
                await originalImageBlob.UploadFromStreamAsync(memoryStream, cancellationToken);

                // transform the image
                var avatar = ImageHelper.ToAvatar(memoryStream.ToArray());
                var avatarImageBlob = _blobContainer.GetBlockBlobReference($"{blobName}_avatar");
                await avatarImageBlob.UploadFromByteArrayAsync(avatar, 0, avatar.Length, cancellationToken);

                _databaseContext.Avatars.Add(new AvatarEntity
                {
                    BlobReference = blobName,
                    Created = DateTime.UtcNow,
                    Id = Guid.NewGuid(),
                    OriginalSize = memoryStream.Length,
                    UserId = userId
                });

                await _databaseContext.SaveChangesAsync(cancellationToken);
            }

            return NoContent();
        }
    }
}
