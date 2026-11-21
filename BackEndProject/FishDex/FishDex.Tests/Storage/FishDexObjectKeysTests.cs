using FishDex.Domain.Storage;

namespace FishDex.Tests.Storage;

/// <summary><c>SpeciesSnapshot.ThumbnailObjectKey</c> đang lưu key theo format này — khoá lại bằng chuỗi viết tay.</summary>
public sealed class FishDexObjectKeysTests
{
    [Fact]
    public void Community_cover_key_format()
    {
        Assert.Equal("community/500001/cover.jpg", FishDexObjectKeys.CommunityCover(500001, ".jpg"));
    }

    [Fact]
    public void Community_key_parses_back_to_its_spec_code()
    {
        Assert.True(FishDexObjectKeys.TryParseCommunity("community/500001/cover.png", out var specCode));
        Assert.Equal(500001, specCode);
    }

    [Theory]
    [InlineData("100/abc.jpg")]
    [InlineData("community/abc/cover.jpg")]
    [InlineData("community/-5/cover.jpg")]
    [InlineData("community")]
    public void Non_community_or_malformed_keys_do_not_parse(string key)
    {
        Assert.False(FishDexObjectKeys.TryParseCommunity(key, out _));
    }
}
