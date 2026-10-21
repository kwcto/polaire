# Polaire: guidance for Codex

## Mission and motivation

Polaire is a ground-up C#/.NET DataFrame library inspired by Polars. The goals are
**complete feature parity**, **reasonable performance parity**, and an idiomatic
C# API. This is a .NET 8 / C# 12 library, despite its location under a `web` directory.

The project's motivation, in the maintainer's words:

> I love C# and I feel that it is underrated for exploratory data analysis.

The maintainer deliberately revised the README to this wording in commit
`c026598` ("Motivation!"). Emphasize C#'s potential for exploratory data analysis.
Familiarity with Polars should transfer, while the API should feel natural in .NET.

## The maintainer's standing guidance

- **Be correct and fast.** Both are requirements. Pay particular attention to edge
  cases that could silently produce miscalculated or corrupted results.
- **Port the upstream tests faithfully.** The request is to port all Polars tests,
  preserving their intent, integrity, and static input/expected values wherever
  possible. An arbitrary test-count milestone is not completion of this goal.
  Record .NET adaptations and reasons a case is not applicable; do not silently
  exclude difficult cases or weaken expectations to match the implementation.
- **Measure performance.** Reasonable parity with optimized Rust is a guiding
  principle. Profile and benchmark performance-sensitive changes; favor shorter,
  targeted benchmark runs when they provide a useful sample, and disclose their
  limitations.
- **Keep continuity between sessions.** Maintain concise notes about significant
  decisions, workarounds, bug fixes, and optimization techniques. Keep public
  documentation current. Avoid turning agent instructions into a session diary.
- **Parallelize work when useful**, with separate ownership of files or areas to
  avoid conflicting edits.
- **Keep local configuration private.** This is a public repository.
  `.claude/settings.local.json` must remain ignored and must not be committed.

These directions were recovered from the January 2026 project prompt history
and the October 2026 sessions. Historical assistant proposals, rough estimates,
and suggested schedules are not additional user requirements or verified facts.

## Start here

- [README.md](README.md): setup, public examples, and a concise current overview.
- [docs/FEATURE_GAP_ANALYSIS.md](docs/FEATURE_GAP_ANALYSIS.md): current inventory,
  remaining gaps, the pinned Polars test census, and test-porting strategy.
- [CLAUDE.md](CLAUDE.md): detailed API gotchas, temporal representation, and
  optimization lessons. Check current source when older notes disagree.
- `Directory.Build.props` and the `.csproj` files: actual compiler settings and
  dependency versions; do not infer these from an older documentation table.

The October 21, 2026 baseline is **2,527 passing Polaire test cases**. The separately
measured Polars **`py-2.0.0`** inventory is **42,076 collected Python test cases**,
including **42,053 unit cases**. Collection is not execution, and a ratio of test
counts is neither feature coverage nor proof of faithfully ported tests. See the
gap analysis for the command, environment, breakdown, and provenance limits.

## Architecture and code locations

| Area | Location / role |
| --- | --- |
| Storage | `src/Polaire/Core/`, `DataTypes/`, `Memory/`: Arrow-backed chunked arrays, values, validity, pooling |
| Public API | `Series/`, `DataFrame/`, `Polaire.cs`: Series, DataFrame, GroupBy, joins, and `Pl` helpers |
| Computation | `Compute/`: typed kernels, SIMD, strings, temporal, rolling/window/EWM and nested operations |
| Expressions | `Expressions/`: expression trees and evaluation |
| Lazy queries | `LazyFrame/`: logical plans, optimizer, executor |
| SQL | `Sql/`: SQL parsing into the existing expressions and logical-plan pipeline |
| I/O | `IO/`: CSV, Parquet, JSON and NDJSON readers/writers |
| Validation | `tests/Polaire.Tests/` (xUnit), `benchmarks/Polaire.Benchmarks/` (BenchmarkDotNet) |

Check a feature through its entire execution path: a declared data type, public
method, expression node, or logical plan does not establish working support.
Series, eager DataFrame, expression, lazy, SQL, and I/O coverage can differ.

## Correctness rules learned from previous work

- Test **actual values, types, null positions, and ordering**, not only shape or
  successful execution. Old shape-only tests missed incorrect date epochs,
  all-null numeric JSON reads, and transformations silently changing types.
- Preserve physical types through `Take`, filtering, sorting, unique, concatenation,
  and I/O. Do not use string conversion as an implicit unsupported-type fallback.
- Dates store Arrow Date32 **days since 1970-01-01**; timestamps store **units since
  the Unix epoch** in the column's `TimeUnit`. `AnyValue` uses .NET day numbers/ticks.
  Convert through `Core/TemporalConversions`, never interpret stored values directly
  as `DateOnly.DayNumber` or `DateTime.Ticks`.
- `Series.FromValues(DateTime[])` stores wall-clock values, ignores input `Kind`,
  and returns UTC-kind values. Do not change that behavior incidentally.
- Keep SIMD, scalar, null-aware, and parallel paths semantically consistent.
  Cover NaNs, infinities, empty/all-null inputs, and lengths around vector/tail
  boundaries. A previous SIMD min/max bug depended on input length.
- Verify both eager and lazy results where relevant, including optimizer effects.
  Test the result of a query, not merely that its plan contains an optimization.

## API pitfalls

- `Series` and `DataFrame` are in the root `Polaire` namespace. `Pl` uses type
  aliases because its factory methods have those same names.
- Expression arithmetic uses operators. Eager `GroupBy.Agg` uses tuples such as
  `("value", "sum", s => s.Sum(), "total")`; lazy `Agg` accepts expressions.
- Expression comparisons use builders such as `.Gt(26)` and `.Eq("active")`;
  a column-count expression uses `.CountExpr()`. Verify examples against the API.
- Lazy grouping currently requires column keys. Materialize computed keys with
  `WithColumns` before grouping.
- Descending lazy sort uses `.Sort((Col("total"), true))`.
- `Series.NullCount` is a property; `DataFrame.Rename` takes a dictionary.
- Window functions use `.Over(...)`; several builders use an `Expr` suffix,
  such as `RankExpr`, `RollingMeanExpr`, and `SliceExpr`. Check their signatures.
- Temporal format strings containing `%` use supported Polars/chrono specifiers;
  other formats use .NET conventions. `OffsetBy` accepts duration strings like `1mo`.

## Validation and performance

```sh
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~TemporalCorrectnessTests"
dotnet run -c Release --project benchmarks/Polaire.Benchmarks/ -- --filter '*SeriesAggregationBenchmarks*'
```

Warnings are treated as errors (missing XML documentation warnings are suppressed).
Run checks appropriate to the change; report what ran and any limitations. Build
before using `dotnet test --no-build`. For README/API examples, compile and exercise
the example against the current public API when changing its behavior or syntax.

Historical M1 Max results demonstrate competitiveness on selected basic Float64
aggregations, not performance parity across the library. The Python comparison
script hardcodes old Polaire timings while measuring Polars afresh; do not present
it as a fresh measurement of both implementations. Record versions, hardware,
input types/sizes/null patterns, and method when making new comparisons.

Architecture-specific intrinsics, four accumulators, and direct memory access
produced previous gains. Copying data for parallel execution also introduced
overhead; the documented parallel threshold is 10 million elements. Measure before
extending these patterns to another operation or changing thresholds.

## Maintaining the project record

Keep durable guidance here, technical gotchas in `CLAUDE.md`, public examples in
the README, and the changing feature/test inventory in the gap analysis. When a
feature lands, update affected documentation in the same change. Distinguish
implemented subsets from full parity, measured results from estimates, and proposed
next work from completed work. Preserve unrelated local changes.
