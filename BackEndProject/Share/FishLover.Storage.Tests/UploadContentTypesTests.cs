namespace FishLover.Storage.Tests;

/// <summary>Đuôi đã gắn vào object key trong DB và trên R2 — test khoá từng cặp, đổi là mất object cũ.</summary>
public sealed class UploadContentTypesTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public void Images_map_to_their_extension(string contentType, string ext)
    {
        Assert.Equal(ext, UploadContentTypes.Images[contentType]);
        Assert.Equal(ext, UploadContentTypes.Logos[contentType]);
    }

    [Fact]
    public void Logos_add_svg_on_top_of_images()
    {
        Assert.Equal(".svg", UploadContentTypes.Logos["image/svg+xml"]);
        Assert.DoesNotContain("image/svg+xml", UploadContentTypes.Images.Keys);
        Assert.Equal(UploadContentTypes.Images.Count + 1, UploadContentTypes.Logos.Count);
    }

    [Theory]
    [InlineData("video/mp4", ".mp4")]
    [InlineData("video/quicktime", ".mov")]
    [InlineData("video/webm", ".webm")]
    public void Videos_map_to_their_extension(string contentType, string ext)
    {
        Assert.Equal(ext, UploadContentTypes.Videos[contentType]);
    }
}
