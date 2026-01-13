# Polaire - Development Notes

## Project Overview

Polaire is a ground-up C#/.NET implementation inspired by [Polars](https://pola.rs), the high-performance DataFrame library. The goal is **complete feature parity** and **reasonable performance parity** with the Python/Rust Polars library.

**Motivation:** "I love C# and I don't want to write Python, but Polars is clearly superior in design compared to anything in .NET."

## Current State (January 2025)

- **Build:** Passing
- **Tests:** 163 passing, 0 failing
- **Target:** .NET 8.0
- **I/O:** CSV, Parquet, JSON/NDJSON (read/write complete)
- **Lazy Scanning:** Implemented with predicate/projection pushdown

### To Build & Test
```bash
dotnet build
dotnet test
```

### To Run Benchmarks
```bash
dotnet run -c Release --project benchmarks/Polaire.Benchmarks/
```

## Architecture

### Project Structure
```
polaire/
├── src/Polaire/           # Main library
│   ├── Core/              # ChunkedArray, IChunkedArray
│   ├── DataTypes/         # 29 data types, AnyValue
│   ├── Series/            # Series class (in Polaire namespace)
│   ├── DataFrame/         # DataFrame, GroupBy, JoinOperations
│   ├── Compute/           # SIMD operations, aggregations
│   ├── Expressions/       # Expr tree, ExprEvaluator
│   ├── LazyFrame/         # LazyFrame, LogicalPlan, QueryOptimizer, PlanExecutor
│   ├── IO/                # CSV, Parquet, JSON readers/writers
│   ├── Memory/            # MemoryPool, ValidityBitmap
│   └── Polaire.cs         # Main API entry point (Pl class)
├── tests/Polaire.Tests/   # xUnit tests
└── benchmarks/            # BenchmarkDotNet benchmarks
```

### Key Design Decisions

1. **Namespace Structure:** `Series` and `DataFrame` classes are in the root `Polaire` namespace (not sub-namespaces) to avoid type/namespace collisions.

2. **Type Aliases in Polaire.cs:** The `Pl` static class has methods named `Series()` and `DataFrame()` that shadow the type names. We use `using` aliases (`SeriesType`, `DataFrameType`, `LazyFrameType`) to disambiguate.

3. **Apache Arrow Foundation:** Uses `Apache.Arrow` package for columnar memory format. ChunkedArray<T> wraps Arrow arrays.

4. **SIMD Optimization:** Uses `System.Numerics.Vector<T>` for vectorized arithmetic and aggregations. Custom `VectorBinaryOp` delegate because `Span<T>` can't be used as generic type parameter in `Action<>`.

5. **AnyValue Union Type:** Type-erased value holder using `StructLayout.Explicit` for union-style storage. Avoids boxing for heterogeneous operations.

6. **Lazy Evaluation:** `LazyFrame` builds a `LogicalPlan` tree. `QueryOptimizer` applies passes (predicate pushdown, projection pushdown, constant folding). `PlanExecutor` executes the optimized plan.

7. **GroupBy Aggregation Tuples:** The `GroupBy.Agg` method uses 4-element tuples: `(column, aggName, func, alias?)` where `aggName` is used for type inference and `alias` for result column naming.

## Known Issues / TODOs

### Not Yet Implemented
- SQL interface (SqlParser dependency ready)
- Streaming/chunked processing for very large files
- GPU acceleration
- Window functions
- More string operations

### Technical Debt
- `MemoryPool.Memory<T>` property creates a copy (performance impact)
- `VectorScalarOp` uses scalar fallback (SIMD optimization removed due to delegate comparison issue)
- XML documentation incomplete (CS1591 warnings suppressed)
- Std/Var aggregations not yet SIMD optimized

## Dependencies

- `Apache.Arrow` 18.0.0 - Columnar data format
- `SqlParser` 1.0.2 - SQL parsing (for future SQL interface)
- `Parquet.Net` 5.0.2 - Parquet file support
- `CsvHelper` 33.0.1 - CSV parsing
- `System.Text.Json` 8.0.5 - JSON support

## Session History

### Session 1 (Initial Implementation)
- Created complete project structure (~11,000 lines)
- Implemented 29 data types, Series, DataFrame
- Built expression system with lazy evaluation
- Added query optimizer with multiple passes
- Created 127 tests

### Session 2 (Build Fixes - January 2025)
- Fixed SqlParser version (0.5.0 → 1.0.2)
- Resolved namespace collisions (Series/DataFrame moved to root namespace)
- Fixed C# `when` keyword collision in pattern matching
- Fixed `Span<T>` generic constraint issue with custom delegate
- Fixed `AnyValue.InvalidCast` error message bug
- Fixed GroupBy aggregation type inference for aliased expressions
- All 127 tests now passing
- Added README.md and LICENSE

### Session 3 (I/O Implementation - January 2025)
- Implemented full I/O module:
  - `CsvReader` / `CsvWriter` with parallel parsing
  - `ParquetReader` / `ParquetWriter`
  - `JsonReader` / `NdjsonReader`
- Added lazy scanning (`ScanCsv`, `ScanParquet`, `ScanNdjson`)
- Predicate pushdown to file scans
- Projection pushdown to file scans
- Added 36 new I/O tests (148 total)

### Session 4 (Benchmarks & Fixes - January 2025)
- Ran full benchmark suite (70 benchmarks)
- Fixed `Series.Slice()` to support all 14 data types (was only 3)
- Added `Slice()` method to `StringChunkedArray`
- Fixed Head and LazyChainedOperations benchmarks
- All 163 tests passing

### Session 5 (SIMD Optimization - January 2025)
- Added SIMD optimization for Min/Max aggregations
- Created `MinVectorized` / `MaxVectorized` helpers using `Vector.Min()` / `Vector.Max()`
- Performance improvement: **85x faster** for Min/Max (27.7ms → 324µs on 1M rows)
- Min/Max now faster than Sum/Mean (simpler reduction, no accumulation)
- Added `.claude/settings.local.json` to .gitignore
- Removed it from git tracking (was accidentally committed)

**Key SIMD pattern** (see `SeriesAggregations.cs`):
```csharp
if (!series.HasNulls && data.ChunkCount == 1)
{
    var span = data.GetChunkSpan(0);
    result = MinVectorized(span);  // Uses Vector.Min()
}
```

## Design Objectives (from original requirements)

1. **Feature Parity** - Match Polars functionality
2. **Port Tests** - Demonstrate correctness
3. **Profile & Optimize** - Performance parity with Polars

## Useful Commands

```bash
# Quick test run
dotnet test --no-build

# Run specific test
dotnet test --filter "TestName"

# Build in release mode
dotnet build -c Release

# Check for compiler warnings (usually hidden)
dotnet build -warnaserror-

# Run all benchmarks (takes ~10 minutes)
dotnet run -c Release --project benchmarks/Polaire.Benchmarks/

# Run specific benchmarks with shorter duration (~2-5 min)
dotnet run -c Release --project benchmarks/Polaire.Benchmarks/ -- --filter "*Aggregation*" --job short
```

## API Quick Reference

```csharp
using Polaire;
using static Polaire.Pl;

// Create data
var series = Series("name", new[] { 1, 2, 3 });
var df = DataFrame(series1, series2);

// Expressions
Col("x") > 10
Col("x").Sum().As("total")

// Lazy operations
df.Lazy().Filter(...).Select(...).Collect()

// GroupBy
df.GroupBy("key").Sum()
df.GroupBy("key").Agg(Col("value").Mean().As("avg"))

// I/O - Eager
var df = ReadCsv("data.csv");
var df = ReadParquet("data.parquet");
df.WriteCsv("output.csv");
df.WriteParquet("output.parquet");

// I/O - Lazy (enables optimization)
var result = ScanCsv("large.csv")
    .Filter(Col("status").Eq("active"))  // Pushdown to reader
    .Select("id", "name")                 // Only these columns read
    .Collect();
```

## Benchmark Results (Apple M1 Max, .NET 8.0)

### SIMD-Optimized Aggregations
| Operation | 1K | 10K | 100K | 1M |
|-----------|-----|------|------|------|
| Sum | 511 ns | 4.8 µs | 48 µs | 482 µs |
| Mean | 515 ns | 4.8 µs | 48 µs | 483 µs |
| Min | 500 ns | 3.2 µs | 32 µs | 324 µs |
| Max | 500 ns | 3.2 µs | 32 µs | 322 µs |
| Addition | 6 µs | 56 µs | 691 µs | 6.7 ms |

### DataFrame Operations
| Operation | 1K | 10K | 100K |
|-----------|------|-------|--------|
| Select | 157 ns | 156 ns | 158 ns |
| Filter | 287 µs | 2.9 ms | 28.4 ms |
| Sort | 1.15 ms | 14.3 ms | 190 ms |
| GroupBySum | 375 µs | 2.5 ms | 25.4 ms |
| Join | 318 µs | 5.4 ms | - |

### Known Performance Issues
- Std/Var: Not yet SIMD optimized (~38ms for 1M rows)

## Next Steps / Roadmap

1. **Performance Comparison with Polars** - Run equivalent benchmarks
2. **SIMD for Std/Var** - Implement vectorized standard deviation/variance
3. **SQL Interface** - Use SqlParser to execute SQL queries
4. **Window Functions** - Rolling aggregations, rank, etc.
5. **More String Operations** - Regex, split, extract, etc.
6. **Streaming I/O** - Process files larger than memory
