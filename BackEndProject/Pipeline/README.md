# Pipeline

Thư mục này chứa toàn bộ pipeline config cho 3 BE services: **FishDex**, **UserManagement**, **AquaHome**.

---

## Cấu trúc

```
Pipeline/
  FishDexLocal/            Local Docker stack cho FishDex
  UserManagementLocal/     Local Docker stack cho UserManagement
  AquaHomeLocal/           Local Docker stack cho AquaHome
  github-actions/          CI/CD templates cho GitHub Actions
  azure-devops/            CI/CD templates cho Azure DevOps
```

---

## Local Docker

Mỗi service có docker-compose riêng. Chạy từ thư mục tương ứng:

```bash
# FishDex (PostgreSQL 16 + pgAdmin)
cd Pipeline/FishDexLocal
docker compose up -d

# UserManagement (SQL Server 2022 + Redis 7)
cd Pipeline/UserManagementLocal
docker compose up -d

# AquaHome (SQL Server 2022 + Redis 7)
cd Pipeline/AquaHomeLocal
docker compose up -d
```

### Port mapping

| Service           | Component   | Host Port |
|-------------------|-------------|-----------|
| FishDex           | PostgreSQL  | 5433      |
| FishDex           | pgAdmin     | 5050      |
| UserManagement    | SQL Server  | 1433      |
| UserManagement    | Redis       | 6379      |
| AquaHome          | SQL Server  | 1434      |
| AquaHome          | Redis       | 6380      |

AquaHome dùng port offset +1 so với UserManagement để có thể chạy song song.

### Credentials (local only)

| Service        | Component  | User       | Password                  |
|----------------|------------|------------|---------------------------|
| FishDex        | PostgreSQL | fishdex    | fishdex_local_pwd         |
| FishDex        | pgAdmin    | admin@fishdex.local | admin123         |
| UserManagement | SQL Server | sa         | UserMgmt_Local_Pwd1!      |
| AquaHome       | SQL Server | sa         | AquaHome_Local_Pwd1!      |

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

### TODO khi chốt deployment

Tìm các comment `# TODO` trong pipeline files và điền:
1. Login + push Docker image lên container registry (ACR / GHCR / ECR)
2. Deploy command tới target environment
