# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0-alpha.1] - 2026-04-29

### Added
- Complete rewrite as `Microsoft.Azure.Cosmos.OData`
- SOLID architecture with pluggable interfaces (`IFieldNameResolver`, `ISqlFunctionMapper`, `ISqlExpressionRenderer`)
- Immutable `SqlExpression` AST replacing string concatenation
- `ODataToCosmosSqlTranslator` orchestrator with clause builders
- `TranslatedQuery` result type with SQL + parameters + optional count SQL
- `ODataQueryClauses` framework-agnostic input DTO
- Parameterized queries by default (`@p0`, `@p1`, ...)
- `OFFSET ... LIMIT ...` pagination (replaces `TOP`)
- `$count=true` → companion `SELECT VALUE COUNT(1)` query
- `$apply` → aggregate/groupby → `GROUP BY`
- `$search` → `FullTextContains(c, 'term')`
- `any()` / `all()` → `EXISTS` sub-queries
- `in` operator → OR chain
- `matchesPattern` → `RegexMatch`
- Math functions: `round`, `floor`, `ceiling`
- Date functions: `year`, `month`, `day`, `hour`, `minute`, `second` → `DateTimePart`
- Geospatial: `geo.distance` → `ST_DISTANCE`, `geo.intersects` → `ST_INTERSECTS`
- Vector search: `vectordistance` → `VectorDistance`
- Full-text search: `fulltextcontains` → `FullTextContains`, etc.
- `IS_DEFINED` / `ARRAY_CONTAINS` via OData function extensions
- `BinaryOperatorKind.Has` support (enum flags)
- `AggregatedCollectionPropertyNode` visitor
- `Microsoft.Azure.Cosmos.OData.AspNetCore` adapter package with DI integration
- `Microsoft.Azure.Cosmos.OData.Cosmos` bridge package (TranslatedQuery → QueryDefinition)
- GitHub Actions CI (build + test on ubuntu/windows/macos)
- xUnit test suite (20 tests)
- `.editorconfig`, `Directory.Build.props`, central package management
- Comprehensive README and MIGRATION.md

### Changed
- **BREAKING:** Namespace changed from `Microsoft.Azure.Documents.OData.Sql` to `Microsoft.Azure.Cosmos.OData`
- **BREAKING:** Package renamed from `Microsoft.Azure.Documents.OData.Sql` to `Microsoft.Azure.Cosmos.OData`
- **BREAKING:** Target framework changed from `net452` to `net8.0`/`net9.0`/`net10.0`
- **BREAKING:** OData upgraded from 6.x to 8.x
- **BREAKING:** Entry point changed from `ODataToSqlTranslator` + `SQLQueryFormatter` to `ODataToCosmosSqlTranslator`
- **BREAKING:** Input changed from `ODataQueryOptions` to `ODataQueryClauses`
- **BREAKING:** Output changed from `string` to `TranslatedQuery`
- **BREAKING:** Options changed from `TranslateOptions` enum to `TranslationOptions` record

### Removed
- Legacy `net452` support
- `QueryFormatterBase` / `SQLQueryFormatter` (replaced by focused interfaces)
- `Constants` grab-bag class
- `ODataNodeToStringBuilder` (replaced by `ODataExpressionVisitor` + `SqlExpression` AST)
- Legacy GitHub Pages site
- Dependency on ASP.NET Web API 2 / Microsoft.Azure.DocumentDB SDK

## [2.0.2] - 2017 (legacy)

### Added
- Support for functions: `length()`, `indexof()`, `substring()`, `trim()`, `concat()`

## [2.0.1] - 2017 (legacy)

### Added
- Support for functions: `contains()`, `startswith()`, `endswith()`, `toupper()`, `tolower()`

## [2.0.0] - 2017 (legacy)

### Changed
- **BREAKING:** Simplified usage with `ODataToSqlTranslator` class

## [1.0.0] - 2016 (legacy)

### Added
- Initial release
