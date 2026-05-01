using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace buronet_service.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.CreateTable(
                name: "TagFrequencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        ,
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagFrequencies", x => x.Id);
                })
                ;

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        ,
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    isAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                ;

            migrationBuilder.CreateTable(
                name: "ConnectionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        ,
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionRequests_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                ;

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId2 = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Connections_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Users_UserId2",
                        column: x => x.UserId2,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                ;

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    Content = table.Column<string>(type: "text", nullable: false)
                        ,
                    ImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        ,
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPoll = table.Column<bool>(type: "boolean", nullable: false),
                    PollId = table.Column<int>(type: "integer", nullable: true),
                    TagsJson = table.Column<string>(type: "text", nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        ,
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        ,
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        ,
                    AddressLine1 = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    AddressLine2 = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        ,
                    StateProvince = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        ,
                    ZipCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        ,
                    Country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        ,
                    ProfilePictureUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        ,
                    Bio = table.Column<string>(type: "text", nullable: true)
                        ,
                    Headline = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                        ,
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                ;

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Likes_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Likes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                ;

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Polls_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserCoaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachingInstitute = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    CourseName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCoaching", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCoaching_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserCommunityGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    Description = table.Column<string>(type: "text", nullable: true)
                        ,
                    Url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCommunityGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCommunityGroups_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserEducation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Degree = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    Major = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    Institution = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEducation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEducation_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserExamAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Result = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        ,
                    Remarks = table.Column<string>(type: "text", nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExamAttempts_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserExperiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    Organization = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExperiences_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        ,
                    Description = table.Column<string>(type: "text", nullable: true)
                        ,
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProjects_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserPublications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        ,
                    JournalConference = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        ,
                    PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        ,
                    Abstract = table.Column<string>(type: "text", nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPublications_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "UserSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        ,
                    Level = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSkills_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "PollOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PollId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                        
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollOptions_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateTable(
                name: "PollVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PollId = table.Column<int>(type: "integer", nullable: false),
                    PollOptionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollVotes_PollOptions_PollOptionId",
                        column: x => x.PollOptionId,
                        principalTable: "PollOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PollVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                ;

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId",
                table: "Comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_ReceiverId",
                table: "ConnectionRequests",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionRequests_SenderId_ReceiverId",
                table: "ConnectionRequests",
                columns: new[] { "SenderId", "ReceiverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_UserId1_UserId2",
                table: "Connections",
                columns: new[] { "UserId1", "UserId2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_UserId2",
                table: "Connections",
                column: "UserId2");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_PostId_UserId",
                table: "Likes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId",
                table: "Likes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollOptions_PollId",
                table: "PollOptions",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_PostId",
                table: "Polls",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_PollId_UserId",
                table: "PollVotes",
                columns: new[] { "PollId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_PollOptionId",
                table: "PollVotes",
                column: "PollOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_UserId",
                table: "PollVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TagFrequencies_TagName",
                table: "TagFrequencies",
                column: "TagName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCoaching_UserProfileId",
                table: "UserCoaching",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCommunityGroups_UserProfileId",
                table: "UserCommunityGroups",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEducation_UserProfileId",
                table: "UserEducation",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExamAttempts_UserProfileId",
                table: "UserExamAttempts",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExperiences_UserProfileId",
                table: "UserExperiences",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProjects_UserProfileId",
                table: "UserProjects",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPublications_UserProfileId",
                table: "UserPublications",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_UserProfileId",
                table: "UserSkills",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ConnectionRequests");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropTable(
                name: "PollVotes");

            migrationBuilder.DropTable(
                name: "TagFrequencies");

            migrationBuilder.DropTable(
                name: "UserCoaching");

            migrationBuilder.DropTable(
                name: "UserCommunityGroups");

            migrationBuilder.DropTable(
                name: "UserEducation");

            migrationBuilder.DropTable(
                name: "UserExamAttempts");

            migrationBuilder.DropTable(
                name: "UserExperiences");

            migrationBuilder.DropTable(
                name: "UserProjects");

            migrationBuilder.DropTable(
                name: "UserPublications");

            migrationBuilder.DropTable(
                name: "UserSkills");

            migrationBuilder.DropTable(
                name: "PollOptions");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

