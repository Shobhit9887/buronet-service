using Microsoft.EntityFrameworkCore;
using buronet_messaging_service.Models;
using buronet_messaging_service.Models.Users; // Important: Reference the User model
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace buronet_messaging_service.Data
{
    public class MessagingDbContext : DbContext
    {
        public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options) { }

        // DbSet properties for your messaging entities
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<UserExperience> UserExperiences { get; set; } = null!;
        public DbSet<UserSkill> UserSkills { get; set; } = null!;
        public DbSet<UserEducation> UserEducation { get; set; } = null!;
        public DbSet<UserExamAttempt> UserExamAttempts { get; set; } = null!;
        public DbSet<UserCoaching> UserCoaching { get; set; } = null!;
        public DbSet<UserPublication> UserPublications { get; set; } = null!;
        public DbSet<UserProject> UserProjects { get; set; } = null!;
        public DbSet<UserCommunityGroup> UserCommunityGroups { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Like> Likes { get; set; } = null!;
        public DbSet<TagFrequency> TagFrequencies { get; set; } = null!;
        public DbSet<PollVote> PollVotes { get; set; } = null!;
        public DbSet<Poll> Polls { get; set; } = null!;
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ApplyUtcDateTimeConverters(modelBuilder);

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

        private static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
        {
            var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
                toDb => DateTime.SpecifyKind(
                    (toDb.Kind == DateTimeKind.Utc) ? toDb : toDb.ToUniversalTime(),
                    DateTimeKind.Unspecified),
                fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));

            var utcNullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                toDb => toDb.HasValue
                    ? DateTime.SpecifyKind(
                        (toDb.Value.Kind == DateTimeKind.Utc) ? toDb.Value : toDb.Value.ToUniversalTime(),
                        DateTimeKind.Unspecified)
                    : null,
                fromDb => fromDb.HasValue
                    ? DateTime.SpecifyKind(fromDb.Value, DateTimeKind.Utc)
                    : null);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcDateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(utcNullableDateTimeConverter);
                    }
                }
            }
        }
    }
}
