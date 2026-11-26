using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using UserManagement.Domain.DTOs.Account;
using UserManagement.Domain.DTOs.Auth;
using UserManagement.Tests.Infrastructure;

namespace UserManagement.Tests;

/// <summary>
/// Luồng tài khoản qua HTTP của <c>/api/auth/*</c>: xin mã OTP → đăng ký → đăng nhập,
/// quên/đặt lại mật khẩu, và logout phải có đăng nhập.
/// </summary>
[Collection(UserManagementApiCollection.Name)]
public sealed class AuthFlowTests(UserManagementApiFixture api)
{
    private const string Password = "Fishlover2026";
    private readonly HttpClient _client = api.CreateClient();

    private static string NewGmail() => $"test.{Guid.NewGuid():N}@gmail.com";

    private Task<HttpResponseMessage> RequestCodeAsync(string email) =>
        _client.GetAsync($"/api/auth/valid-email/{Uri.EscapeDataString(email)}");

    private Task<HttpResponseMessage> RegisterAsync(string email, string code, string password = Password) =>
        _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password, password, "Test", "User", code));

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

    private async Task RegisterUserAsync(string email)
    {
        (await RequestCodeAsync(email)).EnsureSuccessStatusCode();
        (await RegisterAsync(email, api.Emails.LastCodeFor(email)!)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Requesting_a_code_for_a_non_gmail_address_is_rejected_and_sends_nothing()
    {
        const string email = "someone@yahoo.com";

        var response = await RequestCodeAsync(email);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EmailVerificationResponse>();
        Assert.False(body!.Success);
        Assert.Null(api.Emails.LastCodeFor(email));
    }

    [Fact]
    public async Task Registering_with_the_emailed_code_creates_a_member_who_can_sign_in()
    {
        var email = NewGmail();

        Assert.Equal(HttpStatusCode.OK, (await RequestCodeAsync(email)).StatusCode);
        var code = api.Emails.LastCodeFor(email);
        Assert.False(string.IsNullOrWhiteSpace(code));

        var register = await RegisterAsync(email, code!);
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        Assert.True(api.Emails.WasWelcomed(email));

        var login = await LoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(session!.AccessToken));
        Assert.Contains("Member", session.User!.Roles);

        // Mã dùng một lần: đăng ký lại bằng chính mã đó không tạo được tài khoản thứ hai.
        Assert.Equal(HttpStatusCode.BadRequest, (await RegisterAsync(email, code!)).StatusCode);
    }

    [Fact]
    public async Task A_second_code_cannot_be_requested_within_a_minute()
    {
        var email = NewGmail();
        Assert.Equal(HttpStatusCode.OK, (await RequestCodeAsync(email)).StatusCode);

        var again = await RequestCodeAsync(email);

        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task Five_wrong_codes_burn_the_code_so_the_right_one_no_longer_works()
    {
        var email = NewGmail();
        (await RequestCodeAsync(email)).EnsureSuccessStatusCode();
        var realCode = api.Emails.LastCodeFor(email)!;

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var wrong = await RegisterAsync(email, "WRONG000");
            var body = await wrong.Content.ReadFromJsonAsync<RegisterResponse>();
            Assert.Equal("Invalid verification code", body!.Message);
        }

        var sixth = await (await RegisterAsync(email, realCode)).Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.False(sixth!.Success);
        Assert.StartsWith("Too many invalid attempts", sixth.Message);

        var afterBurn = await (await RegisterAsync(email, realCode)).Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.False(afterBurn!.Success);
        Assert.Equal("Verification code expired or not found", afterBurn.Message);
    }

    [Fact]
    public async Task Forgot_then_reset_password_replaces_the_old_password()
    {
        var email = NewGmail();
        await RegisterUserAsync(email);

        (await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email)))
            .EnsureSuccessStatusCode();
        var resetUrl = api.Emails.LastResetUrlFor(email);
        Assert.NotNull(resetUrl);
        var query = QueryHelpers.ParseQuery(new Uri(resetUrl!).Query);
        Assert.Equal(email, query["email"].ToString());

        const string newPassword = "NewFishlover2026";
        var reset = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, query["token"].ToString(), newPassword, newPassword));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, newPassword)).StatusCode);
    }

    [Fact]
    public async Task Reset_with_a_made_up_token_is_rejected()
    {
        var email = NewGmail();
        await RegisterUserAsync(email);

        var reset = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(email, "not-a-real-token", "NewFishlover2026", "NewFishlover2026"));

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, Password)).StatusCode);
    }

    [Fact]
    public async Task Forgot_password_for_an_unknown_email_answers_the_same_and_sends_nothing()
    {
        var email = NewGmail();

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(api.Emails.LastResetUrlFor(email));
    }

    [Fact]
    public async Task Logout_requires_a_signed_in_user()
    {
        // Trước đây [AllowAnonymous] ở cấp class làm [Authorize] của logout mất tác dụng.
        var anonymous = await _client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var email = NewGmail();
        await RegisterUserAsync(email);
        var session = await (await LoginAsync(email, Password)).Content.ReadFromJsonAsync<LoginResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var signedIn = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
    }
}
