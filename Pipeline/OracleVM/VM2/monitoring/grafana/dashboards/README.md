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

## Dashboard dự kiến của Epic 10

| File | Nguồn | Task |
|------|-------|------|
| `containers.json` | Import ID 14282 (cAdvisor) | 10.3 |
| `logs.json`       | Import ID 14055 (Loki Logs) | 10.2 |
| `vm1-host.json`   | Import ID 1860 (Node Exporter Full) | 10.5 |
