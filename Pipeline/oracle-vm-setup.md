# Oracle Cloud VM — Setup Guide

## Thông tin VM

| | VM1 (app) | VM2 (data/ai) |
|-|-----------|--------------|
| Shape | A1.Flex | A1.Flex |
| OCPU | 2 | 2 |
| RAM | 12 GB | 12 GB |
| Storage | 90 GB | (chưa tạo) |
| OS | Ubuntu 24.04 | Ubuntu 24.04 |
| Region | Singapore | Singapore |

---

## Bước 1 — SSH vào VM

```bash
chmod 400 ~/path/to/your-key.key
ssh -i ~/path/to/your-key.key ubuntu@<PUBLIC_IP>
```

---

## Bước 2 — Mở firewall Oracle (Security List)

Oracle có 2 lớp firewall: **Security List** (cloud level) và **UFW** (OS level). Phải mở cả 2.

**Trên Oracle Console:**
Networking → Virtual Cloud Networks → VCN của bạn → Security Lists → Default Security List → Add Ingress Rules:

| Source CIDR | Protocol | Port | Dùng cho |
|-------------|----------|------|---------|
| 0.0.0.0/0 | TCP | 22 | SSH |
| 0.0.0.0/0 | TCP | 80 | HTTP |
| 0.0.0.0/0 | TCP | 443 | HTTPS |
| 0.0.0.0/0 | TCP | 5000 | API Gateway |

---

## Bước 3 — Mở UFW (Ubuntu firewall)

```bash
sudo ufw allow 22
sudo ufw allow 80
sudo ufw allow 443
sudo ufw allow 5000
sudo ufw enable
sudo ufw status
```

---

## Bước 4 — Cài Docker + Docker Compose

```bash
sudo apt update && sudo apt upgrade -y

# Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ubuntu

# Log out rồi SSH lại để group có hiệu lực
exit
```

SSH lại, verify:
```bash
docker --version
docker compose version
```

---

## Bước 5 — Keepalive (tránh Oracle reclaim)

```bash
# Ping localhost mỗi 5 phút để VM không bị coi là idle
(crontab -l 2>/dev/null; echo "*/5 * * * * curl -s http://localhost > /dev/null 2>&1") | crontab -
```

> Khi đã deploy Nginx + services thật, không cần cron job này nữa — traffic thật giữ VM active.

---

## Bước 6 — Clone repo + chạy Docker Compose

```bash
# Tạo thư mục app
mkdir -p ~/app && cd ~/app

# Clone repo (hoặc dùng CI/CD deploy)
git clone https://github.com/TheEastPham/FishDex.git .

# Copy env files
cp Pipeline/FrontEndLocal/.env.example Pipeline/FrontEndLocal/.env
# Chỉnh các giá trị trong .env

# Chạy
cd Pipeline/FrontEndLocal
docker compose up -d
```

---

## CI/CD — Azure DevOps deploy lên Oracle VM

Xem file `Pipeline/azure-devops/deploy-oracle.yml`
