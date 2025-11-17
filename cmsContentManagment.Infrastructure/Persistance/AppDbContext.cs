using cmsContentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace cmsContentManagement.Infrastructure.Persistance;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public virtual DbSet<Content> Contents { get; set; }

}
