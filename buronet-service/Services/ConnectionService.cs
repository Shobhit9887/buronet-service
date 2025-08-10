using Microsoft.EntityFrameworkCore;
using buronet_service.Data; // Your DbContext
using buronet_service.Models.User; // Entities
using buronet_service.Models.DTOs.User; // DTOs
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions; // For ProjectTo

namespace buronet_service.Services // Ensure this namespace is correct
{
    public class ConnectionService : IConnectionService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public ConnectionService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Helper to enforce canonical order for connections (UserId1 < UserId2 alphabetically)
        private (Guid user1Id, Guid user2Id) GetCanonicalConnectionPair(Guid userid1, Guid userid2)
        {
            string id1 = userid1.ToString();
            string id2 = userid2.ToString();
            return string.Compare(id1, id2, StringComparison.Ordinal) < 0 ? (userid1, userid2) : (userid2, userid1);
        }

        // --- Network Metrics ---
        public async Task<NetworkMetricsDto> GetNetworkMetricsAsync(Guid userIdGuid)
        {
            string userIdString = userIdGuid.ToString();

            var totalConnections = await _context.Connections
                                                .CountAsync(c => c.UserId1 == userIdGuid || c.UserId2 == userIdGuid);

            var joinedGroups = await _context.UserCommunityGroups // UserCommunityGroups is a user's joined groups
                                            .CountAsync(ucg => ucg.UserProfileId == userIdGuid);

            var pendingRequests = await _context.ConnectionRequests
                                                .CountAsync(cr => cr.ReceiverId == userIdGuid && cr.Status == "Pending");

            // Network Growth Percentage is a placeholder for now, would need historical data
            return new NetworkMetricsDto
            {
                TotalConnections = totalConnections,
                JoinedGroups = joinedGroups,
                PendingRequests = pendingRequests,
                NetworkGrowthPercentage = 0.0 // Placeholder
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
                    ConnectedUserProfilePictureUrl = connectedUserProfile.ProfilePictureUrl,
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

        public async Task<IEnumerable<SuggestedUserDto>> GetSuggestedUsersAsync(Guid currentUserId, int limit)
        {
            //_logger.LogInformation("Fetching suggested users for user {UserId} with a limit of {Limit}.", currentUserId, limit);

            // Get a list of all user IDs that the current user is already connected to
            var connectedUserIds = await _context.Connections
                .Where(c => c.UserId1 == currentUserId)
                .Select(c => c.UserId2)
                .ToListAsync();

            // Also get the reverse connections
            var reverseConnectedUserIds = await _context.Connections
                .Where(c => c.UserId2 == currentUserId)
                .Select(c => c.UserId1)
                .ToListAsync();

            connectedUserIds.AddRange(reverseConnectedUserIds);
            connectedUserIds = connectedUserIds.Distinct().ToList();
            connectedUserIds.Add(currentUserId); // Exclude the current user themselves

            // Get a list of all user IDs that the current user has sent a connection request to
            var sentRequestUserIds = await _context.ConnectionRequests
                .Where(cr => cr.SenderId == currentUserId)
                .Select(cr => cr.ReceiverId)
                .ToListAsync();

            // Get a list of all user IDs that have sent a connection request to the current user
            var receivedRequestUserIds = await _context.ConnectionRequests
                .Where(cr => cr.ReceiverId == currentUserId)
                .Select(cr => cr.SenderId)
                .ToListAsync();

            // Combine all users to exclude from suggestions
            var usersToExclude = connectedUserIds.Union(sentRequestUserIds).Union(receivedRequestUserIds);

            // Fetch a list of suggested users from the database.
            var suggestedUsers = await _context.Users
                .AsNoTracking()
                .Include(u => u.Profile)
                .Where(u => !usersToExclude.Contains(u.Id)) // Filter out already connected/requested users
                .OrderByDescending(u => u.Profile!.UpdatedAt) // Order by recent profile activity
                .Take(limit)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SuggestedUserDto>>(suggestedUsers);
        }
    }
}