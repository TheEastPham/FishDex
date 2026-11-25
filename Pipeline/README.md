# Pipeline

Thư mục này chứa toàn bộ pipeline config cho 3 BE services: **FishDex**, **UserManagement**, **AquaHome**.

---

## Cấu trúc

```
Pipeline/
  local/
    FishDex/             Local Docker stack cho FishDex
    UserManagement/      Local Docker stack cho UserManagement
    AquaHome/            Local Docker stack cho AquaHome
    ApiGateway/          Local Docker stack cho ApiGateway
    FrontEnd/            Local Docker stack cho FrontEnd
  OracleVM/              Production Docker Compose cho Oracle VM
  azure-devops/          4 pipeline CI/CD đang chạy (Azure DevOps)
```

Lưu ý: image **production** cũng được build từ các `Dockerfile.*` trong `local/<Service>/` (4 pipeline Azure trỏ vào đó).

---

## Local Docker

Mỗi service có docker-compose riêng trong `local/`. Chạy từ thư mục tương ứng:

```bash
# FishDex (PostgreSQL 16 + pgAdmin)
cd Pipeline/local/FishDex
docker compose up -d

# UserManagement (PostgreSQL 16 + Redis 7)
cd Pipeline/local/UserManagement
docker compose up -d

# AquaHome (PostgreSQL 16 + Redis 7)
cd Pipeline/local/AquaHome
docker compose up -d
```

### Port mapping

| Service           | Component   | Host Port |
|-------------------|-------------|-----------|
| FishDex           | PostgreSQL  | 5433      |
| FishDex           | pgAdmin     | 5050      |
| UserManagement    | PostgreSQL  | 5435      |
| UserManagement    | Redis       | 6379      |
| AquaHome          | PostgreSQL  | 5434      |
| AquaHome          | Redis       | 6380      |

### Credentials (local only)

| Service        | Component  | User            | Password                  |
|----------------|------------|-----------------|---------------------------|
| FishDex        | PostgreSQL | fishdex         | fishdex_local_pwd         |
| FishDex        | pgAdmin    | admin@fishdex.local | admin123             |
| UserManagement | PostgreSQL | usermanagement  | UserMgmt_Local_Pwd1!      |
| AquaHome       | PostgreSQL | aquahome        | AquaHome_Local_Pwd1!      |

---

## CI/CD — Azure DevOps

4 pipeline trong `azure-devops/` (`apigateway`, `usermanagement`, `fishdex`, `aquahome`), đã tạo trên Azure DevOps ("Existing Azure Pipelines YAML file"). Mỗi pipeline build image từ `local/<Service>/Dockerfile.*` rồi push lên GHCR; VM chỉ pull về chạy (xem `OracleVM/shared/DEPLOY_CHECKLIST.md`).

| Khi nào chạy | Điều kiện |
|---|---|
| Push lên `release/v1.1` | Có thay đổi trong `BackEndProject/<Service>/**` (3 service kèm `BackEndProject/Share/**`), trừ file `.md` |
| Pull request | Không chạy |

Khi đổi nhánh phát hành (ví dụ sang `release/v1.2`), sửa `trigger.branches.include` ở **cả 4 file**.

FE (`aquahome-web`) không đi qua Azure DevOps: Cloudflare Pages tự build khi push, xem `cloudflare-pages-setup.md`.
