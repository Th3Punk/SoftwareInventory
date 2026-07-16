# Search – Configuration

## Config Keys

```json
{
  "Features": {
    "Search": {
      "Enabled": true,
      "Provider": "PostgresFts",
      "Language": "simple"
    }
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `Features:Search:Enabled` | bool | `false` | Enables search. When false, `NullSearchProvider` is used and endpoints return 501. |
| `Features:Search:Provider` | string | required if Enabled | Currently only `"PostgresFts"` is supported. |
| `Features:Search:Language` | string | `"simple"` | PostgreSQL text search configuration. Use `"simple"` for language-neutral, `"english"`, or `"hungarian"`. Must match the GIN index language if expression indexes are used. |

## Language Notes

The default `simple` configuration disables stemming but works for any language. For Hungarian content, set `Language: "hungarian"` — however, this also requires recreating the generated column with the matching language config (re-run the migration `Down()` then `Up()` after changing the config).
