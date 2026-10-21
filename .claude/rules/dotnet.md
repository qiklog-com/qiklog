---
paths:
  - "src/**/*.cs"
  - "src/**/*.razor"
  - "tests/**/*.cs"
---
# .NET rules (summary; full rules in .cursor/rules/qiklog.mdc)

- .NET 9, file-scoped namespaces, `sealed` by default, records for DTOs.
- Nullable on, warnings as errors. No `!` without a comment.
- Async all the way, pass `CancellationToken`, no `.Result` or `.Wait()`.
- `ILogger<T>` with named placeholders, never string interpolation.
- Tests: xUnit + Shouldly, `Given_X_When_Y_Then_Z`, `// Given:` `// When:` `// Then:` comments.
- Brand colors only via `var(--ql-*)` tokens in `brand.css`.
