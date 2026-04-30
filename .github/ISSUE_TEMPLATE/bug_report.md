---
name: Bug Report
about: Report a translation error or unexpected behavior
title: "[BUG] "
labels: bug
assignees: ''
---

## Describe the bug

<!-- A clear and concise description of what the bug is. -->

## To reproduce

**OData query:**
```
http://localhost/Entity?$filter=...&$orderby=...&$top=...
```

**Expected Cosmos SQL:**
```sql
SELECT * FROM c WHERE ...
```

**Actual Cosmos SQL (or error):**
```sql
-- paste actual output here
```

## Configuration

```csharp
var options = new TranslationOptions
{
    Parameterization = ParameterizationMode.Inline, // or Parameters
    Pagination = PaginationMode.OffsetLimit,         // or Top
};
```

## Environment

- Package version: [e.g. 3.0.0]
- .NET version: [e.g. net8.0]
- OS: [e.g. Windows 11, macOS 15, Ubuntu 24.04]

## Additional context

<!-- Add any other context about the problem here. -->
