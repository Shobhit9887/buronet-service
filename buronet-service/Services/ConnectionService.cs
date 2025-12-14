using AutoMapper;
using AutoMapper.QueryableExtensions; // For ProjectTo
using buronet_service.Data; // Your DbContext
using buronet_service.Models.DTOs.User; // DTOs
using buronet_service.Models.User; // Entities
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver.Core.Servers;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations
using System.Threading.Tasks;

namespace buronet_service.Services // Ensure this namespace is correct
{
    public class ConnectionService : IConnectionService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;

        public ConnectionService(AppDbContext context, IMapper mapper, IHttpClientFactory httpClientFactory, IUserService userService   )
        {
            _context = context;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
        }

        private async Task SendNotificationToService(Guid userId, string title, string message, string type, string redirectUrl, string? targetId = null)
        {
            // Implementation of this method goes here (using IHttpClientFactory)
            var client = _httpClientFactory.CreateClient("NotificationsService");
            var notificationPayload = new { UserId = userId, Title = title, Message = message, Type = type, RedirectUrl = redirectUrl, TargetId = targetId };

            try
            {
                await client.PostAsJsonAsync("/api/notifications/internal-create", notificationPayload);
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        // Helper to enforce canonical order for connections (UserId1 < UserId2 alphabetically)
        private (Guid user1Id, Guid user2Id) GetCanonicalConnectionPair(Guid userid1, Guid userid2)
        {
            string id1 = userid1.ToString();
            string id2 = userid2.ToString();
            return string.Compare(id1, id2, StringComparison.Ordinal) < 0 ? (userid1, userid2) : (userid2, userid1);
        }

        // --- Network Metrics ---
        //public async Task<NetworkMetricsDto> GetNetworkMetricsAsync(Guid userIdGuid)
        //{
        //    var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

        //    string userIdString = userIdGuid.ToString();

        //    var totalConnections = await _context.Connections
        //                                        .CountAsync(c => c.UserId1 == userIdGuid || c.UserId2 == userIdGuid);

        //    var joinedGroups = await _context.UserCommunityGroups // UserCommunityGroups is a user's joined groups
        //                                    .CountAsync(ucg => ucg.UserProfileId == userIdGuid);

        //    var pendingRequests = await _context.ConnectionRequests
        //                                        .CountAsync(cr => cr.ReceiverId == userIdGuid && cr.Status == "Pending");

        //    var networkGrowth = await _context.Connections
        //                                        .CountAsync(ng => (ng.UserId1 == userIdGuid || ng.UserId2 == userIdGuid) && ng.CreatedAt >= oneMonthAgo);

        //    // Network Growth Percentage is a placeholder for now, would need historical data
        //    return new NetworkMetricsDto
        //    {
        //        TotalConnections = totalConnections,
        //        JoinedGroups = joinedGroups,
        //        PendingRequests = pendingRequests,
        //        NetworkGrowthPercentage = 0.0, // Placeholder
        //        NetworkGrowth = networkGrowth
        //    };
        //}

        public async Task<NetworkMetricsDto> GetNetworkMetricsAsync(Guid userIdGuid)
        {
            // Define time frames for comparison
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var lastMonthStart = DateTime.UtcNow.AddMonths(-2); // For previous month's connections
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

            // 1. Total Connections and its trend vs. 1 month ago
            var totalConnections = await _context.Connections
                                                 .CountAsync(c => c.UserId1 == userIdGuid || c.UserId2 == userIdGuid);

            var totalConnectionsLastMonth = await _context.Connections
                                                          .CountAsync(c => (c.UserId1 == userIdGuid || c.UserId2 == userIdGuid) && c.CreatedAt < oneMonthAgo);

            double totalConnectionsTrend = 0.0;
            if (totalConnectionsLastMonth > 0)
            {
                totalConnectionsTrend = ((double)totalConnections - totalConnectionsLastMonth) / totalConnectionsLastMonth * 100;
            } else
            {
                totalConnectionsTrend = 100;
            }

                // 2. Joined Groups in the current month
                var newGroupsThisMonth = await _context.UserCommunityGroups
                                                       .CountAsync(ucg => ucg.UserProfileId == userIdGuid && ucg.CreatedAt >= oneMonthAgo);

            // 3. New Pending Requests in the last 1 week
            var newPendingRequestsThisWeek = await _context.ConnectionRequests
                                                           .CountAsync(cr => cr.ReceiverId == userIdGuid && cr.Status == "Pending" && cr.CreatedAt >= oneWeekAgo);

            var pendingRequestsThisWeekTrend = await _context.ConnectionRequests
                                                              .CountAsync(cr => cr.ReceiverId == userIdGuid && cr.Status == "Pending" && cr.CreatedAt < oneWeekAgo);

            double pendingRequestsTrend = 0.0;
            if (pendingRequestsThisWeekTrend > 0)
            {
                pendingRequestsTrend = ((double)newPendingRequestsThisWeek - pendingRequestsThisWeekTrend) / pendingRequestsThisWeekTrend * 100;
            }
            else if (newPendingRequestsThisWeek > 0)
            {
                pendingRequestsTrend = 100.0;
            }

            // 4. Network Growth (new connections) and its trend vs. 1 month ago
            var networkGrowthCurrentMonth = await _context.Connections
                                                          .CountAsync(c => (c.UserId1 == userIdGuid || c.UserId2 == userIdGuid) && c.CreatedAt >= oneMonthAgo);

            var networkGrowthPreviousMonth = await _context.Connections
                                                           .CountAsync(c => (c.UserId1 == userIdGuid || c.UserId2 == userIdGuid) && c.CreatedAt >= lastMonthStart && c.CreatedAt < oneMonthAgo);

            double networkGrowthPercentage = 0.0;
            if (networkGrowthPreviousMonth > 0)
            {
                networkGrowthPercentage = ((double)networkGrowthCurrentMonth - networkGrowthPreviousMonth) / networkGrowthPreviousMonth * 100;
            }
            else if (networkGrowthCurrentMonth > 0)
            {
                networkGrowthPercentage = 100.0;
            }

            return new NetworkMetricsDto
            {
                TotalConnections = totalConnections,
                TotalConnectionsTrend = totalConnectionsTrend,
                JoinedGroups = newGroupsThisMonth,
                JoinedGroupsTrend = (newGroupsThisMonth > 0) ? 100.0 : 0.0, // Assuming 100% growth if there were no groups last month
                PendingRequests = newPendingRequestsThisWeek,
                PendingRequestsTrend = pendingRequestsTrend,
                NetworkGrowth = networkGrowthCurrentMonth,
                NetworkGrowthPercentage = networkGrowthPercentage
            };
        }

        // --- User Discovery & Connections ---

        public async Task<IEnumerable<UserCardDto>> GetDiscoverUsersAsync(Guid currentUserIdGuid)
        {
            string currentUserIdString = currentUserIdGuid.ToString();

            // Fetch all users except the current one
            var allOtherUsers = await _context.Users
                                              .Include(u => u.Profile)
                                              .Where(u => u.Id != currentUserIdGuid)
                                              .ToListAsync();

            // Get current user's existing connections and pending requests
            var userConnections = await _context.Connections
                                                .Where(c => c.UserId1 == currentUserIdGuid || c.UserId2 == currentUserIdGuid)
                                                .Select(c => c.UserId1 == currentUserIdGuid ? c.UserId2 : c.UserId1)
                                                .ToListAsync();

            var sentRequests = await _context.ConnectionRequests
                                             .Where(cr => cr.SenderId == currentUserIdGuid && cr.Status == "Pending")
                                             .Select(cr => cr.ReceiverId)
                                             .ToListAsync();

            var receivedRequests = await _context.ConnectionRequests
                                                 .Where(cr => cr.ReceiverId == currentUserIdGuid && cr.Status == "Pending")
                                                 .Select(cr => cr.SenderId)
                                                 .ToListAsync();

            var discoverUserCards = new List<UserCardDto>();

            foreach (var user in allOtherUsers)
            {
                var userCard = _mapper.Map<UserCardDto>(user);

                userCard.IsConnected = userConnections.Contains(user.Id);
                userCard.HasPendingRequestFromCurrentUser = sentRequests.Contains(user.Id);
                userCard.HasPendingRequestToCurrentUser = receivedRequests.Contains(user.Id);

                // Placeholder for mutual connections (complex calculation, typically done with graph traversal)
                userCard.MutualConnectionsCount = 0; // For now

                // You might add logic here for "Popular in your network" (e.g., users from same city, industry, etc.)
                // For now, this just lists all other users with connection status.
                discoverUserCards.Add(userCard);
            }

            // Order: prioritize users not connected/requested, then by mutual connections (if implemented), then alphabetically
            return discoverUserCards.OrderByDescending(u => u.MutualConnectionsCount) // Put users with mutual connections first (if > 0)
                                    .ThenBy(u => u.Username); // Alphabetical
        }


        public async Task<IEnumerable<ConnectionDto>> GetUserConnectionsAsync(Guid userIdGuid)
        {
            string userIdString = userIdGuid.ToString();

            var connections = await _context.Connections
                                            .Include(c => c.User1) // Include both users to get their data
                                            .Include(c => c.User2)
                                            .Include(c => c.User1.Profile) // And their profiles
                                            .Include(c => c.User2.Profile)
                                            .Where(c => c.UserId1 == userIdGuid || c.UserId2 == userIdGuid)
                                            .ToListAsync();

            var connectionDtos = new List<ConnectionDto>();
            foreach (var connection in connections)
            {
                var connectedUser = (connection.UserId1 == userIdGuid) ? connection.User2 : connection.User1;
                var connectedUserProfile = (connection.UserId1 == userIdGuid) ? connection.User2.Profile : connection.User1.Profile;

                if (connectedUser == null || connectedUserProfile == null) continue; // Should not happen with Includes

                connectionDtos.Add(new ConnectionDto
                {
                    Id = connection.Id, // ID of the connection record
                    ConnectedUserId = connectedUser.Id,
                    ConnectedUserName = connectedUser.Username,
                    ConnectedUserHeadline = connectedUserProfile.Headline ?? string.Empty,
                    ConnectedUserProfilePictureId = connectedUserProfile.ProfilePictureMediaId,
                    CreatedAt = connection.CreatedAt,
                    ConnectedUser = _mapper.Map<UserProfileDto>(connectedUserProfile) // Map to UserDto
                });
            }
            return connectionDtos.OrderByDescending(c => c.CreatedAt);
        }

        public async Task<IEnumerable<ConnectionRequestDto>> GetPendingConnectionRequestsAsync(Guid receiverIdGuid)
        {
            string receiverIdString = receiverIdGuid.ToString();

            var pendingRequests = await _context.ConnectionRequests
                                                .Include(cr => cr.Sender)
                                                .Include(cr => cr.Sender.Profile)
                                                .Where(cr => cr.ReceiverId == receiverIdGuid && cr.Status == "Pending")
                                                .ToListAsync();

            return _mapper.Map<List<ConnectionRequestDto>>(pendingRequests);
        }

        // --- Connection Management ---

        public async Task<ConnectionRequestDto?> SendConnectionRequestAsync(Guid senderIdGuid, SendConnectionRequestDto sendDto)
        {
            string senderIdString = senderIdGuid.ToString();
            string receiverIdString = sendDto.ReceiverId.ToString();

            // Prevent self-connection requests
            if (senderIdString == receiverIdString)
            {
                throw new ApplicationException("Cannot send a connection request to yourself.");
            }

            // Ensure receiver exists
            var receiverExists = await _context.Users.AnyAsync(u => u.Id == sendDto.ReceiverId);
            if (!receiverExists)
            {
                throw new ApplicationException("Receiver user not found.");
            }

            // Check if already connected
            var (u1, u2) = GetCanonicalConnectionPair(senderIdGuid, sendDto.ReceiverId);
            var alreadyConnected = await _context.Connections.AnyAsync(c => c.UserId1 == u1 && c.UserId2 == u2);
            if (alreadyConnected)
            {
                throw new ApplicationException("You are already connected to this user.");
            }

            // Check for existing pending request (either way)
            var existingRequest = await _context.ConnectionRequests
                                                .FirstOrDefaultAsync(cr =>
                                                    (cr.SenderId == senderIdGuid && cr.ReceiverId == sendDto.ReceiverId && cr.Status == "Pending") ||
                                                    (cr.SenderId == sendDto.ReceiverId && cr.ReceiverId == senderIdGuid && cr.Status == "Pending"));
            if (existingRequest != null)
            {
                throw new ApplicationException("A pending connection request already exists with this user.");
            }

            var newRequest = new ConnectionRequest
            {
                SenderId = senderIdGuid,
                ReceiverId = sendDto.ReceiverId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ConnectionRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            // Fetch sender details for the DTO
            var senderUser = await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == senderIdGuid);
            var receiverUser = await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == sendDto.ReceiverId);

            var newRequestDto = _mapper.Map<ConnectionRequestDto>(newRequest);
            // Manually populate SenderName/Headline as mapper might not handle recursive includes without ProjectTo
            if (senderUser != null)
            {
                //newRequestDto.SenderName = senderUser.Username;
                //newRequestDto.SenderHeadline = senderUser.Profile?.Headline;
                //newRequestDto.SenderProfilePictureUrl = senderUser.Profile?.ProfilePictureUrl;
                newRequestDto.Sender = new UserDto
                {
                    Id = senderUser.Id,
                    Username = senderUser.Username,
                    Email = senderUser.Email,
                };
            }
            if (receiverUser != null)
            {
                newRequestDto.Receiver = new UserDto
                {
                    Id = receiverUser.Id,
                    Username = receiverUser.Username,
                    Email = receiverUser.Email,
                };
            }

            if (senderUser != null)
            {
                await SendNotificationToService(
                    userId: receiverUser.Id, // Notify the RECEIVER
                    title: "New Connection Request",
                    message: $"{senderUser.Profile.FirstName} {senderUser.Profile.LastName} sent you a connection request.",
                    type: "ConnectionRequestReceived",
                    redirectUrl: "/network/requests",
                    targetId: senderUser.Id.ToString() // Target is the sender's ID
                );
            }

            return newRequestDto;
        }

        public async Task<bool> UpdateConnectionRequestStatusAsync(Guid receiverIdGuid, int requestId, UpdateConnectionRequestStatusDto updateDto)
        {
            string receiverIdString = receiverIdGuid.ToString();

            var request = await _context.ConnectionRequests
                                        .Include(cr => cr.Sender) // Include Sender to check if already connected after accept
                                        .Include(cr => cr.Receiver) // Include Receiver
                                        .FirstOrDefaultAsync(cr => cr.Id == requestId && cr.ReceiverId == receiverIdGuid && cr.Status == "Pending");

            if (request == null)
            {
                throw new ApplicationException("Connection request not found or not pending for this user.");
            }

            if (updateDto.Status == ConnectionRequestStatus.Accepted.ToString())
            {
                // Create new connection
                var (u1, u2) = GetCanonicalConnectionPair(request.SenderId, request.ReceiverId);
                var newConnection = new Connection
                {
                    UserId1 = u1,
                    UserId2 = u2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Connections.Add(newConnection);
                request.Status = ConnectionRequestStatus.Accepted.ToString();

                if (request == null) return false;

                var accepterUser = await _context.UserProfiles.FindAsync(request.ReceiverId);

                if (accepterUser != null)
                {
                    await SendNotificationToService(
                        userId: request.SenderId, // Notify the SENDER
                        title: "Connection Accepted!",
                        message: $"{accepterUser.FirstName} {accepterUser.LastName} accepted your connection request.",
                        type: "ConnectionAccepted",
                        redirectUrl: $"/profile/{accepterUser.Id}",
                        targetId: accepterUser.Id.ToString() // Target is the accepter's ID
                    );
                }
            }
            else if (updateDto.Status == ConnectionRequestStatus.Rejected.ToString())
            {
                request.Status = ConnectionRequestStatus.Rejected.ToString();
            }
            else
            {
                throw new ArgumentException("Invalid status for connection request update.");
            }

            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveConnectionAsync(Guid currentUserIdGuid, Guid connectedUserId)
        {
            //    string currentUserIdString = currentUserIdGuid.ToString();
            //    string connectedUserIdString = connectedUserId.ToString();

            // Find the canonical pair for the connection
            var (u1, u2) = GetCanonicalConnectionPair(currentUserIdGuid, connectedUserId);

            var connection = await _context.Connections
                                           .FirstOrDefaultAsync(c => c.UserId1 == u1 && c.UserId2 == u2);

            if (connection == null) return false; // Connection not found

            _context.Connections.Remove(connection);
            await _context.SaveChangesAsync();
            return true;
        }

        //public async Task<IEnumerable<SuggestedUserDto>> GetSuggestedUsersAsync(Guid currentUserId, int limit)
        //{

        //    // STEP 1: Build the exclusion list in SQL (already connected, pending requests, yourself)
        //    var usersToExclude =
        //        from u in _context.Users
        //        where
        //            // Already connected (either direction)
        //            _context.Connections.Any(c => (c.UserId1 == currentUserId && c.UserId2 == u.Id) ||
        //                                          (c.UserId2 == currentUserId && c.UserId1 == u.Id))
        //            // Pending connection requests (either direction)
        //            || _context.ConnectionRequests.Any(cr => (cr.SenderId == currentUserId && cr.ReceiverId == u.Id) ||
        //                                                     (cr.ReceiverId == currentUserId && cr.SenderId == u.Id))
        //            // The current user themselves
        //            || u.Id == currentUserId
        //        select u.Id;

        //    // STEP 2: Find connections-of-your-connections
        //    var connectionsOfConnections = await _context.Connections
        //        .Where(coc => usersToExclude.Contains(coc.UserId1) || usersToExclude.Contains(coc.UserId2))
        //        .Select(coc => usersToExclude.Contains(coc.UserId1) ? coc.UserId2 : coc.UserId1)
        //        .Distinct()
        //        .ToListAsync();

        //    // STEP 3: Get the final suggested users
        //    // - Must be a connection-of-a-connection
        //    // - Must NOT be in the exclusion list
        //    // - Order by recent profile activity
        //    // - Limit results to 'limit'
        //    var suggestedUsers = await _context.Users
        //        .AsNoTracking()
        //        .Include(u => u.Profile)
        //        .Where(u => connectionsOfConnections.Contains(u.Id) && !usersToExclude.Contains(u.Id))
        //        .OrderByDescending(u => u.Profile!.UpdatedAt)
        //        .Take(limit)
        //        .ToListAsync();


        //    // STEP 6: Convert the DB entities into the format needed by the API
        //    return _mapper.Map<IEnumerable<SuggestedUserDto>>(suggestedUsers);
        //}


        //public async Task<List<IEnumerable<SuggestedUserDto>>> GetSuggestedUsersAsync(Guid currentUserId, int limit)
        //{
        //    var result = new List<IEnumerable<SuggestedUserDto>>();

        //    // STEP 1: Build exclusion list (already connected, pending requests, yourself)
        //    var usersToExcludeQuery =
        //        from u in _context.Users
        //        where
        //            _context.Connections.Any(c =>
        //                (c.UserId1 == currentUserId && c.UserId2 == u.Id) ||
        //                (c.UserId2 == currentUserId && c.UserId1 == u.Id))
        //            || _context.ConnectionRequests.Any(cr =>
        //                (cr.SenderId == currentUserId && cr.ReceiverId == u.Id) ||
        //                (cr.ReceiverId == currentUserId && cr.SenderId == u.Id))
        //            || u.Id == currentUserId
        //        select u.Id;

        //    // STEP 2: Get connections-of-connections (both directions in one query)
        //    var connectionsOfConnections = await _context.Connections
        //        .Where(coc => !usersToExcludeQuery.Contains(coc.UserId1) && !usersToExcludeQuery.Contains(coc.UserId2))
        //        .Select(coc => usersToExcludeQuery.Contains(coc.UserId1) ? coc.UserId2 : coc.UserId1)
        //        .Distinct()
        //        .ToListAsync();

        //    // STEP 3: Get current user's data sequentially (to avoid DbContext concurrency issues)
        //    var currentUserHeadline = await _context.Users
        //        .Where(u => u.Id == currentUserId)
        //        .Select(u => u.Profile!.Headline)
        //        .FirstOrDefaultAsync();

        //    var currentUserTitle = await _context.UserExperiences
        //        .Where(e => e.UserProfileId == currentUserId)
        //        .OrderByDescending(e => e.EndDate ?? DateTime.MaxValue)
        //        .Select(e => e.Title)
        //        .FirstOrDefaultAsync();

        //    var currentUserEducation = await _context.UserEducation
        //        .Where(e => e.UserProfileId == currentUserId)
        //        .Select(e => new { e.Institution, e.Degree, e.Major })
        //        .ToListAsync();

        //    // STEP 4: Similar headline
        //    string? headlineKeyword = currentUserHeadline ;
        //        //.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        //        //.FirstOrDefault();

        //    var similarHeadlineUsers = await _context.Users
        //        .AsNoTracking()
        //        .Include(u => u.Profile)
        //        .Where(u =>
        //            //connectionsOfConnections.Contains(u.Id) &&
        //            !usersToExcludeQuery.Contains(u.Id) &&
        //            ((string.IsNullOrEmpty(headlineKeyword) ||
        //             (u.Profile!.Headline != null && u.Profile.Headline.Contains(headlineKeyword)))))
        //        .OrderByDescending(u => u.Profile!.UpdatedAt)
        //        .Take(limit)
        //        .ToListAsync();

        //    if (similarHeadlineUsers.Count() > 0) result.Add(_mapper.Map<IEnumerable<SuggestedUserDto>>(similarHeadlineUsers));

        //    // STEP 5: Similar title
        //    if (!string.IsNullOrEmpty(currentUserTitle))
        //    {
        //        var similarTitleUsers = await _context.Users
        //            .AsNoTracking()
        //            .Include(u => u.Profile)
        //            .Where(u =>
        //                //connectionsOfConnections.Contains(u.Id) &&
        //                !usersToExcludeQuery.Contains(u.Id) &&
        //                _context.UserExperiences.Any(e =>
        //                    e.UserProfileId == u.Id &&
        //                    e.Title == currentUserTitle))
        //            .OrderByDescending(u => u.Profile!.UpdatedAt)
        //            .Take(limit)
        //            .ToListAsync();

        //        if(similarTitleUsers.Count() > 0) result.Add(_mapper.Map<IEnumerable<SuggestedUserDto>>(similarTitleUsers));
        //    }
        //    else
        //    {
        //        result.Add(Enumerable.Empty<SuggestedUserDto>());
        //    }

        //    // STEP 6: Similar education
        //    var possibleUsers = await _context.Users
        //        .AsNoTracking()
        //        .Include(u => u.Profile)
        //        .Where(u =>
        //            //connectionsOfConnections.Contains(u.Id) &&
        //            !usersToExcludeQuery.Contains(u.Id))
        //        .OrderByDescending(u => u.Profile!.UpdatedAt)
        //        .Take(limit * 3) // extra for filtering
        //        .ToListAsync();

        //    var possibleUserIds = possibleUsers.Select(u => u.Id).ToList();

        //    var possibleUserEducations = await _context.UserEducation
        //        .Where(e => possibleUserIds.Contains(e.UserProfileId))
        //        .Select(e => new { e.UserProfileId, e.Institution, e.Degree, e.Major })
        //        .ToListAsync();

        //    // In-memory parallel filtering for speed
        //    var similarEducationUsers = possibleUsers
        //        .AsParallel()
        //        .Where(u =>
        //            possibleUserEducations.Any(e =>
        //                e.UserProfileId == u.Id &&
        //                currentUserEducation.Any(edu =>
        //                    (edu.Institution != null && e.Institution == edu.Institution) ||
        //                    (edu.Degree != null && e.Degree == edu.Degree) ||
        //                    (edu.Major != null && e.Major == edu.Major)
        //                )
        //            )
        //        )
        //        .Take(limit)
        //        .ToList();

        //    if (similarEducationUsers.Count() > 0) result.Add(_mapper.Map<IEnumerable<SuggestedUserDto>>(similarEducationUsers));

        //    return result;
        //}

        //public async Task<IEnumerable<SuggestedUserDto>> GetSuggestedUsersAsync(Guid currentUserId, int limit)
        public async Task<Dictionary<string, List<SuggestedUserDto>>> GetSuggestedUsersAsync(Guid currentUserId, int limit)
        {
            //_logger.LogInformation("Fetching categorized suggested users for user {UserId} with limit {Limit}.", currentUserId, limit);

            var suggestions = new Dictionary<string, List<SuggestedUserDto>>
            {
                { "People With Similar Headline", new List<SuggestedUserDto>() },
                { "People With Similar Title", new List<SuggestedUserDto>() },
                { "People With Similar Education", new List<SuggestedUserDto>() }
            };

            // Get all excluded users in one go
            var excludedUsers = await _context.Connections
                .Where(c => c.UserId1 == currentUserId || c.UserId2 == currentUserId)
                .Select(c => c.UserId1 == currentUserId ? c.UserId2 : c.UserId1)
                .ToListAsync();

            excludedUsers.AddRange(await _context.ConnectionRequests
                .Where(cr => cr.SenderId == currentUserId || cr.ReceiverId == currentUserId)
                .Select(cr => cr.SenderId == currentUserId ? cr.ReceiverId : cr.SenderId)
                .ToListAsync());

            excludedUsers.Add(currentUserId);
            var excludedUserIds = excludedUsers.Distinct().ToHashSet();

            // Get current user's profile data
            var currentUserProfileData = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId)
                .Select(u => new
                {
                    Headline = u.Profile!.Headline,
                    JobTitle = _context.UserExperiences.Where(e => e.UserProfileId == u.Id).OrderByDescending(e => e.EndDate ?? DateTime.MaxValue).Select(e => e.Title).FirstOrDefault(),
                    Education = _context.UserEducation.Where(e => e.UserProfileId == u.Id).Select(e => new { e.Institution, e.Degree, e.Major }).ToList(),
                })
                .FirstOrDefaultAsync();

            if (currentUserProfileData == null) return suggestions;

            // Fetch potential suggestions to filter in memory
            var allPotentialSuggestions = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .Include(u => u.Profile.Experiences)
                .Include(u => u.Profile.Education)
                .Where(u => !excludedUserIds.Contains(u.Id))
                .OrderByDescending(u => u.Profile!.UpdatedAt)
                .Take(limit * 3) // Fetch more than needed to ensure we have enough suggestions after filtering
                .ToListAsync();

            // Filter and categorize in memory
            var similarHeadlineUsers = allPotentialSuggestions
                .Where(u => !string.IsNullOrEmpty(currentUserProfileData.Headline) &&
                            !string.IsNullOrEmpty(u.Profile?.Headline) &&
                            u.Profile.Headline.Contains(currentUserProfileData.Headline))
                .Take(limit)
                .ToList();

            var similarTitleUsers = allPotentialSuggestions
                .Where(u => !string.IsNullOrEmpty(currentUserProfileData.JobTitle) &&
                            u.Profile.Experiences.Any(e => e.Title == currentUserProfileData.JobTitle))
                .Take(limit)
                .ToList();

            var similarEducationUsers = allPotentialSuggestions
                .Where(u => currentUserProfileData.Education.Any(edu =>
                            u.Profile.Education.Any(sugEdu =>
                                (edu.Institution != null && sugEdu.Institution == edu.Institution) ||
                                (edu.Degree != null && sugEdu.Degree == edu.Degree) ||
                                (edu.Major != null && sugEdu.Major == edu.Major))))
                .Take(limit)
                .ToList();

            suggestions["People With Similar Headline"] = _mapper.Map<List<SuggestedUserDto>>(similarHeadlineUsers);
            suggestions["People With Similar Title"] = _mapper.Map<List<SuggestedUserDto>>(similarTitleUsers);
            suggestions["People With Similar Education"] = _mapper.Map<List<SuggestedUserDto>>(similarEducationUsers);

            //_logger.LogInformation("Found {Count} total categorized suggestions.", similarHeadlineUsers.Count + similarTitleUsers.Count + similarEducationUsers.Count);

            return suggestions;
        }

        public async Task<IEnumerable<SuggestedUserDto>> GetGeneralSuggestedUsersAsync(Guid currentUserId, int limit)
        {
            //_logger.LogInformation("Fetching general suggested users for user {UserId} with limit {Limit}.", currentUserId, limit);

            // Fetch all excluded users in one go
            var excludedUsers = await _context.Connections
                .Where(c => c.UserId1 == currentUserId || c.UserId2 == currentUserId)
                .Select(c => c.UserId1 == currentUserId ? c.UserId2 : c.UserId1)
                .ToListAsync();

            excludedUsers.AddRange(await _context.ConnectionRequests
                .Where(cr => cr.SenderId == currentUserId || cr.ReceiverId == currentUserId)
                .Select(cr => cr.SenderId == currentUserId ? cr.ReceiverId : cr.SenderId)
                .ToListAsync());

            excludedUsers.Add(currentUserId);
            var excludedUserIds = excludedUsers.Distinct().ToHashSet();

            // Fetch current user's data
            var currentUserProfileData = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId)
                .Select(u => new
                {
                    Headline = u.Profile!.Headline,
                    JobTitle = _context.UserExperiences.Where(e => e.UserProfileId == u.Id).OrderByDescending(e => e.EndDate ?? DateTime.MaxValue).Select(e => e.Title).FirstOrDefault(),
                    Educations = _context.UserEducation.Where(e => e.UserProfileId == u.Id).Select(e => new { e.Institution, e.Degree, e.Major }).ToList(),
                })
                .FirstOrDefaultAsync();

            if (currentUserProfileData == null) return Enumerable.Empty<SuggestedUserDto>();

            // Fetch a larger pool of potential users to score in memory
            var potentialUsers = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .Include(u => u.Profile.Experiences)
                .Include(u => u.Profile.Education)
                .Where(u => !excludedUserIds.Contains(u.Id))
                .ToListAsync();

            var scoredUsers = potentialUsers
                .Select(user =>
                {
                    int score = 0;
                    // Score for similar headline
                    if (!string.IsNullOrEmpty(currentUserProfileData.Headline) &&
                        !string.IsNullOrEmpty(user.Profile?.Headline) &&
                        user.Profile.Headline.Contains(currentUserProfileData.Headline))
                    {
                        score += 3;
                    }
                    // Score for similar job title
                    if (!string.IsNullOrEmpty(currentUserProfileData.JobTitle) &&
                        user.Profile.Experiences.Any(e => e.Title == currentUserProfileData.JobTitle))
                    {
                        score += 2;
                    }
                    // Score for similar education
                    if (currentUserProfileData.Educations.Any() &&
                        user.Profile.Education.Any(sugEdu =>
                            currentUserProfileData.Educations.Any(edu =>
                                (edu.Institution != null && sugEdu.Institution == edu.Institution) ||
                                (edu.Degree != null && sugEdu.Degree == edu.Degree) ||
                                (edu.Major != null && sugEdu.Major == edu.Major))))
                    {
                        score += 1;
                    }

                    return new { User = user, Score = score };
                })
                .Where(x => x.Score > 0) // Only include users with at least one match
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .ToList();

            return _mapper.Map<IEnumerable<SuggestedUserDto>>(scoredUsers.Select(x => x.User));
        }



        //public async Task<IEnumerable<PopularUserDto>> GetPopularUsersAsync(Guid currentUserId, int limit)
        //{
        //    var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);

        //    // Fetch all excluded users in one go
        //    var excludedUserIds = new HashSet<Guid>();
        //    excludedUserIds.Add(currentUserId);
        //    var connectionUserIds = await _context.Connections
        //        .Where(c => c.UserId1 == currentUserId || c.UserId2 == currentUserId)
        //        .Select(c => c.UserId1 == currentUserId ? c.UserId2 : c.UserId1)
        //        .ToListAsync();
        //    excludedUserIds.UnionWith(connectionUserIds);

        //    var popularUsersQuery =
        //        from u in _context.Users.AsNoTracking().Include(u => u.Profile)
        //        where !excludedUserIds.Contains(u.Id)
        //        let recentConnectionCount = _context.Connections.Count(c =>
        //            (c.UserId1 == u.Id || c.UserId2 == u.Id) &&
        //            c.CreatedAt >= twoWeeksAgo)
        //        let mutualCount = _context.Connections.Count(c =>
        //            (c.UserId1 == u.Id && connectionUserIds.Contains(c.UserId2)) ||
        //            (c.UserId2 == u.Id && connectionUserIds.Contains(c.UserId1)))
        //        where recentConnectionCount > 0
        //        orderby recentConnectionCount descending, mutualCount descending, u.Profile!.UpdatedAt descending
        //        select new PopularUserDto
        //        {
        //            Id = u.Id,
        //            Username = u.Username,
        //            FirstName = u.Profile!.FirstName,
        //            LastName = u.Profile!.LastName,
        //            ProfilePictureUrl = u.Profile!.ProfilePictureUrl,
        //            Headline = u.Profile!.Headline,
        //            MutualConnections = mutualCount
        //        };

        //    return await popularUsersQuery.Take(limit).ToListAsync();
        //}

        public async Task<IEnumerable<PopularUserDto>> GetPopularUsersAsync(Guid currentUserId, int limit)
        {
            var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);

            // Fetch all excluded users in one go
            var excludedUserIds = await _context.Connections
                .Where(c => c.UserId1 == currentUserId || c.UserId2 == currentUserId)
                .Select(c => c.UserId1 == currentUserId ? c.UserId2 : c.UserId1)
                .ToListAsync();

            excludedUserIds.Add(currentUserId);
            var excludedUserIdsSet = excludedUserIds.Distinct().ToHashSet();

            // Query for users popular by recent connections
            var popularByRecentConnections = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .Where(u => !excludedUserIdsSet.Contains(u.Id) && _context.Connections.Any(c =>
                    (c.UserId1 == u.Id || c.UserId2 == u.Id) && c.CreatedAt >= twoWeeksAgo))
                .ToListAsync();

            // Query for users popular by mutual connections
            var popularByMutualConnections = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .Where(u => !excludedUserIdsSet.Contains(u.Id) && _context.Connections.Any(c =>
                    (c.UserId1 == u.Id && excludedUserIds.Contains(c.UserId2)) ||
                    (c.UserId2 == u.Id && excludedUserIds.Contains(c.UserId1))))
                .ToListAsync();

            // Query for users popular by profile updates
            var popularByProfileUpdates = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .Where(u => !excludedUserIdsSet.Contains(u.Id))
                .OrderByDescending(u => u.Profile!.UpdatedAt)
                .Take(limit)
                .ToListAsync();

            // Combine all results into a single list of unique users
            var combinedUsers = popularByRecentConnections
                .Union(popularByMutualConnections)
                .Union(popularByProfileUpdates)
                .DistinctBy(u => u.Id)
                .ToList();

            // Manually map to DTOs and calculate MutualConnections in memory
            var finalSuggestions = combinedUsers.Select(u => new PopularUserDto
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.Profile!.FirstName,
                LastName = u.Profile!.LastName,
                ProfilePictureUrl = _userService.MapToDo(u.Profile.ProfilePictureMediaId),
                Headline = u.Profile!.Headline,
                MutualConnections = _context.Connections.Count(c =>
                    (c.UserId1 == u.Id && excludedUserIds.Contains(c.UserId2)) ||
                    (c.UserId2 == u.Id && excludedUserIds.Contains(c.UserId1)))
            })
            .OrderByDescending(u => u.MutualConnections) // You can add custom ordering here
            .Take(limit);

            return finalSuggestions;
        }

    }
}