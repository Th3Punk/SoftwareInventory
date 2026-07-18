-- Migration: 202507161500_Search_FtsIndexes
-- Full-text search vectors for Applications and Documentations

ALTER TABLE "Applications" ADD COLUMN "SearchVector" tsvector GENERATED ALWAYS AS (
    to_tsvector('simple', "Name" || ' ' || "ShortDescription" || ' ' || COALESCE("DetailedDescription", '') || ' ' || "OwnerTeam")
) STORED;

CREATE INDEX "IX_Applications_SearchVector" ON "Applications" USING GIN ("SearchVector");

ALTER TABLE "Documentations" ADD COLUMN "SearchVector" tsvector GENERATED ALWAYS AS (
    to_tsvector('simple', "Title" || ' ' || "Content")
) STORED;

CREATE INDEX "IX_Documentations_SearchVector" ON "Documentations" USING GIN ("SearchVector");

-- Rollback:
-- DROP INDEX IF EXISTS "IX_Applications_SearchVector";
-- ALTER TABLE "Applications" DROP COLUMN IF EXISTS "SearchVector";
-- DROP INDEX IF EXISTS "IX_Documentations_SearchVector";
-- ALTER TABLE "Documentations" DROP COLUMN IF EXISTS "SearchVector";
