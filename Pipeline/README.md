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
  github-actions/        CI/CD templates cho GitHub Actions
  azure-devops/          CI/CD templates cho Azure DevOps
```

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

## CI/CD Templates

Chưa chốt platform (GitHub Actions vs Azure DevOps). Templates có sẵn cho cả hai.

### GitHub Actions

Files ở `github-actions/*.yml` là templates — **không tự trigger**. Khi chốt platform:

```
cp Pipeline/github-actions/fishdex.yml          .github/workflows/fishdex.yml
cp Pipeline/github-actions/usermanagement.yml   .github/workflows/usermanagement.yml
cp Pipeline/github-actions/aquahome.yml         .github/workflows/aquahome.yml
```

### Azure DevOps

Files ở `azure-devops/*-pipeline.yml`. Trong Azure DevOps portal: tạo pipeline → chọn "Existing Azure Pipelines YAML file" → trỏ vào file tương ứng.

### Trigger logic (cả hai platform)

| Branch    | Hành động                              |
|-----------|----------------------------------------|
| `develop` | build → test → docker → deploy **dev** |
| `main`    | build → test → docker → deploy **prod**|

Pipeline chỉ chạy khi có thay đổi trong folder của service đó hoặc `Share/`.
