namespace AdminPageAndDashboard.Models.ViewModels
{
    public class RecentRequest
    {
        public string RequestId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ClientIp { get; set; }
        public string Endpoint { get; set; }
        public bool IsAnomaly { get; set; }
        public float Confidence { get; set; }
    }
}
