-- ─────────────────────────────────────────────────────────────────────────────
-- Reset sequences sau khi ETL.
-- Khi ETL insert explicit PK values (autoctr, AutoCtr...), sequences trong
-- PostgreSQL không tự update. Nếu sau ETL có thêm bản ghi qua EF Core
-- (auto-gen Id), sẽ bị duplicate key. Chạy file này một lần sau ETL.
-- ─────────────────────────────────────────────────────────────────────────────

SELECT setval(
    pg_get_serial_sequence('"Ecologies"', 'EcologyId'),
    GREATEST(COALESCE((SELECT MAX("EcologyId") FROM "Ecologies"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"HabitatZones"', 'HabitatZoneId'),
    GREATEST(COALESCE((SELECT MAX("HabitatZoneId") FROM "HabitatZones"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"FeedingAndDiets"', 'FeedingId'),
    GREATEST(COALESCE((SELECT MAX("FeedingId") FROM "FeedingAndDiets"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"Associations"', 'AssociationId'),
    GREATEST(COALESCE((SELECT MAX("AssociationId") FROM "Associations"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"Substrates"', 'SubstrateId'),
    GREATEST(COALESCE((SELECT MAX("SubstrateId") FROM "Substrates"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"SpecialHabitats"', 'SpecialHabitatId'),
    GREATEST(COALESCE((SELECT MAX("SpecialHabitatId") FROM "SpecialHabitats"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"CircadianBehaviors"', 'CircadianId'),
    GREATEST(COALESCE((SELECT MAX("CircadianId") FROM "CircadianBehaviors"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"Ecosystems"', 'AutoCtr'),
    GREATEST(COALESCE((SELECT MAX("AutoCtr") FROM "Ecosystems"), 0), 1)
);
SELECT setval(
    pg_get_serial_sequence('"CommonNames"', 'AutoCtr'),
    GREATEST(COALESCE((SELECT MAX("AutoCtr") FROM "CommonNames"), 0), 1)
);
