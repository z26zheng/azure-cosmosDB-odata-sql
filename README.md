# Microsoft.Azure.Cosmos.OData

[![CI](https://github.com/z26zheng/azure-documentdb-odata-sql/actions/workflows/ci.yml/badge.svg)](https://github.com/z26zheng/azure-documentdb-odata-sql/actions/workflows/ci.yml)

Translates [OData V4](http://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html) queries into [Azure Cosmos DB SQL](https://learn.microsoft.com/azure/cosmos-db/nosql/query/) queries.

> **v3 — complete rewrite.** This version replaces the legacy `Microsoft.Azure.Documents.OData.Sql` package.
> See [MIGRATION.md](MIGRATION.md) for upgrade instructions from v1/v2.

## Key features

| Feature | v1/v2 | v3 |
|---|:---:|:---:|
| `$select` / `$filter` / `$orderby` / `$top` | ✅ | ✅ |
| `$skip` + `$top` → `OFFSET … LIMIT …` | ❌ | ✅ |
| `$count=true` → companion `COUNT` query | ❌ | ✅ |
| `$apply` (aggregate / groupby) → `GROUP BY` | ❌ | ✅ |
| Parameterized queries (`@p0`, `@p1`, …) | ❌ | ✅ (default) |
| `contains`, `startswith`, `endswith` | ✅ | ✅ |
| `toupper` / `tolower` / `length` / `indexof` / `substring` / `trim` / `concat` | ✅ | ✅ |
| `matchesPattern` → `RegexMatch` | ❌ | ✅ |
| Math: `round` / `floor` / `ceiling` | ❌ | ✅ |
| Date parts: `year()`, `month()`, … | ❌ | ✅ |
| `any()` / `all()` → `EXISTS` sub-queries | ❌ | ✅ |
| `in` operator (`x in ('a','b')`) | ❌ | ✅ |
| Geospatial: `geo.distance` / `geo.intersects` → `ST_DISTANCE` / `ST_INTERSECTS` | ❌ | ✅ |
| Vector search: `vectordistance(…)` → `VectorDistance(…)` | ❌ | ✅ |
| Full-text search: `fulltextcontains(…)` → `FullTextContains(…)` | ❌ | ✅ |
| `IS_DEFINED` / `ARRAY_CONTAINS` (via OData function extensions) | ❌ | ✅ |
| Multi-target: `netstandard2.0` / `net8.0` / `net9.0` / `net10.0` | ❌ | ✅ |
| SOLID architecture with pluggable interfaces | ❌ | ✅ |
| ASP.NET Core + ASP.NET Web API 2 adapters | Web API 2 only | Both (planned) |

## Quick start

```csharp
using Microsoft.Azure.Cosmos.OData;

// 1. Create the translator (reuse it — it's thread-safe)
var translator = new ODataToCosmosSqlTranslator();

// 2. Build clauses from an OData URI (or from ASP.NET's ODataQueryOptions via an adapter)
var clauses = new ODataQueryClauses
{
    Filter = parser.ParseFilter(),
    OrderBy = parser.ParseOrderBy(),
    Select = parser.ParseSelectAndExpand(),
    Top = parser.ParseTop(),
    Skip = parser.ParseSkip(),
    Count = parser.ParseCount(),
};

// 3. Translate
TranslatedQuery result = translator.Translate(clauses);

Console.WriteLine(result.Sql);
// SELECT * FROM c WHERE c.revenue > @p0 ORDER BY c.name ASC OFFSET 0 LIMIT 10

Console.WriteLine(result.Parameters);
// { "@p0": 1000 }

// 4. Use with Cosmos SDK
var queryDef = new QueryDefinition(result.Sql);
foreach (var (key, value) in result.Parameters)
    queryDef = queryDef.WithParameter(key, value);
```

## Translation options

```csharp
var options = new TranslationOptions
{
    Clauses = TranslationClauses.All,                    // which clauses to translate
    Parameterization = ParameterizationMode.Parameters,  // default: parameterized SQL
    Pagination = PaginationMode.OffsetLimit,              // default: OFFSET/LIMIT (not TOP)
    DocumentAlias = "c",                                  // the FROM alias
    AdditionalWhereClause = "c.isActive = true",          // extra raw WHERE fragment
};

var result = translator.Translate(clauses, options);
```

### Inline mode (for debugging)

```csharp
var options = new TranslationOptions { Parameterization = ParameterizationMode.Inline };
// Produces: SELECT * FROM c WHERE c.name = 'Microsoft'
```

### Legacy TOP mode

```csharp
var options = new TranslationOptions { Pagination = PaginationMode.Top };
// Produces: SELECT TOP 10 * FROM c ...
```

## Architecture

The engine is designed around **SOLID principles**:

- **`IFieldNameResolver`** — maps OData property names to Cosmos document paths (`englishName` → `c.englishName`). Default: `DefaultFieldNameResolver`.
- **`ISqlFunctionMapper`** — maps OData functions to Cosmos SQL functions (`contains` → `CONTAINS`, `trim` → `LTRIM(RTRIM(…))`). Built-in mappers:
  - `DefaultFunctionMapper` — string, math, date, type-check functions.
  - `GeospatialFunctionMapper` — `geo.distance` → `ST_DISTANCE`, etc.
  - `VectorSearchFunctionMapper` — `vectordistance` → `VectorDistance`.
  - `FullTextSearchFunctionMapper` — `fulltextcontains` → `FullTextContains`, etc.
  - `CompositeFunctionMapper` — composes multiple mappers in priority order.
- **`ISqlExpressionRenderer`** — renders the SQL expression tree to a string with parameter substitution. Default: `CosmosSqlRenderer`.
- **`ODataToCosmosSqlTranslator`** — the orchestrator. Accepts `ODataQueryClauses` (a framework-agnostic DTO) and `TranslationOptions`, delegates to the above.

### SQL expression AST

Instead of concatenating strings, the visitor builds an immutable `SqlExpression` tree:

```
SqlExpression
├── SqlLiteral(value)
├── SqlMember(path)
├── SqlBinary(op, left, right)
├── SqlUnary(op, operand)
├── SqlFunctionCall(name, args)
├── SqlExists(rangeVar, source, predicate)
├── SqlRaw(text)
└── SqlNull
```

This makes the engine testable, extensible, and free of leaky abstractions.

## Supported OData → Cosmos SQL mappings

### System query options

| OData | Cosmos SQL |
|---|---|
| `$select=a,b` | `SELECT c.a, c.b FROM c` |
| `$filter=x eq 5` | `WHERE c.x = 5` |
| `$orderby=x desc` | `ORDER BY c.x DESC` |
| `$top=10&$skip=20` | `OFFSET 20 LIMIT 10` |
| `$top=10` (legacy mode) | `SELECT TOP 10 …` |
| `$count=true` | companion `SELECT VALUE COUNT(1) FROM c …` |
| `$apply=aggregate(price with sum as total)` | `SELECT SUM(c.price) AS total FROM c` |
| `$apply=groupby((cat),aggregate(…))` | `SELECT c.cat, SUM(…) FROM c GROUP BY c.cat` |

### Built-in functions

| OData | Cosmos SQL |
|---|---|
| `contains(field,'value')` | `CONTAINS(c.field,'value')` |
| `startswith(field,'value')` | `STARTSWITH(c.field,'value')` |
| `endswith(field,'value')` | `ENDSWITH(c.field,'value')` |
| `toupper(field)` | `UPPER(c.field)` |
| `tolower(field)` | `LOWER(c.field)` |
| `length(field)` | `LENGTH(c.field)` |
| `indexof(field,'value')` | `INDEX_OF(c.field,'value')` |
| `substring(field,idx1,idx2)` | `SUBSTRING(c.field,idx1,idx2)` |
| `trim(field)` | `LTRIM(RTRIM(c.field))` |
| `concat(field,'value')` | `CONCAT(c.field,'value')` |
| `matchesPattern(field,'^A')` | `RegexMatch(c.field,'^A')` |
| `round(field)` / `floor` / `ceiling` | `ROUND` / `FLOOR` / `CEILING` |
| `geo.distance(loc,point)` | `ST_DISTANCE(c.loc,point)` |
| `geo.intersects(loc,polygon)` | `ST_INTERSECTS(c.loc,polygon)` |
| `vectordistance(embedding,query)` | `VectorDistance(c.embedding,query)` |
| `fulltextcontains(field,'term')` | `FullTextContains(c.field,'term')` |

## Project structure

```
src/
  Microsoft.Azure.Cosmos.OData/        ← engine (netstandard2.0 + net8/9/10)
    Abstractions/                       ← IFieldNameResolver, ISqlFunctionMapper, ISqlExpressionRenderer
    Ast/                                ← SqlExpression record hierarchy
    Errors/                             ← ODataTranslationException hierarchy
    Functions/                          ← Default, Geospatial, Vector, FullText function mappers
    Naming/                             ← DefaultFieldNameResolver
    Options/                            ← TranslationOptions, TranslationClauses, ParameterizationMode, PaginationMode
    Rendering/                          ← CosmosSqlRenderer
    Translation/                        ← ODataExpressionVisitor, ODataToCosmosSqlTranslator
    ODataQueryClauses.cs                ← Framework-agnostic input DTO
    TranslatedQuery.cs                  ← Result (SQL + parameters)
tests/
  Microsoft.Azure.Cosmos.OData.Tests/   ← xUnit tests
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) (coming soon).

## License

[MIT](LICENSE.txt)

## Authors

* **Ziyou Zheng** — original author
* Contributors welcome!
