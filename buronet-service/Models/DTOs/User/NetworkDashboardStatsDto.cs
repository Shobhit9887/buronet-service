namespace buronet_service.Models.DTOs.User;

public class NetworkDashboardStatsDto
{
    public long TotalConnections { get; set; }
    public long ConnectionsThisMonth { get; set; }
    public long PendingRequests { get; set; }
    public long JoinedGroups { get; set; }
    public long GroupsJoinedThisMonth { get; set; }
    public long NewConnectionsThisWeek { get; set; }

    // Calculated trend properties
    public decimal ConnectionsTrendPercentage
    {
        get
        {
            if (TotalConnections == 0)
                return 0;
            return (decimal)ConnectionsThisMonth / TotalConnections * 100;
        }
    }

    public decimal GroupsTrendPercentage
    {
        get
        {
            if (JoinedGroups == 0)
                return 0;
            return (decimal)GroupsJoinedThisMonth / JoinedGroups * 100;
        }
    }

    public long PendingRequestsTrend => PendingRequests;

    public decimal NetworkGrowthPercentage
    {
        get
        {
            if (NewConnectionsThisWeek == 0)
                return 0;
            return (decimal)NewConnectionsThisWeek / (TotalConnections == 0 ? 1 : TotalConnections) * 100;
        }
    }
}
