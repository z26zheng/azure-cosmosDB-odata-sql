---
name: Feature Request
about: Suggest a new OData query option, function, or feature
title: "[FEATURE] "
labels: enhancement
assignees: ''
---

## Is your feature request related to a problem?

<!-- A clear and concise description of the problem. -->

## Describe the desired behavior

**OData input:**
```
$filter=myNewFunction(field, 'value')
```

**Expected Cosmos SQL output:**
```sql
WHERE MY_NEW_FUNCTION(c.field, 'value')
```

## Alternatives considered

<!-- Have you considered alternative approaches? -->

## Additional context

<!-- Links to OData spec sections, Cosmos DB documentation, etc. -->
