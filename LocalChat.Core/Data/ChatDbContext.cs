using LocalChat.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalChat.Core.Data
{
    public class ChatDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Database=ChatBoxDb;Username=postgres;Password=123456");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MessageReaction>(entity =>
            {
                entity.HasIndex(e => new { e.MessageId, e.UserId, e.Emoji })
                    .IsUnique()
                    .HasDatabaseName("IX_MessageReactions_MessageId_UserId_Emoji");
            });
        }
    }
}
