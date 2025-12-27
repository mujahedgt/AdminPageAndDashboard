namespace AdminPageAndDashboard.Models.ViewModels
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? Message { get; set; }
        public string? Exception { get; set; }
        public bool IsDevelopment { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}