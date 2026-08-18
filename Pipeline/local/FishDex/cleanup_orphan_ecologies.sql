-- ─────────────────────────────────────────────────────────────────────────────
-- Dọn các dòng "Ecologies" rác do bản ETL cũ sinh ra.
--
-- Bản cũ insert Ecologies mà KHÔNG truyền "EcologyId", để serial tự sinh. Sub-table
-- (FeedingAndDiets / Associations / HabitatZones / Substrates / SpecialHabitats /
-- CircadianBehaviors) thì khoá theo autoctr, nên các dòng đó mồ côi hoàn toàn.
-- Bản ETL hiện tại luôn ghi EcologyId = autoctr, nên điều kiện nhận dạng là
-- "EcologyId <> autoctr".
--
-- Hậu quả nếu để nguyên: một loài có 2 dòng Ecology, BE chọn phải dòng rác thì cả
-- khối Sinh thái học của loài đó biến mất trên FE. Đo trên local 15/08: 627 dòng rác
-- ảnh hưởng 627 loài.
--
-- BE đã có guard (EcologyService.GetBySpecCodeAsync ưu tiên EcologyId = autoctr) nên
-- file này không bắt buộc — nhưng chạy thì DB sạch và query bớt một dòng phải lọc.
--
-- Chạy:
--   LOCAL  docker exec -i fishdex_postgres psql -U fishdex -d fishdex < cleanup_orphan_ecologies.sql
--   PROD   docker exec -i postgres psql -U postgres -d fishdex < cleanup_orphan_ecologies.sql
-- ─────────────────────────────────────────────────────────────────────────────

BEGIN;

-- Kiểm tra an toàn trước khi xoá. Cả ba số phải bằng 0, nếu không thì ROLLBACK.
--   1. dòng lệch mà vẫn có sub-row  → không phải rác, đừng xoá
--   2. loài chỉ có mỗi dòng lệch    → xoá là mất trắng dữ liệu của loài đó
DO $$
DECLARE
    v_with_subrow int;
    v_only_orphan int;
    v_to_delete   int;
BEGIN
    SELECT count(*) INTO v_with_subrow
    FROM "Ecologies" e
    WHERE e."EcologyId" <> e."autoctr"
      AND (EXISTS (SELECT 1 FROM "FeedingAndDiets"    x WHERE x."EcologyId" = e."EcologyId")
        OR EXISTS (SELECT 1 FROM "Associations"       x WHERE x."EcologyId" = e."EcologyId")
        OR EXISTS (SELECT 1 FROM "HabitatZones"       x WHERE x."EcologyId" = e."EcologyId")
        OR EXISTS (SELECT 1 FROM "Substrates"         x WHERE x."EcologyId" = e."EcologyId")
        OR EXISTS (SELECT 1 FROM "SpecialHabitats"    x WHERE x."EcologyId" = e."EcologyId")
        OR EXISTS (SELECT 1 FROM "CircadianBehaviors" x WHERE x."EcologyId" = e."EcologyId"));

    SELECT count(*) INTO v_only_orphan FROM (
        SELECT "SpecCode" FROM "Ecologies" WHERE "EcologyId" <> "autoctr"
        EXCEPT
        SELECT "SpecCode" FROM "Ecologies" WHERE "EcologyId"  = "autoctr"
    ) t;

    SELECT count(*) INTO v_to_delete FROM "Ecologies" WHERE "EcologyId" <> "autoctr";

    IF v_with_subrow > 0 OR v_only_orphan > 0 THEN
        RAISE EXCEPTION
            'Dừng lại: % dòng lệch vẫn có sub-row, % loài chỉ có mỗi dòng lệch. Kiểm tra tay trước khi xoá.',
            v_with_subrow, v_only_orphan;
    END IF;

    RAISE NOTICE 'Sẽ xoá % dòng Ecologies rác.', v_to_delete;
END $$;

DELETE FROM "Ecologies" WHERE "EcologyId" <> "autoctr";

COMMIT;

-- Sequence đang trỏ quá MAX cũ sau khi xoá — kéo về cho khớp.
SELECT setval(
    pg_get_serial_sequence('"Ecologies"', 'EcologyId'),
    GREATEST(COALESCE((SELECT MAX("EcologyId") FROM "Ecologies"), 0), 1)
);
