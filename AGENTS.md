# KeySequence

Standalone Terminal.Gui v2 add-on. Project and NuGet package: `KeySequence`.
Root namespace: `Terminal.Gui.KeySequence`. Target: .NET 10 / C# 14.

- Read README.md and docs/specification.md before changing behavior.
- Keep Terminal.Gui as a NuGet dependency; do not add references to a local checkout.
- Use Terminal.Gui v2 instance APIs and public keyboard events.
- Preserve explicit types, target-typed `new ()`, collection expressions, spaces
  before method parentheses, and guard clauses. Use SubView/SuperView terminology.
- Keep public/internal types in separate files and document public APIs.
- To add tests, use tests/KeySequence.Tests and avoid static application state.

Verification from this repository root:

```powershell
dotnet restore
dotnet build -c Release --no-restore
dotnet test --project tests/KeySequence.Tests -c Release --no-build
dotnet pack src/KeySequence -c Release --no-build -o artifacts
```

Tests use xUnit v3 with Microsoft Testing Platform. To filter tests, use
`--filter-class "*KeySequenceBindingsTests"` or `--filter-method "*MethodName"`.
