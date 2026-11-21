using System.Security.Cryptography;
using System.Text;
using FishLover.Storage.MediaAccess;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FishLover.Storage.Tests;

public sealed class MediaAccessSignerTests
{
    private static readonly byte[] Key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 8, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }

    private static MediaAccessSigner NewSigner(FixedClock clock, string? key = null, int maxExpiry = 7200)
    {
        var settings = new MediaAccessSettings
        {
            Key = key ?? Convert.ToBase64String(Key),
            DefaultExpiryInSeconds = 3600,
            MaxExpiryInSeconds = maxExpiry,
        };
        return new MediaAccessSigner(Options.Create(settings), clock, NullLogger<MediaAccessSigner>.Instance);
    }

    private static (string Exp, string Sig) Parse(string sas)
    {
        var parts = sas.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p[1]);
        Assert.Equal("1", parts["shared"]);
        return (parts["exp"], parts["sig"]);
    }

    /// <summary>
    /// Khoá định dạng chữ ký: link đã phát trước khi tách signer ra thư viện chung vẫn phải verify được.
    /// Chuỗi ký = "fishlover-media-access\n/\n{exp}", HMAC-SHA256, base64url không padding.
    /// </summary>
    [Fact]
    public void Signature_format_is_unchanged_so_issued_links_stay_valid()
    {
        var sas = NewSigner(new FixedClock(Now)).CreateShared(600)!;

        var exp = Now.ToUnixTimeSeconds() + 600;
        var expected = Convert.ToBase64String(HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes($"fishlover-media-access\n/\n{exp}")))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        Assert.Equal($"shared=1&exp={exp}&sig={expected}", sas.Sas);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(exp), sas.ExpiresAt);
    }

    [Fact]
    public void Default_expiry_is_used_when_none_requested()
    {
        var sas = NewSigner(new FixedClock(Now)).CreateShared(null)!;

        Assert.Equal(Now.AddSeconds(3600), sas.ExpiresAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7201)]
    public void Expiry_outside_the_allowed_range_throws(int expiry)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewSigner(new FixedClock(Now)).CreateShared(expiry));
    }

    [Fact]
    public void Valid_link_returns_the_canonical_key_and_remaining_seconds()
    {
        var clock = new FixedClock(Now);
        var signer = NewSigner(clock);
        var (exp, sig) = Parse(signer.CreateShared(600)!.Sas);
        clock.Now = Now.AddSeconds(100);

        var result = signer.Verify("/100//abc.jpg", "1", exp, sig);

        Assert.Null(result.Denial);
        Assert.Equal("100/abc.jpg", result.ObjectKey);
        Assert.Equal(500, result.RemainingSeconds);
    }

    [Fact]
    public void Tampered_link_is_invalid()
    {
        var signer = NewSigner(new FixedClock(Now));
        var (exp, sig) = Parse(signer.CreateShared(600)!.Sas);
        var otherExp = (long.Parse(exp) + 1).ToString();
        var flipped = (sig[0] == 'A' ? 'B' : 'A') + sig[1..];

        Assert.Equal(MediaAccessDenial.Invalid, signer.Verify("a.jpg", "1", otherExp, sig).Denial);
        Assert.Equal(MediaAccessDenial.Invalid, signer.Verify("a.jpg", "1", exp, flipped).Denial);
        Assert.Equal(MediaAccessDenial.Invalid, signer.Verify("a.jpg", "true", exp, sig).Denial);
        Assert.Equal(MediaAccessDenial.Invalid, signer.Verify("a.jpg", "1", "0" + exp, sig).Denial);
        Assert.Equal(MediaAccessDenial.Invalid, signer.Verify("a.jpg", "1", exp, null).Denial);
    }

    [Theory]
    [InlineData("a/../b.jpg")]
    [InlineData("a/%2e%2e/b.jpg")]
    [InlineData("a%2Fb.jpg")]
    [InlineData("https://evil.example/x.jpg")]
    [InlineData("a\\b.jpg")]
    [InlineData("")]
    public void Paths_that_could_escape_the_bucket_are_invalid(string path)
    {
        var signer = NewSigner(new FixedClock(Now));
        var (exp, sig) = Parse(signer.CreateShared(600)!.Sas);

        Assert.Equal(MediaAccessDenial.Invalid, signer.Verify(path, "1", exp, sig).Denial);
    }

    [Fact]
    public void Expired_link_is_reported_as_expired()
    {
        var clock = new FixedClock(Now);
        var signer = NewSigner(clock);
        var (exp, sig) = Parse(signer.CreateShared(600)!.Sas);
        clock.Now = Now.AddSeconds(600);

        Assert.Equal(MediaAccessDenial.Expired, signer.Verify("a.jpg", "1", exp, sig).Denial);
    }

    [Fact]
    public void Lowering_max_expiry_shortens_links_already_issued()
    {
        var clock = new FixedClock(Now);
        var (exp, sig) = Parse(NewSigner(clock, maxExpiry: 7200).CreateShared(7200)!.Sas);

        var stricter = NewSigner(clock, maxExpiry: 600);

        Assert.Equal(MediaAccessDenial.Expired, stricter.Verify("a.jpg", "1", exp, sig).Denial);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64!")]
    [InlineData("AQIDBA==")] // 4 byte — ngắn hơn 32
    public void Missing_or_weak_key_disables_media_access(string key)
    {
        var signer = NewSigner(new FixedClock(Now), key);

        Assert.False(signer.IsConfigured);
        Assert.Null(signer.CreateShared(600));
        Assert.Equal(MediaAccessDenial.NotConfigured, signer.Verify("a.jpg", "1", "1", "x").Denial);
    }
}
