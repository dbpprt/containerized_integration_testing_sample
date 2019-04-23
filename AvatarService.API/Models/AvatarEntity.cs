using System;
using System.ComponentModel.DataAnnotations;

namespace AvatarService.API.Models
{
    public class AvatarEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public long OriginalSize { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public string BlobReference { get; set; }
    }
}
