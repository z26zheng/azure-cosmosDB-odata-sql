# Migration Guide: v1/v2 → v3

This document helps you migrate from `Microsoft.Azure.Documents.OData.Sql` (v1/v2) to `Microsoft.Azure.Cosmos.OData` (v3).

## Breaking changes summary

| Area | v1/v2 | v3 |
|---|---|---|
| **NuGet package** | `Microsoft.Azure.Documents.OData.Sql` | `Microsoft.Azure.Cosmos.OData` |
| **Root namespace** | `Microsoft.Azure.Documents.OData.Sql` | `Microsoft.Azure.Cosmos.OData` |
| **Framework** | .NET Framework 4.5.2 only | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| **OData library** | `Microsoft.OData.Core` 6.x | `Microsoft.OData.Core` 7.x |
| **ASP.NET** | `System.Web.OData` (Web API 2 only) | Framework-agnostic core; adapters for both Web API 2 and ASP.NET Core |
| **Entry point** | `ODataToSqlTranslator` + `SQLQueryFormatter` | `ODataToCosmosSqlTranslator` (no formatter needed) |
| **Input** | `ODataQueryOptions` (ASP.NET type) | `ODataQueryClauses` (framework-agnostic record) |
| **Output** | `string` | `TranslatedQuery` (SQL + parameters + optional count SQL) |
| **Options** | `TranslateOptions` enum (bit flags) | `TranslationOptions` record with named properties |
| **Parameterization** | None (inline only) | Parameterized by default; inline via option |
| **Pagination** | `TOP n` only | `OFFSET … LIMIT …` by default; `TOP` via option |

## Step-by-step migration

### 1. Update NuGet reference

```diff
- <PackageReference Include="Microsoft.Azure.Documents.OData.Sql" Version="2.0.2" />
+ <PackageReference Include="Microsoft.Azure.Cosmos.OData" Version="3.0.0" />
```

### 2. Update using statements

```diff
- using Microsoft.Azure.Documents.OData.Sql;
+ using Microsoft.Azure.Cosmos.OData;
```

### 3. Replace translator construction

```diff
- var translator = new ODataToSqlTranslator(new SQLQueryFormatter());
+ var translator = new ODataToCosmosSqlTranslator();
```

### 4. Build ODataQueryClauses instead of passing ODataQueryOptions directly

**Before (v1/v2):**
```csharp
string sql = translator.Translate(oDataQueryOptions, TranslateOptions.ALL);
```

**After (v3):**
```csharp
// If you have ODataQueryOptions from ASP.NET:
var clauses = new ODataQueryClauses
{
    Filter = oDataQueryOptions.Filter?.FilterClause,
    OrderBy = oDataQueryOptions.OrderBy?.OrderByClause,
    Select = oDataQueryOptions.SelectExpand?.SelectExpandClause,
    Top = oDataQueryOptions.Top?.Value,
    Skip = oDataQueryOptions.Skip?.Value,
    Count = oDataQueryOptions.Count?.Value,
};

TranslatedQuery result = translator.Translate(clauses);
string sql = result.Sql;  // or just use result — it has implicit conversion to string
```

### 5. Update TranslateOptions → TranslationOptions

**Before:**
```csharp
translator.Translate(options, TranslateOptions.SELECT_CLAUSE | TranslateOptions.WHERE_CLAUSE);
translator.Translate(options, TranslateOptions.ALL & ~TranslateOptions.TOP_CLAUSE);
```

**After:**
```csharp
translator.Translate(clauses, new TranslationOptions
{
    Clauses = TranslationClauses.Select | TranslationClauses.Filter,
});

translator.Translate(clauses, new TranslationOptions
{
    Clauses = TranslationClauses.All & ~TranslationClauses.Pagination,
});
```

### 6. Handle the additionalWhereClause parameter

**Before:**
```csharp
translator.Translate(options, TranslateOptions.ALL, additionalWhereClause: "c.type = 'company'");
```

**After:**
```csharp
translator.Translate(clauses, new TranslationOptions
{
    AdditionalWhereClause = "c.type = 'company'",
});
```

### 7. Use parameterized queries (recommended)

v3 parameterizes all literals by default. To get the old inline behavior:

```csharp
var opts = new TranslationOptions { Parameterization = ParameterizationMode.Inline };
```

To use parameterized queries with the Cosmos SDK:

```csharp
var result = translator.Translate(clauses);
var queryDef = new QueryDefinition(result.Sql);
foreach (var (key, value) in result.Parameters)
    queryDef = queryDef.WithParameter(key, value);
```

### 8. Pagination: OFFSET/LIMIT vs TOP

v3 defaults to `OFFSET … LIMIT …` (modern Cosmos best practice). To keep the old `TOP` behavior:

```csharp
var opts = new TranslationOptions { Pagination = PaginationMode.Top };
```

## Custom formatters → interfaces

If you subclassed `QueryFormatterBase` in v1/v2, the equivalent in v3 is to implement the relevant interface:

| v1/v2 method | v3 interface |
|---|---|
| `TranslateFieldName()` | `IFieldNameResolver.TranslateFieldName()` |
| `TranslateSource()` | `IFieldNameResolver.TranslateSource()` |
| `TranslateEnumValue()` | `IFieldNameResolver.TranslateEnumValue()` |
| `TranslateFunctionName()` | `ISqlFunctionMapper.Map()` |

Then pass your implementations to the `ODataToCosmosSqlTranslator` constructor.
