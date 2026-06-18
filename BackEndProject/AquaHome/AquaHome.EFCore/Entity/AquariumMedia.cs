using System.IO;

namespace AquaHome.EFCore.Entity;

public class AquariumMedia
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public Guid   AquariumId  { get; set; }
    public string FileName    { get; set; } = string.Empty;  // original filename, used for extension only
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Aquarium Aquarium { get; set; } = null!;

    /// <summary>Relative path within the bucket: aquahome/{env}/aquaria/{aquariumId}/{id}{ext}</summary>
    public string ObjectKey(string env) =>
        $"aquahome/{env}/aquaria/{AquariumId}/{Id}{Path.GetExtension(FileName)}";
}
