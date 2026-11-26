using AquaHome.Domain.Slugs;

namespace AquaHome.Tests;

/// <summary>Luật slug dùng chung cho bài viết, bể đã đăng và tag.</summary>
public sealed class SlugTests
{
    [Theory]
    [InlineData("Bể Đỏ của tôi", "be-do-cua-toi")]             // đ/Đ — trước đây slug bể làm mất chữ "d"
    [InlineData("Đỗ Văn An", "do-van-an")]                       // họ Đỗ/Đặng/Đinh... rất phổ biến
    [InlineData("Cá Lia Thia Đẹp", "ca-lia-thia-dep")]
    [InlineData("  Medaka  --  Ban công  ", "medaka-ban-cong")]  // gộp gạch, bỏ khoảng trắng đầu cuối
    [InlineData("Bể (cá vàng) #1!", "be-ca-vang-1")]             // ký tự đặc biệt bị bỏ
    [InlineData("a_b.c/d", "a-b-c-d")]                           // _ . / thành gạch nối
    [InlineData("我的鱼缸", "我的鱼缸")]                           // chữ không phải Latin được giữ
    [InlineData("!!!", "")]
    [InlineData(null, "")]
    public void From_makes_a_url_safe_vietnamese_friendly_slug(string? input, string expected)
    {
        Assert.Equal(expected, Slug.From(input));
    }

    [Fact]
    public void From_caps_the_length_without_a_trailing_dash()
    {
        var slug = Slug.From(string.Join(' ', Enumerable.Repeat("medaka", 40)));

        Assert.True(slug.Length <= Slug.MaxLength);
        Assert.False(slug.EndsWith('-'));
    }

    [Fact]
    public async Task MakeUnique_adds_the_first_free_numeric_suffix()
    {
        var taken = new HashSet<string> { "be-medaka", "be-medaka-2" };

        Assert.Equal("be-medaka-3", await Slug.MakeUniqueAsync("be-medaka", s => Task.FromResult(taken.Contains(s))));
        Assert.Equal("be-moi", await Slug.MakeUniqueAsync("be-moi", s => Task.FromResult(taken.Contains(s))));
    }
}
