#!/bin/bash
set -e

# Tạo databases + users riêng per service
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "postgres" <<EOSQL
CREATE DATABASE usermanagement;
CREATE DATABASE fishdex;
CREATE DATABASE aquahome;

CREATE USER "${UM_DB_USER}" WITH PASSWORD '${UM_DB_PASSWORD}';
CREATE USER "${FD_DB_USER}" WITH PASSWORD '${FD_DB_PASSWORD}';
CREATE USER "${AH_DB_USER}" WITH PASSWORD '${AH_DB_PASSWORD}';

GRANT ALL PRIVILEGES ON DATABASE usermanagement TO "${UM_DB_USER}";
GRANT ALL PRIVILEGES ON DATABASE fishdex        TO "${FD_DB_USER}";
GRANT ALL PRIVILEGES ON DATABASE aquahome       TO "${AH_DB_USER}";
EOSQL

# PostgreSQL 15+ cần grant schema riêng
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "usermanagement" <<EOSQL
GRANT ALL ON SCHEMA public TO "${UM_DB_USER}";
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "fishdex" <<EOSQL
CREATE EXTENSION IF NOT EXISTS vector;
-- pg_trgm: dò tên loài gần giống khi user submit loài lai (bắt lỗi gõ và tên thương mại).
-- Tạo bằng superuser ở đây cho khớp cách 'vector' đang làm. Migration EnableTrigramSearch
-- cũng có CREATE EXTENSION IF NOT EXISTS để phủ các DB đã dựng từ trước — file này chỉ chạy
-- lần đầu khi container postgres khởi tạo volume.
CREATE EXTENSION IF NOT EXISTS pg_trgm;
GRANT ALL ON SCHEMA public TO "${FD_DB_USER}";
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "aquahome" <<EOSQL
GRANT ALL ON SCHEMA public TO "${AH_DB_USER}";
EOSQL
