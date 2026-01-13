# Polaire - Development Notes

## Project Overview

Polaire is a ground-up C#/.NET implementation inspired by [Polars](https://pola.rs), the high-performance DataFrame library. The goal is **complete feature parity** and **reasonable performance parity** with the Python/Rust Polars library.

**Motivation:** "I love C# and I don't want to write Python, but Polars is clearly superior in design compared to anything in .NET."

## Current State (January 2025)

- **Build:** Passing
- **Tests:** 127 passing, 0 failing
- **Target:** .NET 8.0

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
│   ├── LazyFrame/         # LazyFrame, LogicalPlan, QueryOptimizer
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
- CSV/Parquet/JSON I/O (methods exist but throw NotImplementedException)
- SQL interface
- Streaming/chunked processing
- GPU acceleration

### Technical Debt
- `MemoryPool.Memory<T>` property creates a copy (performance impact)
- `VectorScalarOp` uses scalar fallback (SIMD optimization removed due to delegate comparison issue)
- XML documentation incomplete (CS1591 warnings suppressed)

## Dependencies

- `Apache.Arrow` 18.0.0 - Columnar data format
- `SqlParser` 1.0.2 - SQL parsing (for future SQL interface)
- `Parquet.Net` 5.0.2 - Parquet file support (not yet implemented)
- `CsvHelper` 33.0.1 - CSV parsing (not yet implemented)
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
```
