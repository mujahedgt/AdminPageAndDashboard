namespace AdminPageAndDashboard.Models.ViewModels
{
    public class StatisticsSummary
    {
        public int TotalRequests { get; set; }
        public int AnomalyCount { get; set; }
        public int LegitimateCount { get; set; }
        public float AnomalyRate { get; set; }
        public int AverageResponseTimeMs { get; set; }
    }
}
