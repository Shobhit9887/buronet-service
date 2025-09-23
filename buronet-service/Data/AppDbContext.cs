// buronet_service/Data/AppDbContext.cs
using buronet_service.Data;
using buronet_service.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace buronet_service.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // All DbSets (assuming all corresponding models are correctly defined)
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

        //New DbSets for Connections
        public DbSet<Connection> Connections { get; set; } = null!;
        public DbSet<ConnectionRequest> ConnectionRequests { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the core User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // One-to-one relationship with UserProfile (shared PK)
                entity.HasOne(u => u.Profile)
                      .WithOne(up => up.User) // UserProfile's navigation property back to User
                      .HasForeignKey<UserProfile>(up => up.Id) // UserProfile.Id is the FK to User.Id
                      .IsRequired();
            });

            // Configure UserProfile entity (for its collections)
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id); // Explicitly define PK, though it's FK from User
                entity.Property(p => p.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(p => p.UpdatedAt).HasColumnName("UpdatedAt");

                // Configure relationships for all child entities to UserProfile
                // Ensure UserProfile has ICollection<T> properties for each of these
                entity.HasMany(up => up.Experiences)
                      .WithOne(ue => ue.UserProfile)
                      .HasForeignKey(ue => ue.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade); // If UserProfile is deleted, experiences are too

                entity.HasMany(up => up.Skills)
                      .WithOne(us => us.UserProfile)
                      .HasForeignKey(us => us.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(up => up.Education)
                      .WithOne(ue => ue.UserProfile)
                      .HasForeignKey(ue => ue.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(up => up.ExamAttempts)
                      .WithOne(uea => uea.UserProfile)
                      .HasForeignKey(uea => uea.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(up => up.Coaching)
                      .WithOne(uc => uc.UserProfile)
                      .HasForeignKey(uc => uc.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(up => up.Publications)
                      .WithOne(up => up.UserProfile) // Corrected: up.UserProfile for Publication entity
                      .HasForeignKey(up => up.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(up => up.Projects)
                      .WithOne(up => up.UserProfile) // Corrected: up.UserProfile for Project entity
                      .HasForeignKey(up => up.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(up => up.CommunityGroups)
                      .WithOne(ucg => ucg.UserProfile)
                      .HasForeignKey(ucg => ucg.UserProfileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            // Configure Post entity
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.Id); // Explicitly define PK for clarity
                entity.HasOne(p => p.User) // A Post has one User (creator)
                      .WithMany(u => u.Posts) // User entity MUST have `public ICollection<Post> Posts { get; set; }`
                      .HasForeignKey(p => p.UserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade); // If User is deleted, their posts are deleted
            });

            // Configure Comment entity
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id); // Explicitly define PK for clarity

                entity.HasOne(c => c.Post) // A Comment belongs to one Post
                      .WithMany(p => p.Comments) // A Post has many Comments
                      .HasForeignKey(c => c.PostId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade); // If Post is deleted, its comments are deleted

                entity.HasOne(c => c.User) // A Comment has one User (commenter)
                      .WithMany(u => u.Comments) // User entity MUST have `public ICollection<Comment> Comments { get; set; }`
                      .HasForeignKey(c => c.UserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.NoAction); // Prevent cascading delete on User
            });

            // Configure Like entity
            modelBuilder.Entity<Like>(entity =>
            {
                entity.HasKey(l => l.Id); // Explicitly define PK for clarity
                entity.HasIndex(l => new { l.PostId, l.UserId }).IsUnique(); // Ensure unique like per user per post

                entity.HasOne(l => l.Post) // A Like belongs to one Post
                      .WithMany(p => p.Likes) // A Post has many Likes
                      .HasForeignKey(l => l.PostId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade); // If Post is deleted, its likes are deleted

                entity.HasOne(l => l.User) // A Like has one User (liker)
                      .WithMany(u => u.Likes) // User entity MUST have `public ICollection<Like> Likes { get; set; }`
                      .HasForeignKey(l => l.UserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.NoAction); // Prevent cascading delete on User
            });

            // --- Configure relationships for Connections & ConnectionRequests entities ---

            modelBuilder.Entity<Connection>(entity =>
            {
                entity.HasKey(e => e.Id); // Explicitly define PK for clarity
                entity.HasIndex(c => new { c.UserId1, c.UserId2 }).IsUnique(); // Enforce unique connection pair

                entity.HasOne(c => c.User1)
                      .WithMany(u => u.ConnectionsMade) // User entity MUST have `public ICollection<Connection> ConnectionsMade { get; set; }`
                      .HasForeignKey(c => c.UserId1)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.User2)
                      .WithMany(u => u.ConnectionsReceived) // User entity MUST have `public ICollection<Connection> ConnectionsReceived { get; set; }`
                      .HasForeignKey(c => c.UserId2)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConnectionRequest>(entity =>
            {
                entity.HasKey(e => e.Id); // Explicitly define PK for clarity
                entity.HasIndex(cr => new { cr.SenderId, cr.ReceiverId }).IsUnique(); // Ensure unique pending request

                entity.HasOne(cr => cr.Sender)
                      .WithMany(u => u.SentConnectionRequests) // User entity MUST have `public ICollection<ConnectionRequest> SentConnectionRequests { get; set; }`
                      .HasForeignKey(cr => cr.SenderId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cr => cr.Receiver)
                      .WithMany(u => u.ReceivedConnectionRequests) // User entity MUST have `public ICollection<ConnectionRequest> ReceivedConnectionRequests { get; set; }`
                      .HasForeignKey(cr => cr.ReceiverId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity<TagFrequency>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.HasIndex(e => e.TagName).IsUnique(); // Ensure tag names are unique
                });
            });

            modelBuilder.Entity<Poll>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(p => p.Post)
                      .WithOne(post => post.Poll!)
                      .HasForeignKey<Poll>(p => p.PostId)
                      .IsRequired();
            });

            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(po => po.Poll)
                      .WithMany(p => p.Options)
                      .HasForeignKey(po => po.PollId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PollVote>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(v => new { v.PollId, v.UserId }).IsUnique(); // Ensure unique vote per user per poll

                entity.HasOne(v => v.Poll)
                      .WithMany()
                      .HasForeignKey(v => v.PollId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.PollOption)
                      .WithMany(po => po.PollVotes)
                      .HasForeignKey(v => v.PollOptionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}