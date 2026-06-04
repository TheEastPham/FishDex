# Deploy Checklist — FishDex PROD (Oracle VM)

Target: FishDex API + UserManagement + ApiGateway. AquaHome bỏ qua.

---

## Đã xong
- [x] 4 pipeline Azure DevOps đã tạo và đặt tên rõ ràng
- [x] Variable Group `oracle-prod-secrets` đã tạo + authorize cho pipeline
- [x] Docker đã cài trên Oracle VM
- [x] Dockerfile đủ cho 3 service:
  - `Pipeline/local/FishDex/Dockerfile.api`
  - `Pipeline/local/UserManagement/Dockerfile.api`
  - `Pipeline/local/ApiGateway/Dockerfile.api`

---

## Còn lại

### Bước 1 — Chuẩn bị file trên Oracle VM

SSH vào VM (dùng VS Code Remote SSH hoặc terminal):
```bash
ssh -i <path-to-private-key> ubuntu@<ORACLE_VM_IP>
mkdir -p ~/app
```

**1.1 — Copy docker-compose.prod.yml lên VM**

Từ máy local (PowerShell):
```powershell
scp -i <path-to-private-key> D:\Workspace\FishLover\FishDex\Pipeline\OracleVM\docker-compose.prod.yml ubuntu@<ORACLE_VM_IP>:~/app/
```

**1.2 — Tạo file .env trên VM**
```bash
nano ~/app/.env
```

Điền đủ các giá trị sau (xem `.env.example` để tham khảo):
```env
GHCR_USERNAME=TheEastPham

PG_USER=fishlover
PG_PASSWORD=<mật khẩu mạnh>

USERMGMT_DB_CONN=Host=postgres;Port=5432;Database=usermanagement;Username=fishlover;Password=<PG_PASSWORD>
FISHDEX_DB_CONN=Host=postgres;Port=5432;Database=fishdex;Username=fishlover;Password=<PG_PASSWORD>
AQUAHOME_DB_CONN=Host=postgres;Port=5432;Database=aquahome;Username=fishlover;Password=<PG_PASSWORD>

REDIS_PASSWORD=<mật khẩu redis>
REDIS_CONN=redis:6379,password=<REDIS_PASSWORD>

OIDC_ISSUER=https://<domain-thật-của-bạn>

R2_SERVICE_URL=https://<accountId>.r2.cloudflarestorage.com
R2_ACCESS_KEY=<lấy từ Cloudflare>
R2_SECRET_KEY=<lấy từ Cloudflare>
R2_BUCKET_NAME=fish-images
```

**1.3 — Copy nginx.conf lên VM**

FE chạy trên Cloudflare Pages — nginx chỉ cần proxy đến ApiGateway (không serve FE).
File template đã có sẵn tại `Pipeline/OracleVM/nginx.conf`, chỉ cần đổi `<YOUR_DOMAIN>` rồi scp lên:

```powershell
scp -i "C:\Users\Lenovo\.ssh\ssh-key-2026-06-01.key" `
  "D:\Workspace\FishLover\FishDex\Pipeline\OracleVM\nginx.conf" `
  ubuntu@<ORACLE_VM_IP>:~/app/
```

> Trước khi start nginx cần có SSL cert tại `~/app/ssl/`. Xem Bước 1.4.

---

### Bước 2 — Cloudflare R2

- [ ] Vào Cloudflare dashboard → R2 → tạo bucket tên `fish-images`
- [ ] Manage R2 API Tokens → Create API Token → quyền Object Read & Write
- [ ] Copy `Access Key ID` + `Secret Access Key` → điền vào `.env` bước 1.2

---

### Bước 3 — Enable Swagger trên PROD

- [ ] Kiểm tra `FishDex/FishDex.API/Program.cs` xem Swagger có bị giới hạn chỉ chạy ở `Development` không
- [ ] Nếu có thì bỏ điều kiện môi trường hoặc thêm biến env để bật trên Docker

---

### Bước 4 — Chạy Pipeline lần đầu (trigger thủ công)

Vào Azure DevOps → từng pipeline → **Run pipeline**:
- [ ] Run **UserManagement - Build & Deploy**
- [ ] Run **FishDex - Build & Deploy**
- [ ] Run **ApiGateway - Build & Deploy**

Sau khi chạy xong, vào `github.com/<username>?tab=packages` xác nhận 3 image đã xuất hiện trên GHCR.

---

### Bước 5 — Khởi động services trên VM

SSH vào VM:
```bash
# Chạy DB + Redis trước, đợi sẵn sàng
docker compose -f ~/app/docker-compose.prod.yml up -d postgres redis

# Đợi ~15 giây rồi chạy app
docker compose -f ~/app/docker-compose.prod.yml up -d usermanagement fishdex gateway nginx
```

Kiểm tra trạng thái:
```bash
docker compose -f ~/app/docker-compose.prod.yml ps
docker logs fishdex --tail 50
docker logs usermanagement --tail 50
docker logs gateway --tail 50
```

---

### Bước 6 — Migrate FishDex DB (clone local → PROD)

**Trên máy local** — dump DB từ local PostgreSQL (port 5433):
```powershell
pg_dump -h localhost -p 5433 -U fishdex -d fishdex -F c -f fishdex_dump.dump
```

**Copy dump lên VM:**
```powershell
scp -i <path-to-private-key> fishdex_dump.dump ubuntu@<ORACLE_VM_IP>:~/
```

**Restore trên VM:**
```bash
docker exec -i postgres pg_restore -U fishlover -d fishdex < ~/fishdex_dump.dump
```

---

### Bước 7 — Smoke test

- [ ] `curl http://<ORACLE_VM_IP>:5000` — gateway phản hồi
- [ ] Mở `http://<domain>/swagger` của FishDex — Swagger hiển thị
- [ ] Test 1-2 endpoint FishDex qua Swagger
- [ ] Test login qua UserManagement

---

## Lưu ý
- AquaHome bỏ qua trong đợt deploy này
- `GHCR_TOKEN` hết hạn **03/07/2026** — nhớ rotate trước ngày đó
- FE deploy trên Cloudflare Pages — nginx chỉ proxy đến ApiGateway, không serve static
- `nginx.conf` đã có tại `Pipeline/OracleVM/nginx.conf` — nhớ đổi `<YOUR_DOMAIN>` trước khi dùng
- SSL cert (`~/app/ssl/fullchain.pem` + `privkey.pem`) cần lấy từ Let's Encrypt trước khi start nginx
- Sau khi ổn định: xem xét tắt Swagger trên PROD
