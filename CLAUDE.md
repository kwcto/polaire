# Polaire - Development Notes

## Project Overview

Polaire is a ground-up C#/.NET implementation inspired by [Polars](https://pola.rs), the high-performance DataFrame library. The goal is **complete feature parity** and **reasonable performance parity** with the Python/Rust Polars library.

## Current State (October 2026)

- **Build:** Passing
- **Tests:** 2527 passing, 0 failing
- **Target:** .NET 8.0
- **I/O:** CSV, Parquet, JSON/NDJSON (read and write). Parquet writes Date/DateTime columns as strings.
- **SQL:** `SqlContext` and `df.Sql("SELECT ... FROM self")`
- **Lazy Scanning:** Implemented with predicate/projection pushdown
- **Performance:** Near parity with Polars for aggregations (see benchmarks below)

### Commands
```bash
dotnet build                    # Build
dotnet test                     # Run tests
dotnet test --filter "TestName" # Run specific test
dotnet run -c Release --project benchmarks/Polaire.Benchmarks/  # Benchmarks
```

## Architecture

### Project Structure
```
polaire/
├── src/Polaire/           # Main library
│   ├── Core/              # ChunkedArray, IChunkedArray
│   ├── DataTypes/         # 29 data types, AnyValue
│   ├── Series/            # Series class
│   ├── DataFrame/         # DataFrame, GroupBy, JoinOperations
│   ├── Compute/           # SIMD operations, aggregations
│   ├── Expressions/       # Expr tree, ExprEvaluator
│   ├── LazyFrame/         # LazyFrame, LogicalPlan, QueryOptimizer
│   ├── IO/                # CSV, Parquet, JSON readers/writers
│   └── Memory/            # MemoryPool, ValidityBitmap
├── tests/Polaire.Tests/   # xUnit tests
└── benchmarks/            # BenchmarkDotNet benchmarks
```

### Key Design Decisions

1. **Namespace:** `Series` and `DataFrame` are in root `Polaire` namespace to avoid collisions.

2. **Type Aliases:** `Pl` class uses aliases (`SeriesType`, `DataFrameType`) since methods shadow type names.

3. **GroupBy Tuples:** `GroupBy.Agg` uses 4-element tuples: `(column, aggName, func, alias?)`.

4. **SIMD:** Uses architecture-specific intrinsics (`AdvSimd.Arm64` / `Avx`) with 4 accumulators for ILP.

## Critical API Patterns (Gotchas)

These patterns differ from what you might expect:

```csharp
// 1. Expression Arithmetic - use operators, not methods
Col("a") + 10              // Correct
Col("a").Add(10)           // WRONG - doesn't exist

// 2. Sort with Descending - tuple syntax
.Sort((Col("total"), true))   // Correct (descending)
.Sort("col", descending: true) // WRONG for LazyFrame

// 3. GroupBy.Agg - tuple syntax, NOT Expr
df.GroupBy("id").Agg(("value", "sum", s => s.Sum(), "total"))  // Correct
df.GroupBy("id").Agg(Col("value").Sum().As("total"))           // WRONG

// 4. Null methods
Col("a").IsNotNullExpr()   // Correct (method)
Col("a").FillNullWith(0)   // Correct
Col("a").IsNotNull()       // WRONG - this is a property

// 5. Concat - use Pl.Concat
Pl.Concat(df1.Lazy(), df2.Lazy())  // Correct

// 6. Byte access
value.AsUInt8()            // Correct
value.AsByte()             // WRONG

// 7. Row access - no direct access
df["col_name"][rowIndex]   // Correct
df.Row(rowIndex)           // WRONG - doesn't exist

// 8. Arithmetic results - returns Float64
(Col("a") * 2)             // Result is Float64, not Int32

// 9. DataFrame.Rename - dictionary syntax
df.Rename(new Dictionary<string, string> { { "old", "new" } })  // Correct
df.Rename("old", "new")    // WRONG

// 10. Series.NullCount - property, not method
series.NullCount           // Correct
series.NullCount()         // WRONG

// 11. Window functions - use Over() with Expr methods
Col("value").Sum().Over(Col("group"))           // Sum per group
Col("value").RankExpr().Over(Col("group"))      // Rank within group
Col("value").CumSumExpr().Over(Col("group"))    // Cumulative sum per group
Col("value").ShiftExpr(1).Over(Col("group"))    // Shift within group
Col("value").Sum().Over()                       // Entire frame as partition

// 12. Rolling/Expanding/EWM expressions - use LazyFrame.WithColumns
df.Lazy().WithColumns(Col("value").RollingSumExpr(3).As("rolling"))    // Rolling sum
df.Lazy().WithColumns(Col("value").RollingMeanExpr(3, center: true))   // Centered mean
df.Lazy().WithColumns(Col("value").ExpandingSumExpr())                 // Expanding sum
df.Lazy().WithColumns(Col("value").EwmMeanExpr(0.5).As("ema"))         // EWM mean
df.Lazy().WithColumns(Col("value").RollingSumExpr(2).Over(Col("g")))   // Per-group rolling

// 13. Expr slice is SliceExpr (Expr.Slice is a nested record type)
Col("a").SliceExpr(-2)          // Last two values; Polars offset/length clipping

// 14. Format strings: '%' means Polars/chrono specifiers, otherwise .NET format
s.Str.ToDate("%Y-%m-%d")        s.Str.ToDatetime("%F %T%.f")    s.Dt.Strftime("%d %b %Y")
s.Dt.Strftime("yyyy-MM-dd")     // .NET custom format still works
s.Dt.OffsetBy("1mo")            // Polars duration string; keeps Date/DateTime type and unit

// 15. Row-wise functions take Exprs or column names
Pl.SumHorizontal("a", "b")      // Also MeanHorizontal, MinHorizontal, MaxHorizontal
```

## Temporal Storage (read before touching Date/DateTime code)

- `Date` columns hold Arrow Date32: **days since 1970-01-01** (`ChunkedArray<int>`).
- `DateTime` columns hold Arrow Timestamp: **units since the Unix epoch** in the column's `TimeUnit` (`ChunkedArray<long>`, default nanoseconds).
- `AnyValue` uses .NET representations instead (`DateOnly.DayNumber`, `DateTime.Ticks`).
- Convert only through `Core/TemporalConversions` (`DateFromDays`, `DateTimeFromRaw`, `DateSeries`, `DateTimeSeries`). Calling `DateOnly.FromDayNumber(raw)` or `new DateTime(raw)` on stored values gives wrong dates.
- .NET `DateTime` values are stored as wall-clock time; `Kind` is ignored on input and `Utc` on output.

## Not Yet Implemented

- As-of join, pivot, transpose, `group_by_dynamic`
- `Time` and `Duration` columns (the types exist, but series construction and operations don't)
- Parquet writer: Date/DateTime columns are written as strings; Int8/Int16/UInt columns throw `InvalidCastException`
- Concatenating List/Struct columns (`Series.Concat` throws `NotSupportedException`)
- Streaming execution, IPC/Arrow files, GPU acceleration

## Critical Bug Fixes

### Temporal Epoch Handling (Session 16)
**Problem:** Reads treated Date32 days as `DateOnly.DayNumber` (days since year 1) and Timestamp nanoseconds as .NET ticks, so 2024-01-01 read as 0055-01-01 and `Dt.Year()` on a 2024 timestamp returned 5446. `series[i]` returned null for every DateTime value, and `Take` converted Date/DateTime (and Int8/Int16/UInt) columns to strings, so `Filter`, `Sort`, `Unique` and `Concat` changed column types. The old tests checked only result types and lengths.

**Fix:** All conversions go through `TemporalConversions`; `Take`/`Concat` preserve the physical type. `TemporalCorrectnessTests` checks values.

### JSON Reader Type Inference (Session 16)
`JsonReader` inferred every numeric column as Boolean (all null) because the number case never cleared `allBool`. `NdjsonReader` was correct.

### SIMD NaN Handling (Session 9)
**Problem:** SIMD Min/Max propagated NaN (IEEE 754), but Polars semantics skip NaN.
- Array `[NaN, 1, 2, 3, 4, 5, 6, 7]` (8 elements, SIMD path) → returned NaN (wrong)
- Array `[NaN, 1, 2]` (3 elements, scalar path) → returned 1 (correct)

**Fix:** In `MinFloat64`, `MaxFloat64`, etc.: if SIMD returns NaN or initial infinity, fall through to scalar path.

```csharp
min = MinVectorized(span);
if (!double.IsNaN(min) && !double.IsPositiveInfinity(min))
    return AnyValue.From(min);
// Fall through to scalar path for proper NaN handling
```

### Parallel Aggregation Limitation (Session 8)
**Problem:** `ReadOnlySpan<T>` can't be captured in lambdas (ref struct).

**Workaround:** Copy to array with `unsafe` fixed pointers, but copy overhead negates benefits for <10M elements.

**Current:** Parallel threshold set to 10M elements to avoid regression.

## Performance Optimization Summary

### SIMD Pattern (Session 7)
Key techniques that achieved Polars parity:
1. **Direct memory access:** `MemoryMarshal.GetReference` + `Vector128.LoadUnsafe`
2. **4 accumulators:** Saturates CPU throughput via instruction-level parallelism
3. **Hardware reductions:** `AddPairwiseScalar`, `MinPairwiseScalar`, `MaxPairwiseScalar`

### Float32 vs Float64 Difference
```csharp
// Float64 - uses Arm64 namespace
vSum0 = AdvSimd.Arm64.Add(vSum0, v0);
sum = AdvSimd.Arm64.AddPairwiseScalar(vSum0).ToScalar();

// Float32 - uses base AdvSimd (NOT Arm64!)
vSum0 = AdvSimd.Add(vSum0, v0);
var p1 = AdvSimd.Arm64.AddPairwise(vSum0, vSum0);
var p2 = AdvSimd.Arm64.AddPairwise(p1, p1);
sum = p2.GetElement(0);
```

### Optimization Status
| Optimization | Status | Notes |
|-------------|--------|-------|
| Architecture intrinsics | Done | 3.5x improvement |
| Float32 intrinsics | Done | |
| SIMD Binary Ops | Done | ~6% (memory-bound) |
| Parallel Aggregations | Done | Only >10M elements |
| SIMD Filter | TODO | Good potential |
| Integer intrinsics | TODO | |

## Benchmark Results (Apple M1 Max)

### Polars Comparison (N=1,000,000)
| Operation | Polaire | Polars | Status |
|-----------|---------|--------|--------|
| Sum | 128 µs | 101 µs | 1.3x |
| Mean | 127 µs | 104 µs | 1.2x |
| Min | 107 µs | 101 µs | ~Parity |
| Max | 104 µs | 101 µs | ~Parity |
| Std | 269 µs | 624 µs | **2.3x faster** |

## Dependencies

- `Apache.Arrow` 18.0.0
- `SqlParser` 1.0.2
- `Parquet.Net` 5.0.2
- `CsvHelper` 33.0.1
- `System.Text.Json` 8.0.5

## Session History Summary

| Session | Tests | Key Changes |
|---------|-------|-------------|
| 1-4 | 163 | Initial implementation, I/O, benchmarks |
| 5-8 | 255 | SIMD optimizations (85x→3.5x improvements), Polars parity |
| 9 | 255 | SIMD NaN bug fix, edge case tests |
| 10 | 1194 | Major test expansion, API pattern documentation |
| 11 | 2242 | Test cleanup, removed tests for unimplemented APIs |
| 12 | 2264 | Window functions (over clause) implementation |
| 13 | 2282 | Rolling/Expanding/EWM expression methods |
| 14 | 2293 | Rank/Interpolate expression methods (RowNumber, OrdinalRank, FillForward, etc.) |
| 15 | 2335 | SQL interface (SqlContext, DataFrame.Sql) |
| 16 | 2527 | Date/DateTime epoch fix; JSON/NDJSON writers; Strptime/Strftime (chrono formats), OffsetBy; horizontal functions; SliceExpr; Series.Concat/Append/NUnique; VStack/HStack; LazyFrame.Limit |
