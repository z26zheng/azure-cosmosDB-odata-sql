# Security Policy

## Supported versions

| Version | Supported          |
|---------|--------------------|
| 3.x     | ✅ Current          |
| 2.x     | ❌ End of life      |
| 1.x     | ❌ End of life      |

## Reporting a vulnerability

If you discover a security vulnerability in this project, please report it responsibly:

1. **Do NOT** open a public GitHub issue
2. Email the maintainers at the address listed in the repository
3. Or use [GitHub's private vulnerability reporting](https://github.com/z26zheng/azure-documentdb-odata-sql/security/advisories/new)

We will acknowledge receipt within 48 hours and provide a detailed response within 7 days.

## Security considerations

### SQL injection

By default, the translator uses **parameterized queries** (`ParameterizationMode.Parameters`), which prevents SQL injection attacks. All literal values from OData queries are substituted as `@p0`, `@p1`, etc., and returned in the `TranslatedQuery.Parameters` dictionary.

If you use `ParameterizationMode.Inline`, literal values are inlined into the SQL string. **Do not use inline mode with untrusted input.**

The `AdditionalWhereClause` option accepts raw SQL fragments. **Callers must not inline user input into this string.** Use the `AdditionalParameters` dictionary for any user-supplied values.

### Query complexity

The translator does not currently enforce limits on query complexity (nesting depth, number of filter conditions, etc.). Callers should implement their own limits at the API layer to prevent denial-of-service via excessively complex OData queries.

### Dependencies

We monitor dependencies via GitHub Dependabot. Security advisories for transitive dependencies (Microsoft.OData.Core, Microsoft.Azure.Cosmos, etc.) are tracked and updated promptly.
