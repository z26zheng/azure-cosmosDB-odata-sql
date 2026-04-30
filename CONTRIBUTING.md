# Contributing to Microsoft.Azure.Cosmos.OData

Thank you for your interest in contributing! This project welcomes contributions and suggestions.

## Getting started

1. **Fork** the repository and clone your fork
2. Install the [.NET SDK](https://dot.net/download) (8.0 or later)
3. Run `dotnet build` to build all projects
4. Run `dotnet test` to run the test suite

## Development workflow

1. Create a feature branch from `master`: `git checkout -b feature/my-feature`
2. Make your changes with tests
3. Ensure all tests pass: `dotnet test`
4. Ensure the build is clean: `dotnet build --configuration Release`
5. Commit with a [conventional commit](https://www.conventionalcommits.org/) message
6. Push and open a Pull Request

## Commit message format

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add support for $compute
fix: handle null values in trim() translation
docs: update README with new function mappings
test: add tests for geospatial function mapper
refactor: extract clause builder interface
chore: update OData dependency to 8.5.0
```

For breaking changes, add `!` after the type: `feat!: rename TranslationOptions`

## Code guidelines

- Follow the `.editorconfig` settings
- All public APIs must have XML documentation comments
- All new features must have corresponding unit tests
- Use `Nullable` reference types (enabled project-wide)
- Prefer records for immutable data types
- Follow the existing architecture:
  - New OData functions → add to or create an `ISqlFunctionMapper` implementation
  - New SQL AST nodes → add to `Ast/SqlExpression.cs`
  - New query options → add property to `ODataQueryClauses`, handle in `ODataToCosmosSqlTranslator`

## Adding a new OData function mapping

1. If the function fits an existing mapper (e.g. a new string function), add it to `DefaultFunctionMapper.cs`
2. If it's a new function family, create a new mapper implementing `ISqlFunctionMapper`
3. Register it in `CompositeFunctionMapper` inside `ODataToCosmosSqlTranslator.DefaultFunctions()`
4. Add a test in `TranslatorTests.cs`
5. Update the function table in `README.md`

## Running specific tests

```bash
dotnet test --filter "FullyQualifiedName~Filter_Contains"
dotnet test --filter "ClassName=TranslatorTests"
```

## Reporting bugs

Please use GitHub Issues with the bug report template. Include:
- The OData query URL
- The expected Cosmos SQL output
- The actual output or exception
- Your .NET version and OS

## Code of Conduct

This project has adopted the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE.txt).
