# Deploy Checklist — FishDex PROD (Oracle VM)

Target: FishDex API + UserManagement + ApiGateway. AquaHome bỏ qua.

---

## Đã xong
- [x] 4 pipeline Azure DevOps đã tạo và đặt tên rõ ràng
- [x] Variable Group `oracle-prod-secrets` đã tạo + authorize cho pipeline
- [x] Docker đã cài trên Oracle VM
- [x] Azure DevOps pipelines đã config build image → push GHCR (VM chỉ pull về chạy, không build trực tiếp)
- [x] Cloudflare R2 bucket `system-image` đã tạo + data migrated từ MinIO local
- [x] R2 User API Token (Read Only) đã tạo cho FishDex API

---

## Còn lại

### Bước 1 — Chuẩn bị file trên Oracle VM

SSH vào VM:
```bash
ssh -i "C:\Users\Lenovo\.ssh\ssh-key-2026-06-01.key" ubuntu@<ORACLE_VM_IP>
mkdir -p ~/app
```

**1.1 — Copy tất cả file config lên VM (1 lần)**

Từ máy local (PowerShell) — tạo `.env` local trước, điền credentials, rồi scp:
```powershell
# Copy docker-compose + nginx + init script
scp -i "C:\Users\Lenovo\.ssh\ssh-key-2026-06-01.key" `
  "D:\Workspace\FishLover\FishDex\Pipeline\OracleVM\docker-compose.prod.yml" `
  "D:\Workspace\FishLover\FishDex\Pipeline\OracleVM\nginx.conf" `
  "D:\Workspace\FishLover\FishDex\Pipeline\OracleVM\init-db.sh" `
  ubuntu@<ORACLE_VM_IP>:~/app/

# Copy .env (tạo local từ .env.example, điền xong mới scp)
scp -i "C:\Users\Lenovo\.ssh\ssh-key-2026-06-01.key" `
  "D:\Workspace\FishLover\FishDex\Pipeline\OracleVM\.env.prod" `
  ubuntu@<ORACLE_VM_IP>:~/app/.env
```

> Tạo `.env.prod` từ template `.env.example`, điền đủ values — KHÔNG commit lên git.

**1.2 — Cấp quyền execute cho init script**
```bash
chmod +x ~/app/init-db.sh
```

**1.3 — Đổi domain trong nginx.conf**
```bash
sed -i 's/<YOUR_DOMAIN>/api.yourdomain.com/g' ~/app/nginx.conf
```

---

### Bước 2 — SSL Certificate (Let's Encrypt)

```bash
# Cài certbot
sudo apt install -y certbot

# Lấy cert (tạm dừng nginx nếu đang chạy)
sudo certbot certonly --standalone -d api.yourdomain.com

# Copy cert vào ~/app/ssl/
mkdir -p ~/app/ssl
sudo cp /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem ~/app/ssl/
sudo cp /etc/letsencrypt/live/api.yourdomain.com/privkey.pem ~/app/ssl/
sudo chown ubuntu:ubuntu ~/app/ssl/*
```

---

### Bước 3 — Chạy Pipeline lần đầu (trigger thủ công)

Vào Azure DevOps → từng pipeline → **Run pipeline**:
- [ ] Run **UserManagement - Build & Deploy**
- [ ] Run **FishDex - Build & Deploy**
- [ ] Run **ApiGateway - Build & Deploy**

Xác nhận 3 image đã xuất hiện trên GHCR: `github.com/TheEastPham?tab=packages`

---

### Bước 4 — Migrate FishDex DB (local → PROD)

Làm trước khi start API để FishDex có data ngay khi lên.

**Dump từ local** (port 5433):
```powershell
pg_dump -h localhost -p 5433 -U fishdex -d fishdex -F c -f fishdex_dump.dump
```

**Copy lên VM:**
```powershell
scp -i "C:\Users\Lenovo\.ssh\ssh-key-2026-06-01.key" `
  fishdex_dump.dump ubuntu@<ORACLE_VM_IP>:~/
```

**Khởi động PostgreSQL + Redis trước** (cần DB running để restore):
```bash
echo "<GHCR_TOKEN>" | docker login ghcr.io -u TheEastPham --password-stdin
docker compose -f ~/app/docker-compose.prod.yml up -d postgres redis
sleep 15  # đợi init-db.sh tạo xong users/databases
```

**Restore trên VM** (dùng postgres admin user):
```bash
docker exec -i postgres pg_restore -U postgres -d fishdex < ~/fishdex_dump.dump
```

---

### Bước 5 — Khởi động tất cả services

```bash
# Start app services (DB đã có data)
docker compose -f ~/app/docker-compose.prod.yml up -d
```

Kiểm tra:
```bash
docker compose -f ~/app/docker-compose.prod.yml ps
docker logs fishdex --tail 50
docker logs usermanagement --tail 50
```

---

### Bước 6 — Smoke test

- [ ] `curl https://api.yourdomain.com/health` — gateway phản hồi
- [ ] Test login qua UserManagement
- [ ] Test 1-2 endpoint FishDex, kiểm tra ảnh load từ R2

---

## Lưu ý
- AquaHome bỏ qua trong đợt deploy này
- `GHCR_TOKEN` hết hạn **03/07/2026** — nhớ rotate trước ngày đó
- FE deploy trên Cloudflare Pages — nginx chỉ proxy đến ApiGateway, không serve static
- PostgreSQL: mỗi DB có user riêng (um_user, fd_user, ah_user) — init-db.sh tạo tự động lần đầu
- SSL cert cần renew định kỳ — cân nhắc setup certbot auto-renew
- Sau khi ổn định: tắt Swagger trên PROD
- CORS: `FE_ORIGIN` trong `.env` hiện để placeholder — **cần update khi FE go live** (xem mục bên dưới)

---

## Khi FE go live trên Cloudflare Pages *(việc cần làm)*

1. **Lấy URL Cloudflare Pages** sau khi FE deploy xong (dạng `https://fishlover.pages.dev` hoặc custom domain)
2. **SSH vào VM**, sửa `~/app/.env`:
   ```bash
   # Đổi FE_ORIGIN thành URL thật
   sed -i 's|FE_ORIGIN=.*|FE_ORIGIN=https://fishlover.pages.dev|' ~/app/.env
   ```
3. **Restart 2 services** đọc CORS config (không cần restart toàn bộ stack):
   ```bash
   docker compose -f ~/app/docker-compose.prod.yml up -d --no-deps usermanagement fishdex
   ```
4. **Kiểm tra** login + gọi API từ FE không bị CORS error

---

## Post v1.0 — X.509 Cert thật cho OpenIddict *(long-term)*

**Hiện tại (v1.0):** UserManagement tự tạo self-signed cert lưu tại Docker volume `openiddict_keys`.
Cert sống qua restart container. Phù hợp cho single-instance. Cert có hiệu lực 10 năm.

**Giới hạn cần giải quyết khi scale:**
- Multi-instance (horizontal scale): tất cả instance phải dùng cùng 1 cert để token được ký/giải mã nhất quán. Volume không share được giữa các host.
- Cert rotation: khi cert hết hạn hoặc bị compromise, cần thay thủ công.

**Khi nào cần upgrade lên X.509 thật:**
- Khi cần chạy nhiều hơn 1 instance UserManagement
- Khi yêu cầu compliance bắt buộc dùng cert từ CA

**Các bước thực hiện (khi đến lúc):**

1. **Tạo hoặc import cert** (chọn 1 trong 2):
   - Azure Key Vault: upload `.pfx`, lấy certificate identifier
   - Self-managed: tạo cert từ internal CA, export `.pfx` có password

2. **Lưu cert an toàn** — KHÔNG commit lên git:
   ```bash
   # Ví dụ: lưu tại ~/app/certs/ trên VM, mount vào container
   mkdir -p ~/app/certs
   # Copy cert.pfx lên VM bằng scp
   ```

3. **Cập nhật `OpenIddictServerExtensions.cs`** — thay `GetOrCreateDevCert()`:
   ```csharp
   // Thay đoạn GetOrCreateDevCert bằng:
   var certPath = configuration["OpenIddict:CertPath"]!;      // path tới .pfx trong container
   var certPassword = configuration["OpenIddict:CertPassword"]!;
   var cert = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
   options.AddSigningCertificate(cert);
   options.AddEncryptionCertificate(cert);
   ```

4. **Cập nhật `docker-compose.prod.yml`**:
   ```yaml
   volumes:
     - ~/app/certs:/app/certs:ro   # thêm vào usermanagement service
   environment:
     - OpenIddict__CertPath=/app/certs/cert.pfx
     - OpenIddict__CertPassword=${OIDC_CERT_PASSWORD}
   ```

5. **Thêm vào `.env`**:
   ```
   OIDC_CERT_PASSWORD=your_cert_password
   ```

> **Lưu ý rotation:** Khi thay cert mới, tất cả access token hiện tại bị invalidate (user cần login lại). Refresh token cũng bị mất. Lên kế hoạch maintenance window khi rotate.
