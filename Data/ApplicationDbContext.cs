using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LimeChat.Models;

namespace LimeChat.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public DbSet<Friend> Friends { get; set; }


        public DbSet<UserInGroup> UserInGroups { get; set; }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.User1)
                .WithMany()
                .HasForeignKey(f => f.User1_Id)
                .OnDelete(DeleteBehavior.Restrict); // or DeleteBehavior.NoAction

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.User2)
                .WithMany()
                .HasForeignKey(f => f.User2_Id)
                .OnDelete(DeleteBehavior.Restrict); // or DeleteBehavior.NoAction
        }

    }
}