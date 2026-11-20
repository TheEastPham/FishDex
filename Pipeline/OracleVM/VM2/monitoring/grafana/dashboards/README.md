# Grafana Dashboards (provisioned)

Mọi file `*.json` trong thư mục này được Grafana tự load vào folder **FishLover**
(theo `provisioning/dashboards/dashboards.yaml`). Quét lại mỗi 30s.

## Cách thêm dashboard

1. Trong Grafana UI: mở dashboard → **Settings (⚙️) → JSON Model** → copy toàn bộ JSON.
2. Lưu thành file `<tên>.json` trong thư mục này.
3. **Sửa datasource uid** trong JSON về uid cố định đã provision:
   - Prometheus → `"uid": "prometheus"`
   - Loki → `"uid": "loki"`
   - Tempo → `"uid": "tempo"` (sau khi làm 10.4)
   (Tránh để uid ngẫu nhiên do import tay — recreate container sẽ lệch.)
4. Commit file lên git. Recreate Grafana → dashboard tự xuất hiện.

## Đã bỏ

`logs.json` (import ID 14055) đã xoá: dashboard đó viết cho Kubernetes — dùng label `app`,
metric `kube_pod_*` và recording rule `log_messages_total` của loki-mixin, không cái nào tồn tại
trong setup Docker này. Thay bằng `loki-stack.json` viết tay theo label thật (`service`, `container`, `host`).

## Dashboard dự kiến của Epic 10

| File | Nguồn | Task |
|------|-------|------|
| `containers.json` | Import ID 14282 (cAdvisor) | 10.3 |
| `loki-stack.json`  | Tự viết (task 2 monitoring follow-up) | 10.2 |
| `app-logs.json`    | Tự viết — soi log 4 service BE hằng ngày | task 3 |
| `vm1-host.json`   | Import ID 1860 (Node Exporter Full) | 10.5 |
