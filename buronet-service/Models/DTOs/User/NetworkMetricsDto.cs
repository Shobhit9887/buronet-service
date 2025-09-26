namespace buronet_service.Models.DTOs.User
{
    public class NetworkMetricsDto
    {
        public int TotalConnections { get; set; }
        public int JoinedGroups { get; set; } // Count of user's UserCommunityGroup entries
        public int PendingRequests { get; set; } // Count of requests where current user is receiver
        public double NetworkGrowthPercentage { get; set; } // Placeholder
        public int NetworkGrowth { get; set; }
        public double TotalConnectionsTrend { get; internal set; }
        public double JoinedGroupsTrend { get; internal set; }
        public double PendingRequestsTrend { get; internal set; }
    }
}