---
status: accepted
---

# Scheme "OpenIddict" tạm thời không validate audience

Scheme JWT "OpenIddict" (token OAuth2 PKCE, dùng chung qua `AddFishLoverResourceServer`) đặt `ValidateAudience = false`. FishDex và AquaHome chưa được đăng ký như resource server trong OpenIddict, nên token chưa mang audience riêng cho từng service để so khớp. Đây là shortcut có chủ đích, không phải thiết kế đích: mục #4 trong Production Checklist ở `CLAUDE.md` và `TODO(prod #4)` trong `ResourceServerExtensions.cs`.

## Consequences

- Một token OpenIddict hợp lệ (đúng issuer, chữ ký, còn hạn) được cả hai service chấp nhận, bất kể token cấp cho ai.
- Khi đăng ký resource server trong OpenIddict, bật `ValidateAudience` ở một chỗ duy nhất (`ResourceServerExtensions`); không sửa riêng từng `Program.cs`.
