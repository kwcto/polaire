# Polaire

**Expressive DataFrames for C# and .NET, inspired by [Polars](https://pola.rs).**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

> "I love C# and I feel that it is underrated for exploratory data analysis."

Polaire is a ground-up C# implementation that brings Polars-style data manipulation to .NET. The goal is **complete feature parity and reasonable performance parity**, with an API that feels natural in C#. Correctness and speed are equally important.

The engine combines Apache Arrow columnar storage, null validity bitmaps, expression trees, lazy query optimization, and ARM NEON / x64 AVX intrinsics. It is under active development; the implemented features below do not yet establish full Polars compatibility.

## Current status

As of **October 21, 2026**:

- **2,527 tests passing**, with no failures or skipped tests.
- **.NET 8 / C# 12**; the solution builds with no warnings or errors.
- SQL, window functions, rolling/expanding/EWM expressions, JSON/NDJSON writing, temporal parsing/formatting, and horizontal aggregations are implemented.
- Important gaps remain in type support, I/O fidelity, advanced transformations, and execution. See [known limitations](#known-limitations) and the [feature gap analysis](docs/FEATURE_GAP_ANALYSIS.md).

For scale, the **Polars `py-2.0.0` Python suite collected 42,076 test cases** on October 21, 2026, including 42,053 unit cases. The former estimate of 5,000–10,000 tests was too low. These counts include parameterized cases; they are not a measure of feature coverage or proof that tests have been faithfully ported. The [gap analysis](docs/FEATURE_GAP_ANALYSIS.md) records the collection method and breakdown.

## Build and use from source

Install the .NET 8 SDK, then:

```bash
git clone https://github.com/kwcto/polaire.git
cd polaire
dotnet build
dotnet test
```

Reference the library from a .NET 8 application:

```bash
dotnet add /path/to/your-app.csproj reference /path/to/polaire/src/Polaire/Polaire.csproj
```

## Quick start

```csharp
using Polaire;
using static Polaire.Pl;

var df = DataFrame(
    Series("name", new[] { "Alice", "Bob", "Charlie", "Diana" }),
    Series("team", new[] { "A", "B", "A", "B" }),
    Series("age", new[] { 25, 30, 35, 28 }),
    Series("score", new[] { 85.5, 92.0, 78.5, 95.0 })
);

var result = df
    .Lazy()
    .Filter(Col("age").Gt(26))
    .Select(Col("name"), (Col("score") * 1.1).As("adjusted_score"))
    .Collect();

result.Print();

var summary = df
    .Lazy()
    .GroupBy("team")
    .Agg(
        Col("score").Mean().As("avg_score"),
        Col("name").CountExpr().As("count")
    )
    .Collect();

summary.Print();
```

Lazy grouping currently requires column keys. For a computed key, create a column with `WithColumns` before grouping by its name. The eager `df.GroupBy(...)` API uses aggregation tuples or a dictionary instead of expressions:

```csharp
var eagerSummary = df.GroupBy("team")
    .Agg(("score", "mean", s => s.Mean(), "avg_score"));
```

## Implemented capabilities

| Area | Available today |
|---|---|
| Series | Arithmetic, comparisons, null handling, sorting, uniqueness, casting, cumulative operations, shift/diff, rank, interpolation, rolling and EWM operations |
| DataFrames | Selection, filtering, sorting, grouping, joins, sampling, melt, vertical/horizontal stacking |
| Expressions | Arithmetic, conditions, aggregations, string and temporal operations, window functions, rolling/expanding/EWM, horizontal aggregations |
| Lazy execution | Query plans, projection and predicate optimizer passes, constant folding, plan explanation, limit/fetch |
| SQL | `SqlContext` and `DataFrame.Sql`, including SELECT, WHERE, GROUP BY, HAVING, ORDER BY, LIMIT, JOIN and CASE |
| I/O | CSV, Parquet, JSON and NDJSON readers/writers; CSV, Parquet and NDJSON lazy scans |

Common integer, floating-point, Boolean, string, Date and DateTime columns are supported. List and Struct columns have dedicated operations, with remaining integration gaps. Defining a data type does not imply complete construction, computation, or file-format support; see the [type inventory](docs/FEATURE_GAP_ANALYSIS.md).

### SQL, windows and rolling calculations

These examples continue with the `df` from the quick start:

```csharp
var sqlSummary = df.Sql(
    "SELECT team, AVG(score) AS avg_score FROM self GROUP BY team");

var enriched = df.Lazy()
    .WithColumns(
        Col("score").Mean().Over(Col("team")).As("team_avg"),
        Col("score").RollingMeanExpr(3).As("rolling_avg"),
        SumHorizontal("age", "score").As("row_total")
    )
    .Collect();
```

For queries across multiple named DataFrames, register them with `Polaire.Sql.SqlContext`. SQL uses the existing expression and lazy execution engine; SQL syntax support is a subset of Polars SQL.

### Date parsing and formatting

```csharp
var dates = Series("date", new[] { "2024-01-31", "2024-02-29" })
    .Str.ToDate("%Y-%m-%d");

var nextMonth = dates.Dt.OffsetBy("1mo");
var formatted = nextMonth.Dt.Strftime("%d %b %Y");
```

Formats containing `%` use Polars/chrono-style specifiers; .NET formats such as `"yyyy-MM-dd"` are also supported. `OffsetBy` accepts calendar-aware duration strings such as `"1mo"` and `"1y2mo3d"`.

### Reading, writing and lazy scans

```csharp
var csv = ReadCsv("data.csv");
var parquet = ReadParquet("data.parquet");
var json = ReadJson("data.json");
var ndjson = ReadNdjson("data.ndjson");

df.WriteCsv("output.csv");
df.WriteParquet("output.parquet");
df.WriteJson("output.json");
df.WriteNdjson("output.ndjson");

var query = ScanCsv("data.csv")
    .Filter(Col("age").Gt(26))
    .Select("name", "score");

Console.WriteLine(query.Explain());
var scanned = query.Collect();
```

Scans defer reading until collection and pass projected columns to readers. Scan predicates currently run after the reader has materialized its DataFrame. Lazy scanning therefore does not yet provide streaming execution or guarantee that only matching rows are read.

## Known limitations

- **Parquet writing:** Date/DateTime columns are written as strings; Int8, Int16 and unsigned integer columns can throw `InvalidCastException`.
- **Types:** Time and Duration have type definitions but no usable Series construction/operations. Other specialized types also lack complete support.
- **Nested columns:** concatenating List or Struct columns throws `NotSupportedException`. Lazy `Explode` exists as an API but throws `NotImplementedException` during execution.
- **Transformations:** as-of joins, pivot, transpose and dynamic time-based grouping remain unimplemented.
- **Execution and formats:** streaming execution, IPC/Arrow file I/O and GPU acceleration remain unimplemented.
- **Parity:** API availability, passing tests and individual benchmarks do not establish complete behavioral compatibility with Polars.

The [feature gap analysis](docs/FEATURE_GAP_ANALYSIS.md) tracks the broader inventory and test-porting work. Full feature parity remains the goal.

## Performance

Architecture-specific intrinsics, pooled memory, chunked columns and query optimization support the goal of competitive .NET performance. Optimizations must preserve values, types and null semantics across scalar and vectorized paths.

### Historical aggregation measurements

The following **January 2026** results were recorded on an **Apple M1 Max with .NET 8**, using **1,000,000 Float64 values**:

| Operation | Polaire | Polars | Recorded comparison |
|---|---:|---:|---|
| Sum | 128 µs | 101 µs | Polaire about 1.3× slower |
| Mean | 127 µs | 104 µs | Polaire about 1.2× slower |
| Min | 107 µs | 101 µs | Similar timings |
| Max | 104 µs | 101 µs | Similar timings |
| Std | 269 µs | 624 µs | Polaire about 2.3× faster |

These are historical measurements of selected basic aggregations, not a current comparison against Polars 2.0.0 or evidence of parity across joins, grouping, I/O and full queries. The recorded comparison does not identify the original Polars version. [`benchmarks/polars_comparison.py`](benchmarks/polars_comparison.py) measures the installed Polars version but reuses hardcoded historical Polaire timings; rerunning it alone does not refresh both sides.

Run Polaire benchmarks with:

```bash
dotnet run -c Release --project benchmarks/Polaire.Benchmarks/
```

## Contributing

Contributions toward correctness, feature completeness, performance and documentation are welcome. When porting Polars tests, preserve the original intent, assertions and static values where possible, and record their upstream source. Add checks for actual values, types and nulls, including edge cases; raw test counts are not a substitute for behavioral coverage.

Use [AGENTS.md](AGENTS.md) for project objectives and working guidance, [CLAUDE.md](CLAUDE.md) for API gotchas and implementation lessons, and the [feature gap analysis](docs/FEATURE_GAP_ANALYSIS.md) for remaining work. Keep documentation aligned with verified behavior.

### Secret checks and commit privacy

Install the pinned [Gitleaks](https://github.com/gitleaks/gitleaks) scanner and enable
the local hooks once per clone on macOS or Linux:

```bash
bash scripts/install-gitleaks.sh
bash scripts/setup-hooks.sh
```

The installer verifies the release archive against a pinned SHA-256 checksum and
keeps the executable inside Git's local metadata directory. The hooks require the
installed scanner; they do not download software during a commit or push.

The pre-commit hook checks staged changes. The pre-push hook checks the full history
reachable from each commit being pushed, so committing a secret and then deleting
it does not hide it. Findings are redacted. Missing tools or scanner errors block
the operation. You can also run the checks explicitly:

```bash
bash scripts/check-secrets.sh staged
bash scripts/check-secrets.sh history
```

[GitHub Actions](.github/workflows/secret-scan.yml) repeats the history scan on pushes
and pull requests. That check runs after upload; local hooks provide the check
before upload. Hooks must be enabled separately in each clone, and pattern-based
scanning does not replace reviewing the staged diff for private information.

Use your [GitHub no-reply email](https://docs.github.com/en/account-and-profile/how-tos/email-preferences/setting-your-commit-email-address)
for public commits. Configure it per repository with `git config --local user.email`
and your account's no-reply address. This changes future commits; it does not remove
personal email addresses from existing history.

## License

MIT License — see [LICENSE](LICENSE).
