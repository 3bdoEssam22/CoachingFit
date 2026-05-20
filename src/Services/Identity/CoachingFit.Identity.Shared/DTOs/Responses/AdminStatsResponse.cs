namespace CoachingFit.Identity.Shared.DTOs.Responses;

public class AdminStatsResponse
{
    public int TotalCoaches { get; set; }
    public int ActiveCoaches { get; set; }
    public int PendingCoaches { get; set; }
    public int TotalTrainees { get; set; }
}
