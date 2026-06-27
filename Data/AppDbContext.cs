using Microsoft.EntityFrameworkCore;
using UserPanelApp.Models;

namespace UserPanelApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<UserNote> UserNotes => Set<UserNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<UserNote>()
            .HasOne(note => note.AppUser)
            .WithMany(user => user.Notes)
            .HasForeignKey(note => note.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}