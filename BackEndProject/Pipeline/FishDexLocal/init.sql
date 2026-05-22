-- Run once when the PostgreSQL container first initialises.
-- Enables the pgvector extension required by FishDex.
CREATE EXTENSION IF NOT EXISTS vector;
