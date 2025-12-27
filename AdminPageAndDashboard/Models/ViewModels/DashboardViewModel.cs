namespace AdminPageAndDashboard.Models.ViewModels
{
    public class DashboardViewModel
    {
        public SystemHealthStatus SystemHealth { get; set; }
        public StatisticsSummary Statistics { get; set; }
        public List<RecentRequest> RecentRequests { get; set; }
        public List<TopAnomalousIp> TopAnomalousIps { get; set; }
    }
}
