﻿using Microsoft.EntityFrameworkCore;
using buronet_messaging_service.Models;
using buronet_service.Models.User; // Important: Reference the User model

namespace buronet_messaging_service.Data
{
    public class MessagingDbContext : DbContext
    {
        public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options) { }

        // DbSet properties for your messaging entities
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users"); // Map to the existing "Users" table (plural)
                entity.HasKey(u => u.Id); // FIX: Define the key 
            });

            // Configure composite primary key for ConversationParticipant
            modelBuilder.Entity<ConversationParticipant>()
                .HasKey(cp => new { cp.ConversationId, cp.UserId });

            // Configure relationships for ConversationParticipant
            modelBuilder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade); // If conversation is deleted, participants are too

            // Configure relationship between ConversationParticipant and User
            // This is crucial for EF Core to understand the foreign key to an entity in another DbContext/project.
            // It relies on the project reference you added earlier.
            modelBuilder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.User)
                .WithMany() // User can have many conversation participations (no direct collection on User)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete on User if participant is removed

            // Configure relationships for Message
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade); // If conversation is deleted, messages are too

            // Configure relationship between Message and Sender (User)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany() // User can send many messages (no direct collection on User)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete on User if message is removed
        }
    }
}
