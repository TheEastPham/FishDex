using System.Text.Json;
using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Extensions;

/// <summary>
/// Đọc cột JSONB AquariumSnapshot.SnapshotData một cách an toàn.
/// Dùng chung cho SnapshotService và ContestService — snapshot cũ có thể sai schema,
/// nên mọi lỗi parse đều nuốt về null thay vì làm hỏng cả request.
/// </summary>
public static class SnapshotDataReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static SnapshotDataDto? TryRead(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson)) return null;

        try
        {
            return JsonSerializer.Deserialize<SnapshotDataDto>(snapshotJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? TryGetAquariumName(string? snapshotJson) => TryRead(snapshotJson)?.AquariumName;
}
