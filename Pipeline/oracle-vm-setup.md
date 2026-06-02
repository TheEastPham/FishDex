# Oracle Cloud VM — Setup Guide

## Thông tin VM

| | VM1 (app) |
|-|-----------|
| Shape | VM.Standard.A1.Flex (ARM) |
| OCPU / RAM | 2 OCPU / 12 GB |
| Storage | 90 GB |
| OS | Ubuntu 24.04 |

---

## Bước 1 — SSH từ Windows

Private key Oracle download về có dạng `ssh-key-2026-xx-xx.key`.

**Dùng PowerShell (Windows 10/11 có sẵn OpenSSH):**

```powershell
# Fix permission cho key file (Windows yêu cầu)
icacls "C:\Users\YourName\Downloads\ssh-key-xxxx.key" /inheritance:r
icacls "C:\Users\YourName\Downloads\ssh-key-xxxx.key" /grant:r "$($env:USERNAME):(R)"

# SSH vào VM
ssh -i "C:\Users\YourName\Downloads\ssh-key-xxxx.key" ubuntu@<PUBLIC_IP>
```

**Hoặc dùng Git Bash (đơn giản hơn):**

```bash
chmod 400 ~/Downloads/ssh-key-xxxx.key
ssh -i ~/Downloads/ssh-key-xxxx.key ubuntu@<PUBLIC_IP>
```

> Lưu key vào `C:\Users\YourName\.ssh\` cho gọn, tránh để trong Downloads.

---

## Bước 2 — Mở firewall Oracle (làm trên Console)

**Oracle Console → Networking → Virtual Cloud Networks → VCN → Security Lists → Default → Add Ingress Rules:**

| Port | Dùng cho |
|------|---------|
| 22 | SSH |
| 80 | HTTP |
| 443 | HTTPS |
| 5000 | API Gateway |

---

## Bước 3 — Mở UFW trên VM (sau khi SSH vào)

```bash
sudo ufw allow 22 && sudo ufw allow 80 && sudo ufw allow 443 && sudo ufw allow 5000
sudo ufw enable
```

---

## Bước 4 — Cài Docker

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ubuntu
```

**Logout rồi SSH lại** để group có hiệu lực, sau đó verify:

```bash
docker --version
docker compose version
```

---

## Bước 5 — Keepalive (chạy 1 lần)

```bash
(crontab -l 2>/dev/null; echo "*/5 * * * * curl -s http://localhost > /dev/null 2>&1") | crontab -
```

---

## Tối nay chỉ cần làm đến Bước 4 là xong.
## Deploy services làm sau khi có đủ Dockerfiles + docker-compose.prod.yml.
