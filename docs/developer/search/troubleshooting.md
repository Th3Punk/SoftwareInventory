# Search – Troubleshooting

## Search returns no results / 501

1. Verify `Features:Search:Enabled: true` in active config.
2. Verify `Features:Search:Provider: "PostgresFts"`.
3. Check the API response — `IsAvailable: false` in the 200 body means the provider returned unavailable.

## Migration not applied

The `SearchVector` columns require migration `202507161500_Search_FtsIndexes`. Run the SQL script in `sql/migrations/202507161500_Search_FtsIndexes.sql` against the database.

## Poor search quality for Hungarian text

Set `Features:Search:Language: "hungarian"`. Then re-create the generated columns so the stored `tsvector` uses Hungarian stemming:

```sql
ALTER TABLE "Applications" DROP COLUMN "SearchVector";
ALTER TABLE "Applications" ADD COLUMN "SearchVector" tsvector GENERATED ALWAYS AS (
    to_tsvector('hungarian', "Name" || ' ' || "ShortDescription" || ' ' || COALESCE("DetailedDescription", '') || ' ' || "OwnerTeam")
) STORED;
-- same for Documentations
```

## GIN index not used (slow queries)

The GIN expression index is used only when the `WHERE "SearchVector" @@ ...` clause matches the indexed column. Verify that the `"SearchVector"` column exists and the index is created:

```sql
\d "Applications"
```

## ts_headline returns null

`ts_headline` can return null if the document is very short. The provider handles this with `reader.IsDBNull(2)` and returns an empty string.
