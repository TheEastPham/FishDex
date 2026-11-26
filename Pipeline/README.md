# Pipeline

Thư mục này chứa toàn bộ pipeline config cho 3 BE services: **FishDex**, **UserManagement**, **AquaHome**.

---

## Cấu trúc

```
Pipeline/
  local/
    docker-compose.local.yml   Toàn bộ stack backend local (nguồn duy nhất); biến lấy từ local/.env
    FishDex/             Script ETL FishBase, seed, init.sql cho DB FishDex
    FrontEnd/            Docker stack cho FE (aquahome-web), chạy riêng
  OracleVM/              Production Docker Compose cho Oracle VM
  azure-devops/          4 pipeline CI/CD đang chạy (Azure DevOps)
```

Dockerfile của từng service nằm cạnh project trong `BackEndProject/` (`ApiGateway/Dockerfile`, `UserManagement/UserManagement.API/Dockerfile`, `FishDex/FishDex.API/Dockerfile`, `AquaHome/AquaHome.API/Dockerfile`, `AquaHome/AquaHome.Worker/Dockerfile`), build context là `BackEndProject/`. Cả compose local lẫn pipeline production đều dùng chung các file này.

---

## Local Docker

Một file duy nhất: `local/docker-compose.local.yml`, biến môi trường đọc từ `local/.env` (tạo từ `local/.env.example`).

```bash
cd Pipeline/local
cp .env.example .env          # lần đầu, rồi điền giá trị

# Toàn bộ stack: DB, Redis, 3 API, worker, gateway
docker compose -f docker-compose.local.yml up -d

# Chỉ hạ tầng, khi chạy API bằng `dotnet run` / `dotnet watch` trên máy
docker compose -f docker-compose.local.yml up -d usermanagement-db usermanagement-redis fishdex-db aquahome-db aquahome-redis
```

### Port mapping

| Service           | Component   | Host Port |
|-------------------|-------------|-----------|
| ApiGateway        | API         | 5000      |
| UserManagement    | API         | 8080      |
| UserManagement    | PostgreSQL  | 5435      |
| UserManagement    | Redis       | 6379      |
| FishDex           | API         | 8081      |
| FishDex           | PostgreSQL  | 5433      |
| FishDex           | pgAdmin     | 5050      |
| AquaHome          | API         | 8082      |
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

4 pipeline trong `azure-devops/` (`apigateway`, `usermanagement`, `fishdex`, `aquahome`), đã tạo trên Azure DevOps ("Existing Azure Pipelines YAML file"). Mỗi pipeline build image (linux/arm64) từ Dockerfile cạnh project rồi push lên GHCR; sửa Dockerfile cũng kích hoạt build vì nó nằm trong `BackEndProject/<Service>/**`; VM chỉ pull về chạy (xem `OracleVM/shared/DEPLOY_CHECKLIST.md`).

| Khi nào chạy | Điều kiện |
|---|---|
| Push lên `release/v1.2` | Có thay đổi trong `BackEndProject/<Service>/**` (3 service kèm `BackEndProject/Share/**`), trừ file `.md` |
| Pull request | Không chạy |

Khi đổi nhánh phát hành (ví dụ sang `release/v1.2`), sửa `trigger.branches.include` ở **cả 4 file**.

FE (`aquahome-web`) không đi qua Azure DevOps: Cloudflare Pages tự build khi push, xem `cloudflare-pages-setup.md`.
