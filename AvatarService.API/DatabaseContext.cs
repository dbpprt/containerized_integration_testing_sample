using AvatarService.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AvatarService.API
{
    public class DatabaseContext : DbContext
    {
        public DbSet<AvatarEntity> Avatars { get; set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) 
            : base(options) { }
    }
}
