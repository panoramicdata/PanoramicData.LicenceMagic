# Contributing

Thank you for contributing to PanoramicData.LicenceMagic.

## Workflow

1. Fork the repository.
2. Create a branch for the change.
3. Add or update tests as appropriate.
4. Run `dotnet test --configuration Release`.
5. Open a pull request against `main`.

## Standards

- Target .NET 10.
- Use file-scoped namespaces and tabs for indentation.
- Keep nullable reference types and warnings-as-errors enabled.
- Document public APIs with XML comments where practical.
- Preserve compatibility with existing licence signatures unless documenting a deliberate breaking change.
- Ensure builds complete without diagnostics and all tests pass.

Contributions are licensed under the repository's MIT License.
