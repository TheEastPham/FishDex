using AquaHome.Domain.DTOs;
using AquaHome.Domain.Extensions;

namespace AquaHome.Tests;

/// <summary>
/// JSONB đã lưu trong DB phải đọc được mãi mãi: đổi schema SnapshotDataDto mà làm đỏ test này
/// nghĩa là snapshot cũ sẽ không mở được.
/// </summary>
public sealed class SnapshotDataReaderTests
{
    [Fact]
    public void Round_trip_keeps_the_document()
    {
        var data = new SnapshotDataDto(
            "Bể medaka", "don", 60, 30, 36, 65, null,
            [new SnapshotFishDto(7, "Oryzias latipes", "Medaka", "7/a.jpg", 5, [])],
            SnapshotDataDto.CurrentVersion);

        var read = SnapshotDataReader.TryRead(SnapshotDataReader.Serialize(data));

        Assert.NotNull(read);
        Assert.Equal("Bể medaka", read.AquariumName);
        Assert.Equal(SnapshotDataDto.CurrentVersion, read.Version);
        Assert.Equal("7/a.jpg", Assert.Single(read.Fish).ImageKey);
    }

    [Fact]
    public void Serialize_uses_camelCase_property_names()
    {
        var json = SnapshotDataReader.Serialize(new SnapshotDataDto("X", null, null, null, null, null, null, []));

        Assert.Contains("\"aquariumName\":\"X\"", json);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ not json")]
    [InlineData("[1,2,3]")]
    public void Unreadable_documents_return_null_instead_of_throwing(string? json)
    {
        Assert.Null(SnapshotDataReader.TryRead(json));
    }

    [Fact]
    public void Old_version_zero_document_without_a_version_field_is_still_readable()
    {
        const string old = """{"aquariumName":"Cũ","fish":[{"specCode":1,"speciesName":"A","imageUrl":"https://dead"}]}""";

        var read = SnapshotDataReader.TryRead(old);

        Assert.NotNull(read);
        Assert.Equal(0, read.Version);
        Assert.Equal("Cũ", read.AquariumName);
    }
}
