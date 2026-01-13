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
- Float32 aggregations not yet SIMD optimized (only Float64, Int32, Int64)

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

### Session 6 (SIMD Var/Std + Polars Comparison - January 2025)
- Added SIMD optimization for Var/Std aggregations (two-pass algorithm)
- Created `SumSquaredDiffVectorized` helper for variance calculation
- Performance improvement: **39x faster** for Std (37ms → 955µs on 1M rows)
- Created `benchmarks/polars_comparison.py` for direct Polars comparison
- **Polars Comparison Results (N=1,000,000):**

| Operation | Polaire (C#) | Polars (Rust) | Gap |
|-----------|--------------|---------------|-----|
| Sum | 478 µs | 113 µs | 4.2x |
| Mean | 480 µs | 127 µs | 3.8x |
| Min | 321 µs | 112 µs | 2.9x |
| Max | 322 µs | 111 µs | 2.9x |
| Std | 955 µs | 648 µs | 1.5x |

**Analysis:** A 1.5-4x gap between managed C# and highly-optimized Rust is reasonable:
- Polars uses architecture-specific AVX2/AVX512 intrinsics
- Polaire uses `System.Numerics.Vector<T>` (portable but not optimal)
- Before SIMD: Std had a 58x gap, now just 1.5x

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
| Sum | 510 ns | 4.8 µs | 48 µs | 478 µs |
| Mean | 524 ns | 4.8 µs | 48 µs | 480 µs |
| Min | 349 ns | 3.2 µs | 32 µs | 321 µs |
| Max | 353 ns | 3.2 µs | 32 µs | 322 µs |
| Std | 1.0 µs | 9.6 µs | 95 µs | 955 µs |
| Addition | 6 µs | 56 µs | 691 µs | 6.7 ms |

### DataFrame Operations
| Operation | 1K | 10K | 100K |
|-----------|------|-------|--------|
| Select | 157 ns | 156 ns | 158 ns |
| Filter | 287 µs | 2.9 ms | 28.4 ms |
| Sort | 1.15 ms | 14.3 ms | 190 ms |
| GroupBySum | 375 µs | 2.5 ms | 25.4 ms |
| Join | 318 µs | 5.4 ms | - |

### Polars Comparison (N=1,000,000)
| Operation | Polaire | Polars | Gap |
|-----------|---------|--------|-----|
| Min/Max | 321 µs | 111 µs | 2.9x |
| Sum/Mean | 479 µs | 120 µs | 4.0x |
| Std | 955 µs | 648 µs | 1.5x |

Run comparison: `source .venv/bin/activate && python benchmarks/polars_comparison.py`

## Next Steps / Roadmap

1. ~~**Performance Comparison with Polars**~~ ✓ Done (1.5-4x gap)
2. ~~**SIMD for Std/Var**~~ ✓ Done (39x improvement)
3. **SQL Interface** - Use SqlParser to execute SQL queries
4. **Window Functions** - Rolling aggregations, rank, etc.
5. **More String Operations** - Regex, split, extract, etc.
6. **Streaming I/O** - Process files larger than memory

## Performance Optimization Opportunities

### Quick Wins (Same Patterns)
These follow the existing SIMD pattern in `SeriesAggregations.cs`:

1. **Float32 SIMD** (~15 min)
   - Add `SumFloat32`, `MinFloat32`, `MaxFloat32`, `VarFloat32` with SIMD paths
   - Same pattern as Float64, just change types
   - Currently falls back to scalar loop

2. **Other Integer Types** (~30 min)
   - Int8, Int16, UInt8, UInt16, UInt32, UInt64 not yet SIMD optimized
   - May need widening (Int8 → Int32) to avoid overflow in Sum

### Medium Effort, High Impact

3. **Architecture-Specific Intrinsics** (Biggest potential gain)
   - Replace `System.Numerics.Vector<T>` with `System.Runtime.Intrinsics`
   - Use `Avx2`, `Avx512`, `AdvSimd` (ARM NEON) directly
   - Could close the 2-4x gap with Polars significantly
   - Example for ARM NEON Sum:
   ```csharp
   using System.Runtime.Intrinsics;
   using System.Runtime.Intrinsics.Arm;

   if (AdvSimd.IsSupported)
   {
       var vSum = Vector128<double>.Zero;
       for (int i = 0; i < vectorCount; i += 2)
       {
           var v = AdvSimd.LoadVector128(ptr + i);
           vSum = AdvSimd.Add(vSum, v);
       }
   }
   ```
   - Need separate paths for x64 (AVX2/AVX512) and ARM (NEON)
   - More code but maximum performance

4. **Parallel Aggregations** (~2 hours)
   - Split large arrays across threads, merge results
   - Use `Parallel.For` with thread-local accumulators
   - Threshold: only parallelize above ~100K elements
   - Watch for false sharing on cache lines

5. **SIMD Filter** (~2 hours)
   - Vectorized comparison: `Vector.GreaterThan()` returns mask
   - Use mask to selectively copy matching elements
   - Current Filter is scalar loop, could be 4-8x faster

### Larger Efforts

6. **SIMD Sort** (1-2 days)
   - Radix sort for integers (O(n) vs O(n log n))
   - Vectorized comparison networks for small arrays
   - Hybrid: SIMD for partitioning in quicksort

7. **Memory Prefetching** (~1 hour)
   - `Sse.Prefetch*()` hints for upcoming memory access
   - Useful when traversing large arrays sequentially
   - Marginal gains on modern CPUs with good prefetchers

8. **Cache-Blocking** (~2 hours)
   - Process data in L1/L2 cache-sized chunks
   - Reduces cache misses for multi-pass algorithms (like Var)
   - Typical block size: 32KB (L1) or 256KB (L2)

### Priority Order for Maximum Impact
1. Architecture-specific intrinsics (ARM NEON for M1) - could cut gap in half
2. Parallel aggregations - scales with core count
3. SIMD Filter - very common operation
4. Float32 SIMD - quick win for float32 data

### Notes on Polars Performance
Why Polars is faster:
- Hand-tuned AVX2/AVX512 assembly for hot paths
- Rust's zero-cost abstractions
- Years of micro-optimization
- SIMD string operations (we use scalar)
- Parallel by default for large operations
- Memory-mapped I/O with prefetching
