namespace CareerPilot.Api.dto;

public class DashboardSummaryResponse
{
    public int Wishlist { get; set; }
    public int Applied { get; set; }
    public int Assessment { get; set; }
    public int Interview { get; set; }
    public int Offer { get; set; }
    public int Rejected { get; set; }
    public int Total { get; set; }
}