using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Models;

namespace SallaAlertApp.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Optional: Map to specific table name if needed, or default to 'Merchants'
        modelBuilder.Entity<Merchant>().ToTable("Merchants");
    }
}
