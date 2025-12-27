namespace AdminPageAndDashboard.Models.ViewModels
{
    public class SystemHealthStatus
    {
        public string OverallStatus { get; set; }
        public ServiceHealth ApiMiddleware { get; set; }
        public ServiceHealth IsolationForest { get; set; }
        public ServiceHealth Honeypot { get; set; }
    }
}
