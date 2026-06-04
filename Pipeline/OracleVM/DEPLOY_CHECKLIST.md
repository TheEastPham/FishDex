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

### Bước 4 — Khởi động services trên VM

```bash
# Login GHCR trước
echo "<GHCR_TOKEN>" | docker login ghcr.io -u TheEastPham --password-stdin

# Chạy DB + Redis trước
docker compose -f ~/app/docker-compose.prod.yml up -d postgres redis

# Đợi postgres healthy (~15 giây), init-db.sh sẽ tự chạy tạo users/databases
sleep 15
docker compose -f ~/app/docker-compose.prod.yml ps

# Start tất cả services
docker compose -f ~/app/docker-compose.prod.yml up -d
```

Kiểm tra:
```bash
docker compose -f ~/app/docker-compose.prod.yml ps
docker logs fishdex --tail 50
docker logs usermanagement --tail 50
```

---

### Bước 5 — Migrate FishDex DB (local → PROD)

**Dump từ local** (port 5433):
```powershell
pg_dump -h localhost -p 5433 -U fishdex -d fishdex -F c -f fishdex_dump.dump
```

**Copy lên VM:**
```powershell
scp -i "C:\Users\Lenovo\.ssh\ssh-key-2026-06-01.key" `
  fishdex_dump.dump ubuntu@<ORACLE_VM_IP>:~/
```

**Restore trên VM** (dùng postgres admin user):
```bash
docker exec -i postgres pg_restore -U postgres -d fishdex < ~/fishdex_dump.dump
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
