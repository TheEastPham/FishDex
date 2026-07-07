namespace AquaHome.EFCore.Entity;

public class Contest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? YouTubePlaylistId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int Status { get; set; } // ContestStatus: Draft/Active/Ended
    public Guid CreatedBy { get; set; }

    public ICollection<ContestEntry> Entries { get; set; } = [];
}
