# Polaire Feature Gap Analysis & Test Porting Plan

Updated: October 21, 2026. Implementation baseline: commit `9438de7`.

Polaire aims for **complete feature parity and reasonable performance parity with Polars**, using an idiomatic C# API. Correctness and performance are both requirements. This document replaces the January inventory, estimated coverage percentages, and speculative week-by-week schedule.

The current baseline is **2,527 passing Polaire tests**, with no failures or skips. Polaire targets .NET 8 and C# 12. The reference Polars measurement is **42,076 collected Python test cases for `py-2.0.0`**, detailed below. Neither count establishes feature coverage or proves that the upstream tests have been faithfully ported.

The Polaire baseline was verified on October 21 with `dotnet build --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` (zero warnings/errors), followed by `dotnet test --no-build --no-restore` (2,527 passed, zero failed/skipped).

## Polars test count and provenance

On October 21, 2026, the Polars Python suite was collected from tag **`py-2.0.0`**, commit prefix **`22a147d`** (October 6, 2026). The environment used Python **3.12.13**, the published **Polars 2.0.0** package, and **pytest 9.0.3**, with optional test dependencies installed. The successful collection exited with code 0 and reported no collection errors or skips.

| Measure | Recorded count | Meaning |
|---------|---------------:|---------|
| All Python test cases | **42,076** | Parametrized cases counted individually; all markers included |
| Unit cases | 42,053 | Cases under `tests/unit` |
| Benchmark cases | 22 | Cases under `tests/benchmark` |
| Documentation cases | 1 | Case under `tests/docs` |
| Distinct test functions | 8,488 | Collected node IDs with parameter suffixes removed |
| Parametrized functions | 2,200 | Expand into 35,788 collected cases |
| Files containing collected tests | 417 | Not the total number of Python files in the test tree |
| Default marker selection | 24,448 | The other 17,628 cases are deselected by the default configuration |
| Rust test attributes | 1,792 | Text search: 1,782 `#[test]` plus 10 `#[tokio::test]` attributes |

The collection command, run from the checked-out `py-polars` directory, was:

```bash
python -m pytest tests --collect-only -q -m "" -rs -p no:cacheprovider
```

The empty marker expression overrides Polars' default exclusions. Omitting `-m ""` produced the 24,448-case default selection. **Collection does not execute the tests.** The Rust number is a source-text count, not a compiled inventory; macro-generated tests and build-feature differences can change the executable count. Do not add it to the Python total as a measure of distinct behavior.

These figures were recovered from the retained development-session command output, rather than collected again for this documentation update. The full node-ID list was saved in a temporary directory and is no longer available. A dependency lockfile was not preserved, so any future recount must record its environment and report its own result. The original estimate of “5,000–10,000+ tests” and the “5,000+ tests” completion target are superseded.

Polaire's xUnit total also expands parameterized cases, but its tests are not yet mapped one-to-one to the upstream inventory. Test-count ratios are **not** feature coverage, behavioral coverage, code coverage, or a percentage of the port completed.

## Current implementation

“Implemented” here means code and relevant tests exist for the listed capability. It does not mean every Polars option, dtype, edge case, or API layer has equivalent behavior. Check the Series API, expression builders, evaluator, and lazy executor separately before declaring a feature complete.

### Data types and storage

| Area | Current support | Remaining limits |
|------|-----------------|------------------|
| Primitive columns | Array constructors for signed/unsigned 8/16/32/64-bit integers, Float32/64, Boolean, and String | Operations and I/O do not support every dtype uniformly. `FromNullable<T>` supports only Int32/64, Float32/64, Boolean, Date, and DateTime. |
| Date and DateTime | Construction, scalar reads, temporal operations, comparisons, and type-preserving gathering/concatenation | Parquet writes them as strings. Full timezone and nanosecond behavioral parity is not established. |
| List and Struct | Dedicated chunked arrays and Series namespaces; list access/aggregation/slicing/explode and struct field access/unnest | Concatenation is unsupported; expression namespaces and lazy DataFrame explode are incomplete or absent. |
| Other declared types | The type model includes Time, Duration, Decimal, 128-bit integers, Binary, Array, Categorical, Enum, Object, and other descriptors | A type descriptor does not establish usable Series construction, compute support, or I/O round trips. Time/Duration construction and operations remain missing. |

Sources: [Series factories](../src/Polaire/Series/Series.cs), [type definitions](../src/Polaire/DataTypes/DataType.cs), [List operations](../src/Polaire/Compute/ListOperations.cs), [Struct operations](../src/Polaire/Compute/StructOperations.cs).

Date columns store Arrow Date32 days since the Unix epoch; DateTime columns store timestamp units since that epoch. `AnyValue` uses .NET representations. All conversions must use [TemporalConversions](../src/Polaire/Core/TemporalConversions.cs). `DateTime.Kind` is ignored on input; scalar output uses `Utc`. See [CLAUDE.md](../CLAUDE.md) for the API and storage rules.

### Operations and expressions

| Area | Implemented capabilities | Limits and further work |
|------|--------------------------|-------------------------|
| Series fundamentals | Arithmetic/comparisons, Boolean operations, null handling, casts, sort/argsort, unique/value counts, slicing/take, concat/append, and `NUnique` | Dtype promotion, null semantics, and boundary behavior still need systematic upstream comparison. Concat requires matching types and rejects unsupported nested types. |
| Aggregation and numeric operations | Sum/mean/min/max/std/var/median/count, product, quantile, clip, absolute value, and numeric transforms | The expression/grouped aggregation surfaces are narrower than the Series surface. |
| Window and sequence operations | Partitioned `.Over()`, cumulative operations, shift/lead/lag, diff/pct-change, rank variants, interpolation, and forward/backward fill | This is implemented functionality, not a claim of complete Polars window semantics. |
| Rolling, expanding, and EWM | Series operations and expression builders for common sums, means, extrema, standard deviation, variance, and related operations | Series-only operations such as rolling quantile/apply are not all exposed as expression builders; time-based windows and dynamic grouping remain gaps. |
| String operations | Case conversion, stripping/padding, substring/split, matching/replacement/extraction, lengths, find/reverse, hex/base64 encode/decode, repeat, JSON-path extraction, and temporal parsing | The expression namespace exposes a subset. Full Unicode, regex, nested-output, and upstream string-API parity remains to be assessed. |
| Temporal operations | Date parts, truncation/rounding helpers, epoch extraction, calendar boundaries, timezone conversion helpers, `Strptime`/`Strftime`, and `OffsetBy` | Polars-style `%` formats and duration strings are supported within the implemented parser subset; general temporal parity and Time/Duration operations remain incomplete. |
| Horizontal operations | `SumHorizontal`, `MeanHorizontal`, `MinHorizontal`, and `MaxHorizontal` | Other upstream horizontal/fold operations need inventory and ports. |

Sources: [Series API](../src/Polaire/Series/Series.cs), [expression builders](../src/Polaire/Expressions/Expr.cs), [expression evaluator](../src/Polaire/Expressions/ExprEvaluator.cs), and [compute implementations](../src/Polaire/Compute/).

Relevant tests include [window expressions](../tests/Polaire.Tests/WindowExpressionTests.cs), [rolling expressions](../tests/Polaire.Tests/RollingExpressionTests.cs), [expression additions](../tests/Polaire.Tests/ExpressionAdditionsTests.cs), and [temporal parsing/formatting](../tests/Polaire.Tests/TemporalParsingFormattingTests.cs).

### DataFrames, joins, SQL, and lazy execution

| Area | Current support | Remaining limits |
|------|-----------------|------------------|
| DataFrame operations | Selection/filtering, column updates, sort, grouping, unique, sampling, describe, melt, vertical/horizontal stacking | Pivot, transpose, and dynamic grouping are missing. Do not infer support for every Polars reshape operation. |
| Joins | Inner, left, right, outer, semi, anti, and cross joins in eager execution | As-of and inequality joins are missing; complete upstream join semantics have not been demonstrated. Lazy join keys must be column references. |
| Grouped aggregation | Eager GroupBy and lazy aggregation expressions | Lazy group keys must be column references. Materialize computed keys into columns before grouping. |
| SQL | `SqlContext` and `DataFrame.Sql`; SELECT, filtering, supported aggregations/grouping, HAVING, sorting, LIMIT/OFFSET, joins, CASE, and other supported expressions | SQL reuses the expression/lazy pipeline and inherits its limitations. Only SELECT statements are accepted; UNION/INTERSECT set operations are unsupported. This is not complete Polars SQL coverage. |
| Lazy execution | Select/WithColumns/Filter/Sort, grouping, joins, limits/tail/slice, drop/rename/distinct, union/concat, schema inspection, `Explain`, and `Fetch` | `Fetch` is `Head(n).Collect()`, not a streaming reader. File-scan schema inspection currently returns an empty schema. `LazyFrame.Explode` builds a plan but execution throws `NotImplementedException`. |
| Optimizer | Predicate/projection passes, constant folding, expression simplification, filter combination, and limit combination | No implemented common-subexpression-elimination pass. Predicate pushdown through joins is not active. General slice pushdown, streaming execution, profiling/graph output, and sink APIs remain gaps. |
| Cache | A logical cache node exists | Execution simply executes its input; it does not cache results. |

Sources: [DataFrame](../src/Polaire/DataFrame/DataFrame.cs), [joins](../src/Polaire/DataFrame/JoinOperations.cs), [SQL translation](../src/Polaire/Sql/SqlToLogicalPlan.cs), [LazyFrame](../src/Polaire/LazyFrame/LazyFrame.cs), [executor](../src/Polaire/LazyFrame/PlanExecutor.cs), and [optimizer](../src/Polaire/LazyFrame/QueryOptimizer.cs). Tests include [SQL](../tests/Polaire.Tests/SqlTests.cs), [extended joins](../tests/Polaire.Tests/JoinExtendedTests.cs), and [lazy evaluation](../tests/Polaire.Tests/LazyEvaluationTests.cs).

### I/O

| Format | Read | Write | Lazy scan | Known limitations |
|--------|------|-------|-----------|-------------------|
| CSV | Yes | Yes | Yes | Full Polars option/semantic parity is not established. |
| Parquet | Yes | Yes, with dtype gaps | Yes | Date/DateTime serialize as strings. Int8/Int16 and unsigned integer writes use incompatible scalar conversions and can throw `InvalidCastException`. |
| JSON | Yes | Yes | No dedicated JSON-array scan | Numeric inference was corrected in the latest implementation. |
| NDJSON | Yes | Yes | Yes | Full nested-type and option parity is not established. |
| IPC/Arrow files | No | No | No | Arrow-backed in-memory storage does not imply Arrow file-format support. |
| Avro, Excel, Delta Lake, Iceberg, database connectors | No dedicated implementation | No dedicated implementation | No dedicated implementation | Future integrations; no parity claim. |

File-scan projections are passed to the readers. Predicates are evaluated **after the reader returns a materialized DataFrame**; this is not row-group pruning or streaming predicate evaluation. The current lazy APIs therefore do not establish out-of-core execution.

Sources: [I/O implementations](../src/Polaire/IO/), [file-scan execution](../src/Polaire/LazyFrame/PlanExecutor.cs), [I/O tests](../tests/Polaire.Tests/IO/), and [JSON writer tests](../tests/Polaire.Tests/JsonWriterTests.cs).

## Correctness lessons that govern further work

Recent fixes show why API presence and passing shape-only tests are insufficient:

- Date32 values were interpreted using the wrong epoch, timestamp values used the wrong unit, and DateTime scalar reads returned null. Row selection also changed temporal and narrow/unsigned integer columns into strings. [Temporal correctness tests](../tests/Polaire.Tests/TemporalCorrectnessTests.cs) now check values and types.
- JSON numeric columns were inferred as all-null Booleans while row-count tests still passed.
- SIMD min/max and scalar paths disagreed on NaN behavior at different input lengths. Keep vector boundaries, scalar remainders, nulls, infinities, and NaNs in regression coverage.
- Projection analysis must retain columns referenced by window, slice, and other expressions. A plausible plan or successful call can still return the wrong rows or values.

Tests should assert values, dtypes, null positions, ordering, and expected errors as applicable. Test eager, expression, lazy, SQL, and I/O paths where they expose the same behavior. Never change an upstream expected result merely to make the current implementation pass.

## Test porting strategy

The user's requirement is to port **all of the upstream tests**, preserving their intent and integrity and retaining static values where possible. The previous arbitrary 5,000-test target is not a completion criterion.

The next step is a traceable inventory, not a larger unclassified total. The repository does **not yet contain a complete upstream-to-Polaire mapping**. Create one against a pinned Polars revision with, for each upstream test or parametrized case:

- Upstream path, test name/case ID, and source revision.
- Corresponding xUnit test and preserved inputs/expected outputs.
- Status: ported and passing, ported and failing, blocked by a missing feature, or awaiting port.
- Any .NET-specific adaptation and its rationale. Python-only bindings, external integrations, and runtime-specific tests need explicit treatment rather than silent omission.

Retain upstream test names where practical. Use xUnit `[Fact]` and `[Theory]` to preserve cases, not to inflate totals. Tests blocked by an unimplemented feature must remain visible in the inventory rather than disappearing from the plan. Record missing behavior separately from API naming differences.

Add property-based and integration tests where they cover behavior beyond the direct ports. FsCheck remains a proposed addition, not an installed testing dependency. Validate data round trips, optimized versus unoptimized/eager results, chunk boundaries, empty/all-null inputs, numeric extremes, temporal boundaries, and combinations of operations.

For a future upstream recount, retain the tag/full SHA, Python and Polars versions, dependency lockfile, command, collection output, marker selection, and complete case-ID manifest in a durable location. Count both the full suite and any selected subset, and report collection errors/skips explicitly.

## Proposed work order

This is a suggested engineering sequence, not a committed schedule or a reduction of the parity goal.

1. **Establish the parity inventory and protect existing behavior.** Map current tests to upstream cases and close weak value/type assertions, especially for numerical, null, temporal, join, and optimizer behavior.
2. **Finish incomplete paths already exposed by the API.** Repair Parquet type preservation and integer conversions; add nested-column concatenation; implement lazy explode; test these through public APIs and round trips.
3. **Close common analysis gaps.** Add as-of joins, pivot/transpose, dynamic grouping/time-based windows, Time/Duration support, and missing expression-layer operations.
4. **Extend execution and interoperability.** Implement IPC/Arrow files, streaming/sinks, actual caching, and further optimizer capabilities with correctness and workload benchmarks.
5. **Continue through the full remaining inventory.** Advanced dtypes, external formats/connectors, plugins, and GPU acceleration need explicit design and testing. Their later placement does not make the current library feature-complete.

## Performance and completion criteria

Historical M1 Max/.NET 8 measurements showed competitive performance for selected Float64 aggregations at one million rows. They do not establish performance parity for joins, filtering, grouping, I/O, SQL, or complete workloads. The [comparison script](../benchmarks/polars_comparison.py) times Polars while using hardcoded historical Polaire results; it is not a fresh two-engine benchmark. See the [README performance section](../README.md#performance) for the historical figures and limits.

Profile before optimizing, and compare equivalent inputs, semantics, versions, and hardware. Short targeted benchmark runs are useful when their variance is acceptable; broader claims require representative workloads. Keep correctness checks around fast paths and fallbacks. Parallel aggregation currently uses a 10-million-element threshold because copying data for parallel work can outweigh the speedup at smaller sizes.

| Measure | Current evidence | Completion criterion |
|---------|------------------|----------------------|
| Polaire test execution | 2,527 passing cases | Relevant existing and new tests pass without weakening upstream expectations |
| Upstream test parity | Complete mapping not yet available | Every upstream test accounted for; applicable behavior ported and verified, adaptations documented |
| Feature parity | Partial implementation summarized above | Verified behavior across supported types, API layers, options, and edge cases |
| Performance parity | Historical selected aggregation measurements | Reasonable performance parity on representative, reproducible workloads |
| Code coverage | No current measured figure recorded | Measure coverage and use uncovered branches to guide meaningful tests; do not infer it from test totals |

Keep this inventory and the README current as gaps close. Preserve significant implementation rules and fixes in [CLAUDE.md](../CLAUDE.md), and project-wide instructions for Codex in [AGENTS.md](../AGENTS.md).
