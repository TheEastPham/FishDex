namespace FishDex.Domain.DTOs.Media;

public class SystemImageDto
{
    public Guid Id { get; init; }
    public int SpecCode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PictureType { get; init; } = string.Empty;
    public bool? PicPreferred { get; init; }
}
