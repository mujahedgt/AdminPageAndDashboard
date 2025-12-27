namespace AdminPageAndDashboard.Models.ViewModels
{
    public class TopAnomalousIp
    {
        public string Ip { get; set; }
        public int Count { get; set; }
        public float AverageConfidence { get; set; }
    }
}
