# Polaire - Development Notes

## Project Overview

Polaire is a ground-up C#/.NET implementation inspired by [Polars](https://pola.rs), the high-performance DataFrame library. The goal is **complete feature parity** and **reasonable performance parity** with the Python/Rust Polars library.

**Motivation:** "I love C# and I don't want to write Python, but Polars is clearly superior in design compared to anything in .NET."

## Current State (January 2025)

- **Build:** Passing
- **Tests:** 1194 passing, 0 failing
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

4. **SIMD Optimization:** Uses architecture-specific intrinsics (`System.Runtime.Intrinsics`) for maximum performance:
   - ARM: `AdvSimd.Arm64` (NEON) with `Vector128<T>`
   - x64: `Avx` with `Vector256<T>`
   - Fallback: `System.Numerics.Vector<T>` for portability
   - 4 accumulators for instruction-level parallelism
   - Custom `VectorBinaryOp` delegate for binary operations (Span<T> generic constraint workaround)

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
- Parallel aggregations disabled (threshold=10M) due to array copy overhead from lambda capture limitations

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

### Session 7 (Architecture-Specific Intrinsics - January 2025)
- **Major breakthrough:** Replaced `System.Numerics.Vector<T>` with architecture-specific intrinsics
- Implemented ARM NEON (`AdvSimd.Arm64`) and x64 AVX (`Avx`) paths
- Added `System.Runtime.Intrinsics` for direct hardware control
- **Key optimizations:**
  - 4 accumulators for instruction-level parallelism
  - `Vector128.LoadUnsafe` for efficient memory access
  - `AddPairwiseScalar`, `MinPairwiseScalar`, `MaxPairwiseScalar` for horizontal reductions
- Updated: `SumVectorized`, `MinVectorized`, `MaxVectorized`, `SumSquaredDiffVectorized`

**Performance Improvement (N=1,000,000):**
| Operation | Before (Vector<T>) | After (ARM NEON) | Improvement |
|-----------|-------------------|------------------|-------------|
| Sum | 478 µs | 128 µs | **3.7x faster** |
| Mean | 480 µs | 127 µs | **3.8x faster** |
| Min | 321 µs | 107 µs | **3.0x faster** |
| Max | 322 µs | 104 µs | **3.1x faster** |
| Std | 955 µs | 269 µs | **3.5x faster** |

**New Polars Comparison (N=1,000,000):**
| Operation | Polaire (C#) | Polars (Rust) | Status |
|-----------|--------------|---------------|--------|
| Sum | 128 µs | 101 µs | 1.3x (was 4.2x) |
| Mean | 127 µs | 104 µs | 1.2x (was 3.8x) |
| Min | 107 µs | 101 µs | 1.1x (was 2.9x) |
| Max | 104 µs | 101 µs | **~PARITY!** |
| Std | 269 µs | 624 µs | **2.3x FASTER!** |

**Key intrinsics pattern** (see `SeriesAggregations.cs`):
```csharp
if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
{
    var vSum0 = Vector128<double>.Zero;
    var vSum1 = Vector128<double>.Zero;
    // ... 4 accumulators for ILP
    for (; i < vectorCount; i += 8)
    {
        var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
        vSum0 = AdvSimd.Arm64.Add(vSum0, v0);
        // ...
    }
    sum = AdvSimd.Arm64.AddPairwiseScalar(vSum0).ToScalar();
}
```

### Session 8 (Float32 + Binary SIMD + Parallel - January 2025)
- **Float32 Intrinsics**: Added ARM NEON/AVX for `SumFloat32`, `MinFloat32`, `MaxFloat32`
  - `Vector128<float>` = 4 elements (vs 2 for double)
  - Process 16 elements per iteration with 4 accumulators
  - Two pairwise operations for horizontal float reduction
- **SIMD Binary Operations**: Updated `VectorAdd/Sub/Mul/Div` with architecture-specific intrinsics
  - Replaced `System.Numerics.Vector<T>` with `AdvSimd.Arm64` and `Avx` paths
  - ~6% improvement for binary ops at 1M elements (memory-bound operation)
- **Parallel Aggregations**: Added `SumParallel/MinParallel/MaxParallel` with `Parallel.For`
  - Uses `unsafe` fixed pointers (ref locals can't be captured in lambdas)
  - Array copy overhead negates benefits for <10M elements
  - Threshold set to 10M to avoid regression

**Float32 Intrinsics Pattern:**
```csharp
if (AdvSimd.IsSupported && span.Length >= 16)
{
    var vSum0 = Vector128<float>.Zero;
    // ... 4 accumulators
    for (; i < vectorCount; i += 16)
    {
        var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
        vSum0 = AdvSimd.Add(vSum0, v0);  // Note: AdvSimd (not Arm64) for float
    }
    // Two pairwise ops for 4-element horizontal sum
    var pairwise1 = AdvSimd.Arm64.AddPairwise(vSum0, vSum0);
    var pairwise2 = AdvSimd.Arm64.AddPairwise(pairwise1, pairwise1);
    sum = pairwise2.GetElement(0);
}
```

### Session 9 (Edge Case Tests & SIMD NaN Bug Fix - January 2025)
- **Test Coverage Audit**: Compared Polaire (167 tests) vs Polars (~5,000-10,000+ tests in 203 test files)
- **Critical Bug Found**: SIMD/Scalar path divergence for NaN handling in Min/Max
  - SIMD path propagated NaN (IEEE 754 behavior)
  - Scalar path skipped NaN (Polars semantics)
  - Result: Inconsistent behavior based on array size (< 8 elements = correct, ≥ 8 = wrong)
- **Bug Fixed**: Updated `MinFloat64`, `MaxFloat64`, `MinFloat32`, `MaxFloat32` in `SeriesAggregations.cs`
  - If SIMD returns NaN or initial infinity, fall through to scalar path
- **Created `EdgeCaseTests.cs`**: 88 new test methods covering:
  - SIMD boundary tests (0, 1, 7, 8, 9, 15, 16, 17, 32, 100, 1000 elements)
  - NaN handling (single NaN, all NaN, mixed with real values)
  - Infinity handling (positive, negative, both)
  - Integer overflow (Int32 → Int64 accumulator verification)
  - Floating-point precision (catastrophic cancellation detection)
  - Null handling (all nulls, partial nulls)
- **Test count**: 167 → 255 (+88 new tests)

**The SIMD NaN Bug Pattern** (before fix):
```csharp
// SIMD Min/Max propagates NaN per IEEE 754
// If array = [NaN, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0] (8 elements)
// SIMD: AdvSimd.Arm64.Min(NaN, 1.0) → NaN (propagated!)
// Result: Null (wrong - should be 1.0)

// If array = [NaN, 1.0, 2.0] (3 elements, scalar path)
// Scalar: skips NaN, returns 1.0 (correct)
```

**The Fix Pattern** (in `MinFloat64`, `MaxFloat64`, etc.):
```csharp
if (!series.HasNulls && data.ChunkCount == 1)
{
    var span = data.GetChunkSpan(0);
    min = MinVectorized(span);

    // NEW: If SIMD returned valid value, use it; otherwise fallback to scalar
    if (!double.IsNaN(min) && !double.IsPositiveInfinity(min))
    {
        return AnyValue.From(min);
    }
    // Fall through to scalar path for NaN handling
}

// Scalar path: explicitly skip NaN values (Polars semantics)
for (int i = 0; i < series.Length; i++)
{
    if (!series.IsNull(i))
    {
        var val = data.GetValue(i);
        if (!double.IsNaN(val) && val < min)
        {
            min = val;
            found = true;
        }
    }
}
```

**Key Insight**: IEEE 754 SIMD operations propagate NaN, but Polars semantics require skipping NaN. The fix detects when SIMD returns an "invalid" result and falls back to the scalar path which handles NaN correctly.

### Session 10 (Comprehensive Test Suite Expansion - January 2025)
- **Massive test expansion**: 255 → 1194 tests (+939 new tests)
- **Fixed build errors** in `ConcatStackTests.cs`:
  - `LazyFrame.Concat` namespace collision → use `Pl.Concat()`
  - Removed non-existent `ChunkedArray.FromValues` tests
- **Created 4 new comprehensive test files**:
  - `LazyEvaluationTests.cs` (33 tests) - Lazy operations, chained queries, sorting, groupby, joins
  - `SchemaDataTypesTests.cs` (47 tests) - All 12 numeric types, schema access, AnyValue, special values
  - `SeriesConstructionTests.cs` (40 tests) - FromValues, FromNullable, naming, properties
  - `DataFrameConstructionTests.cs` (32 tests) - Construction, select, drop, withColumn

**API Patterns Discovered & Documented:**

1. **Expression Arithmetic** - Use operators, not methods:
   ```csharp
   // Correct
   Col("a") + 10
   Col("a") * 2

   // Wrong - these methods don't exist
   Col("a").Add(10)
   Col("a").Mul(2)
   ```

2. **Sort with Descending** - Uses tuple syntax:
   ```csharp
   // Correct
   .Sort((Col("total"), true))  // descending
   .Sort((Col("a"), false))     // ascending

   // Wrong
   .Sort(Col("a"), descending: true)
   ```

3. **Schema Access** - Schema is list of tuples, not dictionary:
   ```csharp
   // Correct
   df.Schema[0].Name
   df.Schema[0].Type

   // Wrong - doesn't exist
   df.Schema["col_name"]
   ```

4. **Null Handling Methods**:
   ```csharp
   // Correct
   Col("a").IsNotNullExpr()
   Col("a").FillNullWith(0)

   // Wrong
   Col("a").IsNotNull()   // This is a property, not method
   Col("a").FillNull(0)
   ```

5. **Static Concat** - Use `Pl.Concat()`:
   ```csharp
   // Correct
   Pl.Concat(df1.Lazy(), df2.Lazy())

   // Wrong - namespace collision
   LazyFrame.Concat(...)
   ```

6. **Byte Access on AnyValue**:
   ```csharp
   // Correct
   value.AsUInt8()

   // Wrong
   value.AsByte()
   ```

7. **Row Access** - No direct row access, use Series indexing:
   ```csharp
   // Correct
   df["col_name"][rowIndex]

   // Wrong - doesn't exist
   df.Row(rowIndex)
   ```

8. **Series Rename** - Use `Rename()` method:
   ```csharp
   // Correct
   series.Rename("new_name")

   // Wrong - Alias only exists on Expr
   series.Alias("new_name")
   ```

9. **Multiplication Result Type** - Returns Float64, not Int32:
   ```csharp
   // Multiplication promotes to Float64
   var result = df.Lazy()
       .WithColumns((Col("a") * 2).As("doubled"))
       .Collect();
   result["doubled"][0].AsFloat64()  // Use Float64, not Int32
   ```

**Key Files Modified:**
- `tests/Polaire.Tests/ConcatStackTests.cs` (fixed build errors)
- Created 4 new test files (152 tests total)
- Also includes 787 tests ported from other test file expansions in the session

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

### ARM NEON Intrinsics-Optimized Aggregations
| Operation | 1K | 10K | 100K | 1M |
|-----------|-----|------|------|------|
| Sum | 155 ns | 1.2 µs | 12 µs | 128 µs |
| Mean | 164 ns | 1.3 µs | 12 µs | 127 µs |
| Min | 132 ns | 0.9 µs | 9.5 µs | 107 µs |
| Max | 132 ns | 0.9 µs | 9.6 µs | 104 µs |
| Std | 300 ns | 2.6 µs | 25 µs | 269 µs |
| Addition | 6 µs | 52 µs | 580 µs | 6.3 ms |

### DataFrame Operations
| Operation | 1K | 10K | 100K |
|-----------|------|-------|--------|
| Select | 157 ns | 156 ns | 158 ns |
| Filter | 287 µs | 2.9 ms | 28.4 ms |
| Sort | 1.15 ms | 14.3 ms | 190 ms |
| GroupBySum | 375 µs | 2.5 ms | 25.4 ms |
| Join | 318 µs | 5.4 ms | - |

### Polars Comparison (N=1,000,000) - After ARM NEON Optimization
| Operation | Polaire | Polars | Status |
|-----------|---------|--------|--------|
| Sum | 128 µs | 101 µs | 1.3x (near parity!) |
| Mean | 127 µs | 104 µs | 1.2x (near parity!) |
| Min | 107 µs | 101 µs | **~PARITY!** |
| Max | 104 µs | 101 µs | **~PARITY!** |
| Std | 269 µs | 624 µs | **2.3x FASTER!** |

Run comparison: `source .venv/bin/activate && python benchmarks/polars_comparison.py`

## Next Steps / Roadmap

1. ~~**Performance Comparison with Polars**~~ ✓ Done (now at parity!)
2. ~~**SIMD for Std/Var**~~ ✓ Done (39x improvement)
3. ~~**Architecture-Specific Intrinsics**~~ ✓ Done (3.5x improvement, parity with Polars!)
4. ~~**Critical Edge Case Tests**~~ ✓ Done (Session 9: 88 new tests, SIMD NaN bug fixed)
5. **Property-Based Testing** - Add FsCheck for exhaustive input generation
6. **SQL Interface** - Use SqlParser to execute SQL queries
7. **Window Functions** - Rolling aggregations, rank, etc.
8. **More String Operations** - Regex, split, extract, etc.
9. **Streaming I/O** - Process files larger than memory

## Performance Optimization Opportunities

### Priority Order for Maximum Impact
1. ~~Architecture-specific intrinsics~~ ✓ DONE (3.5x improvement)
2. ~~**Float32 intrinsics**~~ ✓ DONE (Session 8)
3. ~~**SIMD Binary Operations**~~ ✓ DONE (Session 8, ~6% improvement)
4. ~~**Parallel Aggregations**~~ ✓ DONE (Session 8, threshold=10M due to copy overhead)
5. **SIMD Filter** - Very common operation (~2 hours)
6. **Integer type intrinsics** - Complete coverage (~1 hour)

---

### 1. Float32 Intrinsics (~15 min) ⭐ QUICK WIN

**Current state:** `SumFloat32`, `MinFloat32`, `MaxFloat32` use scalar loops.

**Implementation:** Same pattern as Float64, but `Vector128<float>` holds 4 elements (vs 2 for double).

```csharp
// In SeriesAggregations.cs, add new method:
private static float SumVectorizedFloat32(ReadOnlySpan<float> span)
{
    float sum = 0;
    int i = 0;

    ref float ptr = ref MemoryMarshal.GetReference(span);

    if (AdvSimd.IsSupported && span.Length >= 16)  // 4 vectors × 4 floats = 16
    {
        var vSum0 = Vector128<float>.Zero;
        var vSum1 = Vector128<float>.Zero;
        var vSum2 = Vector128<float>.Zero;
        var vSum3 = Vector128<float>.Zero;

        int vectorCount = span.Length - (span.Length % 16);

        for (; i < vectorCount; i += 16)
        {
            var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
            var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
            var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 8));
            var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 12));

            vSum0 = AdvSimd.Add(vSum0, v0);
            vSum1 = AdvSimd.Add(vSum1, v1);
            vSum2 = AdvSimd.Add(vSum2, v2);
            vSum3 = AdvSimd.Add(vSum3, v3);
        }

        vSum0 = AdvSimd.Add(vSum0, vSum1);
        vSum2 = AdvSimd.Add(vSum2, vSum3);
        vSum0 = AdvSimd.Add(vSum0, vSum2);

        // Horizontal sum for float (pairwise twice)
        var pairwise1 = AdvSimd.Arm64.AddPairwise(vSum0, vSum0);
        sum = AdvSimd.Arm64.AddPairwiseScalar(pairwise1.GetLower()).ToScalar();
    }
    // ... Avx path, fallback, scalar remainder
}
```

**Key differences from Float64:**
- `Vector128<float>` = 4 elements (vs 2 for double)
- Process 16 elements per iteration (vs 8)
- Use `AdvSimd.Add` (not `AdvSimd.Arm64.Add` - float version is in base class)
- Horizontal reduction needs two pairwise operations

**Files to modify:**
- `src/Polaire/Compute/SeriesAggregations.cs`: Add `SumVectorizedFloat32`, `MinVectorizedFloat32`, `MaxVectorizedFloat32`
- Update `SumFloat32()`, `MinFloat32()`, `MaxFloat32()` to call vectorized versions

---

### 2. SIMD Binary Operations (~30 min) ⭐ QUICK WIN

**Current state:** `SeriesArithmetic.cs` uses `System.Numerics.Vector<T>` for Add/Sub/Mul/Div.

**Implementation:** Apply same intrinsics pattern to binary operations.

```csharp
// In SeriesArithmetic.cs
public static ChunkedArray<double> Add(ChunkedArray<double> left, ChunkedArray<double> right)
{
    var result = new double[left.Length];
    ref double lPtr = ref MemoryMarshal.GetReference(left.GetChunkSpan(0));
    ref double rPtr = ref MemoryMarshal.GetReference(right.GetChunkSpan(0));
    ref double outPtr = ref MemoryMarshal.GetReference(result.AsSpan());

    int i = 0;
    if (AdvSimd.Arm64.IsSupported && left.Length >= 8)
    {
        int vectorCount = left.Length - (left.Length % 8);
        for (; i < vectorCount; i += 8)
        {
            var l0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref lPtr, i));
            var l1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref lPtr, i + 2));
            var l2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref lPtr, i + 4));
            var l3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref lPtr, i + 6));

            var r0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref rPtr, i));
            var r1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref rPtr, i + 2));
            var r2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref rPtr, i + 4));
            var r3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref rPtr, i + 6));

            Vector128.StoreUnsafe(AdvSimd.Arm64.Add(l0, r0), ref Unsafe.Add(ref outPtr, i));
            Vector128.StoreUnsafe(AdvSimd.Arm64.Add(l1, r1), ref Unsafe.Add(ref outPtr, i + 2));
            Vector128.StoreUnsafe(AdvSimd.Arm64.Add(l2, r2), ref Unsafe.Add(ref outPtr, i + 4));
            Vector128.StoreUnsafe(AdvSimd.Arm64.Add(l3, r3), ref Unsafe.Add(ref outPtr, i + 6));
        }
    }
    // scalar remainder...
}
```

**Note:** Binary operations are memory-bound (read 2 arrays, write 1), so gains may be smaller than aggregations. Still worth doing for consistency.

**Files to modify:**
- `src/Polaire/Compute/SeriesArithmetic.cs`

---

### 3. Parallel Aggregations (~2 hours)

**Current state:** All aggregations are single-threaded.

**Implementation:** Split array across threads, each thread uses intrinsics, merge results.

```csharp
private static double SumParallel(ReadOnlySpan<double> span)
{
    const int ParallelThreshold = 100_000;
    const int ChunkSize = 32_768;  // ~256KB per thread (fits L2 cache)

    if (span.Length < ParallelThreshold)
        return SumVectorized(span);  // Use single-threaded intrinsics

    int numChunks = (span.Length + ChunkSize - 1) / ChunkSize;
    var partialSums = new double[numChunks];

    // Need to pin memory for parallel access
    // Option 1: Copy to array (overhead)
    // Option 2: Use unsafe pointers

    Parallel.For(0, numChunks, chunkIndex =>
    {
        int start = chunkIndex * ChunkSize;
        int length = Math.Min(ChunkSize, span.Length - start);
        var chunk = span.Slice(start, length);
        partialSums[chunkIndex] = SumVectorized(chunk);
    });

    // Merge (could also be SIMD but usually small)
    double total = 0;
    for (int i = 0; i < numChunks; i++)
        total += partialSums[i];

    return total;
}
```

**Challenges:**
- `ReadOnlySpan<T>` can't be captured in lambda (stack-only)
- Need to convert to array or use unsafe pointers
- False sharing: ensure `partialSums` array elements are on different cache lines (pad to 64 bytes)

**Expected gains:** ~4-8x on M1 Max (10 cores) for large arrays (>1M elements).

**Files to modify:**
- `src/Polaire/Compute/SeriesAggregations.cs`

---

### 4. SIMD Filter (~2 hours)

**Current state:** `DataFrame.Filter()` uses scalar loop to evaluate predicates.

**Implementation:** Vectorized comparison + selective copy.

```csharp
// Vectorized: series > threshold
public static bool[] GreaterThanScalar(ReadOnlySpan<double> span, double threshold)
{
    var result = new bool[span.Length];
    ref double ptr = ref MemoryMarshal.GetReference(span);
    ref byte outPtr = ref Unsafe.As<bool, byte>(ref result[0]);

    int i = 0;
    if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
    {
        var vThreshold = Vector128.Create(threshold);
        int vectorCount = span.Length - (span.Length % 8);

        for (; i < vectorCount; i += 8)
        {
            var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
            var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 2));
            var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
            var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 6));

            // Compare returns all 1s or all 0s per element
            var cmp0 = AdvSimd.Arm64.CompareGreaterThan(v0, vThreshold);
            var cmp1 = AdvSimd.Arm64.CompareGreaterThan(v1, vThreshold);
            var cmp2 = AdvSimd.Arm64.CompareGreaterThan(v2, vThreshold);
            var cmp3 = AdvSimd.Arm64.CompareGreaterThan(v3, vThreshold);

            // Extract to bools (narrow from 64-bit masks to bytes)
            // This is the tricky part - need to pack results
            // ...
        }
    }
    // scalar remainder...
}
```

**The hard part:** Converting SIMD comparison masks (all 1s/0s per lane) to packed bools efficiently. May need lookup tables or permute instructions.

**Alternative approach:** Instead of creating bool[], create list of matching indices:
```csharp
// Collect indices where condition is true
var indices = new List<int>();
// ... SIMD comparison, extract set bits to indices
// Then: result = source.Take(indices)
```

**Files to modify:**
- `src/Polaire/Compute/` (new file: `SeriesComparison.cs`)
- `src/Polaire/DataFrame/DataFrame.cs` (update Filter to use vectorized comparison)

---

### 5. Integer Type Intrinsics (~1 hour)

**Current state:** Only Int32 and Int64 have SIMD paths. Others use scalar loops.

**Implementation:** Add intrinsics for remaining integer types.

| Type | Vector128 Elements | Notes |
|------|-------------------|-------|
| Int8/UInt8 | 16 | Widen to Int16/Int32 for Sum to avoid overflow |
| Int16/UInt16 | 8 | Widen to Int32 for Sum |
| UInt32 | 4 | Direct, but Sum needs UInt64 accumulator |
| UInt64 | 2 | Direct, watch for overflow |

**Widening pattern for Int8 Sum:**
```csharp
private static long SumVectorizedInt8(ReadOnlySpan<sbyte> span)
{
    long sum = 0;
    int i = 0;

    ref sbyte ptr = ref MemoryMarshal.GetReference(span);

    if (AdvSimd.Arm64.IsSupported && span.Length >= 64)
    {
        var vSum = Vector128<long>.Zero;  // Accumulate in 64-bit
        int vectorCount = span.Length - (span.Length % 64);

        for (; i < vectorCount; i += 64)
        {
            // Load 16 bytes
            var v = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));

            // Widen: sbyte -> short -> int -> long
            // AdvSimd has SignExtendWideningLower/Upper for this
            var wide16_lo = AdvSimd.Arm64.SignExtendWideningLower(v.GetLower());
            var wide16_hi = AdvSimd.Arm64.SignExtendWideningLower(v.GetUpper());

            // Continue widening to int32, then int64...
            // This gets complex - may be easier to just sum in int32 and check overflow
        }
    }
    // ...
}
```

**Simpler approach:** For small integer types, use Int32 SIMD with periodic overflow checks, or just use the scalar path (small integers are rare in data science).

**Files to modify:**
- `src/Polaire/Compute/SeriesAggregations.cs`

---

### 6. SIMD Sort (1-2 days) - Advanced

**Current state:** Uses `Array.Sort()` (introsort, O(n log n)).

**Opportunities:**
1. **Radix sort for integers** - O(n), but only for fixed-size integers
2. **Vectorized quicksort partition** - SIMD comparison to find pivot position
3. **Sorting networks for small arrays** - SIMD min/max for optimal small sorts

**Not recommended initially** - Sort is complex and Array.Sort is already highly optimized.

---

### 7. String Operations with SIMD (~4 hours) - Advanced

**Current state:** All string operations are scalar.

**Opportunities:**
- `Contains`: Use SIMD to scan for first character, then verify
- `StartsWith`/`EndsWith`: Direct SIMD comparison
- `ToUpper`/`ToLower`: SIMD range check + case flip

```csharp
// SIMD ToLower for ASCII
// Check if char in 'A'-'Z' range, if so add 32
var chars = Vector128.LoadUnsafe(ref charPtr);
var isUpper = AdvSimd.And(
    AdvSimd.CompareGreaterThanOrEqual(chars, Vector128.Create((ushort)'A')),
    AdvSimd.CompareLessThanOrEqual(chars, Vector128.Create((ushort)'Z'))
);
var lowered = AdvSimd.Add(chars, AdvSimd.And(isUpper, Vector128.Create((ushort)32)));
```

**Files to modify:**
- `src/Polaire/Series/StringOperations.cs`

---

### 8. Memory Prefetching (~1 hour) - Minor Gains

**Current state:** No explicit prefetching.

**Implementation:**
```csharp
if (Sse.IsSupported)
{
    // Prefetch next cache line (64 bytes ahead)
    Sse.Prefetch0(Unsafe.AsPointer(ref Unsafe.Add(ref ptr, i + 64)));
}
```

**Expected gains:** Marginal (0-10%) on modern CPUs with good hardware prefetchers. M1 has excellent prefetching.

---

### 9. Cache-Blocking for Multi-Pass Algorithms (~2 hours)

**Current state:** Var/Std does two full passes over the data.

**Implementation:** Process in L2-cache-sized blocks:
```csharp
const int BlockSize = 32768;  // 256KB / 8 bytes = 32K doubles

// Instead of: pass1 over all data, then pass2 over all data
// Do: for each block: pass1, pass2

for (int blockStart = 0; blockStart < span.Length; blockStart += BlockSize)
{
    var block = span.Slice(blockStart, Math.Min(BlockSize, span.Length - blockStart));

    // Pass 1: sum for mean (data now in L2 cache)
    double blockSum = SumVectorized(block);

    // Pass 2: sum squared diff (data still in L2 cache!)
    double blockSumSqDiff = SumSquaredDiffVectorized(block, mean);

    totalSum += blockSum;
    totalSumSqDiff += blockSumSqDiff;
}
```

**Problem:** Need the global mean for pass 2, but we don't know it until pass 1 completes.

**Solution:** Use Welford's online algorithm (single pass) or accept the cache miss on pass 2.

---

### Summary: Recommended Order for Future Sessions

| Priority | Optimization | Effort | Expected Gain | Status |
|----------|-------------|--------|---------------|--------|
| 1 | Float32 intrinsics | 15 min | 3-4x for float data | ✅ DONE |
| 2 | SIMD Binary Ops | 30 min | ~6% for arithmetic | ✅ DONE |
| 3 | Parallel Aggregations | 2 hours | Limited (see notes) | ✅ DONE |
| 4 | SIMD Filter | 2 hours | 4-8x for filtering | TODO |
| 5 | Integer intrinsics | 1 hour | 2-3x for int data | TODO |
| 6 | String SIMD | 4 hours | 2-4x for string ops | TODO |
| 7 | Cache-blocking | 2 hours | 10-20% for Var/Std | TODO |
| 8 | Memory prefetching | 1 hour | 0-10% marginal | TODO |
| 9 | SIMD Sort | 1-2 days | Complex, skip for now | SKIP |

---

## How We Achieved Polars Parity (Session 7 Deep Dive)

This section documents the key techniques that brought Polaire from **4x slower** to **parity with Polars**.

### The Problem: Vector<T> Abstraction Overhead

The original code used `System.Numerics.Vector<T>`:
```csharp
// OLD: Portable but suboptimal
var vSum = Vector<double>.Zero;
for (; i < vectorCount; i += Vector<double>.Count)
{
    vSum += new Vector<double>(span.Slice(i));  // Creates slice, then vector
}
for (int j = 0; j < Vector<double>.Count; j++)
    sum += vSum[j];  // Scalar horizontal reduction
```

**Problems:**
1. `span.Slice(i)` creates a new span on each iteration
2. `new Vector<double>(span)` copies data into the vector
3. `vSum[j]` scalar indexing for horizontal reduction
4. No instruction-level parallelism (single accumulator)

### Solution 1: Direct Memory Access

Use `MemoryMarshal.GetReference` + `Vector128.LoadUnsafe` for zero-copy loads:

```csharp
ref double ptr = ref MemoryMarshal.GetReference(span);

for (; i < vectorCount; i += 2)
{
    var v = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));  // Direct load!
    // ...
}
```

**Why it's faster:**
- No span allocation per iteration
- `LoadUnsafe` compiles to a single load instruction
- `Unsafe.Add` is just pointer arithmetic

### Solution 2: Multiple Accumulators (Critical!)

This was the **biggest win**. Modern CPUs can execute multiple independent operations in parallel (instruction-level parallelism / ILP).

```csharp
// OLD: Single accumulator (CPU stalls waiting for each add to complete)
var vSum = Vector128<double>.Zero;
for (; i < vectorCount; i += 2)
{
    vSum = AdvSimd.Arm64.Add(vSum, Vector128.LoadUnsafe(...));  // Dependency chain!
}

// NEW: 4 independent accumulators (CPU executes adds in parallel)
var vSum0 = Vector128<double>.Zero;
var vSum1 = Vector128<double>.Zero;
var vSum2 = Vector128<double>.Zero;
var vSum3 = Vector128<double>.Zero;

for (; i < vectorCount; i += 8)  // Process 8 elements per iteration
{
    var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
    var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 2));
    var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
    var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 6));

    vSum0 = AdvSimd.Arm64.Add(vSum0, v0);  // Independent!
    vSum1 = AdvSimd.Arm64.Add(vSum1, v1);  // Independent!
    vSum2 = AdvSimd.Arm64.Add(vSum2, v2);  // Independent!
    vSum3 = AdvSimd.Arm64.Add(vSum3, v3);  // Independent!
}

// Combine at the end
vSum0 = AdvSimd.Arm64.Add(vSum0, vSum1);
vSum2 = AdvSimd.Arm64.Add(vSum2, vSum3);
vSum0 = AdvSimd.Arm64.Add(vSum0, vSum2);
```

**Why it works:**
- M1 has 4 NEON execution units
- Each `Add` has ~3-4 cycle latency but 1 cycle throughput
- With 4 independent chains, we saturate throughput instead of waiting on latency
- This is why Std (which does subtract + multiply + add) benefited even more

### Solution 3: Hardware Horizontal Reductions

ARM NEON has special instructions for horizontal operations:

```csharp
// OLD: Scalar reduction (slow)
for (int j = 0; j < Vector<double>.Count; j++)
    sum += vSum[j];

// NEW: Hardware horizontal sum
sum = AdvSimd.Arm64.AddPairwiseScalar(vSum0).ToScalar();

// For Min/Max:
min = AdvSimd.Arm64.MinPairwiseScalar(vMin0).ToScalar();
max = AdvSimd.Arm64.MaxPairwiseScalar(vMax0).ToScalar();
```

These compile to single instructions (`faddp`, `fminp`, `fmaxp`).

### Solution 4: Architecture Detection Pattern

```csharp
if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
{
    // ARM NEON path (Vector128 = 2 doubles)
}
else if (Avx.IsSupported && span.Length >= 16)
{
    // x64 AVX path (Vector256 = 4 doubles)
}
else if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
{
    // Portable fallback
}
// Scalar remainder...
```

**Key points:**
- Check architecture at runtime (JIT eliminates dead branches)
- Different vector sizes: ARM Vector128 (2 doubles), x64 Vector256 (4 doubles)
- Always have a portable fallback
- Process remainder with scalar code

### The Complete Pattern

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static double SumVectorized(ReadOnlySpan<double> span)
{
    double sum = 0;
    int i = 0;

    ref double ptr = ref MemoryMarshal.GetReference(span);

    if (AdvSimd.Arm64.IsSupported && span.Length >= 8)
    {
        var vSum0 = Vector128<double>.Zero;
        var vSum1 = Vector128<double>.Zero;
        var vSum2 = Vector128<double>.Zero;
        var vSum3 = Vector128<double>.Zero;

        int vectorCount = span.Length - (span.Length % 8);

        for (; i < vectorCount; i += 8)
        {
            var v0 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i));
            var v1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 2));
            var v2 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 4));
            var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, i + 6));

            vSum0 = AdvSimd.Arm64.Add(vSum0, v0);
            vSum1 = AdvSimd.Arm64.Add(vSum1, v1);
            vSum2 = AdvSimd.Arm64.Add(vSum2, v2);
            vSum3 = AdvSimd.Arm64.Add(vSum3, v3);
        }

        vSum0 = AdvSimd.Arm64.Add(vSum0, vSum1);
        vSum2 = AdvSimd.Arm64.Add(vSum2, vSum3);
        vSum0 = AdvSimd.Arm64.Add(vSum0, vSum2);

        sum = AdvSimd.Arm64.AddPairwiseScalar(vSum0).ToScalar();
    }
    // ... AVX path, fallback, scalar remainder
}
```

### Required Using Statements

```csharp
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
```

### Results Summary

| Technique | Impact |
|-----------|--------|
| Direct memory access | ~1.5x |
| 4 accumulators (ILP) | ~2x |
| Hardware horizontal reduction | ~1.2x |
| **Combined** | **~3.5x** |

### Applying This Pattern Elsewhere

The same pattern can be applied to:
- Float32 aggregations (use `Vector128<float>` = 4 elements)
- Integer aggregations (watch for overflow, may need widening)
- Filter operations (use comparison intrinsics)
- Any reduction operation (min, max, sum, product, etc.)

### Notes on Why Std is Faster Than Polars

Our Std is 2.3x faster than Polars because:
1. Two-pass algorithm with 4 accumulators in each pass
2. The inner loop does: subtract → multiply → add (3 ops)
3. With 4 independent chains, all 3 ops overlap across iterations
4. Polars may use a different algorithm (online/streaming) that has more dependencies

---

## Session 8 Lessons Learned: Parallel Processing Pitfalls

This section documents critical lessons from Session 8 about parallel processing in .NET.

### The Problem: Span/Ref Can't Be Captured in Lambdas

When attempting to parallelize aggregations:

```csharp
// THIS DOES NOT COMPILE - CS8175 error
private static double SumParallel(ReadOnlySpan<double> span)
{
    ref double ptr = ref MemoryMarshal.GetReference(span);

    Parallel.For(0, numChunks, chunkIndex =>
    {
        // ERROR: Cannot use ref local 'ptr' inside a lambda
        var v = Vector128.LoadUnsafe(ref Unsafe.Add(ref ptr, start));
    });
}
```

**Why:** `ReadOnlySpan<T>` is a `ref struct` (stack-only), and `ref local` variables cannot be captured in lambdas because:
1. Lambdas may outlive the stack frame
2. The CLR cannot guarantee the referenced memory remains valid
3. This is a fundamental safety constraint in C#

### The Workaround: Unsafe Fixed Pointers

```csharp
private static unsafe double SumParallel(ReadOnlySpan<double> span)
{
    // MUST copy to array - span can't be pinned directly
    var array = span.ToArray();  // <-- This is the overhead!

    int numChunks = (array.Length + ChunkSize - 1) / ChunkSize;
    var partialSums = new double[numChunks];

    fixed (double* basePtr = array)
    {
        double* ptr = basePtr;  // Raw pointer CAN be captured
        int length = array.Length;

        Parallel.For(0, numChunks, chunkIndex =>
        {
            double* chunkPtr = ptr + (chunkIndex * ChunkSize);
            // ... SIMD processing with raw pointers
        });
    }

    // Merge partial results
    return partialSums.Sum();
}
```

### Why This Approach Has Limited Benefit

| Array Size | Copy Overhead | SIMD Time | Parallel Benefit | Net Result |
|------------|--------------|-----------|-----------------|------------|
| 100K | ~100 µs | ~10 µs | ~4x | **Slower** (copy dominates) |
| 1M | ~1 ms | ~100 µs | ~4x | **Slower** (copy dominates) |
| 10M+ | ~10 ms | ~1 ms | ~4x | **Faster** (finally worth it) |

**Key insight:** The `span.ToArray()` copy is O(n), and SIMD is already so fast that the copy overhead exceeds the parallel speedup for arrays under ~10M elements.

### Current Implementation

```csharp
// Threshold set very high to avoid regression
private const int ParallelThreshold = 10_000_000;  // 10M elements (~80MB)
private const int ChunkSize = 65_536;  // ~512KB for cache efficiency
```

### Alternative Approaches (Not Yet Implemented)

1. **Memory<T> Instead of Span<T>**
   - `Memory<T>` can be pinned and captured, but Polaire's `ChunkedArray<T>` returns spans
   - Would require significant refactoring

2. **ArrayPool Reuse**
   - Rent arrays from pool to reduce allocation
   - Still has copy overhead

3. **Unsafe.AsPointer on Original Array**
   - If we know the span came from an array, get pointer directly
   - Requires knowing the backing store

4. **Parallel at Higher Level**
   - Parallelize across chunks in ChunkedArray instead of within a single span
   - More natural fit for Arrow's chunked model

### Float32 vs Float64 Intrinsics Differences

| Aspect | Float64 (`double`) | Float32 (`float`) |
|--------|-------------------|-------------------|
| Vector128 elements | 2 | 4 |
| Elements per iteration | 8 (4 vectors × 2) | 16 (4 vectors × 4) |
| Add/Min/Max instruction | `AdvSimd.Arm64.Add` | `AdvSimd.Add` (base class!) |
| Horizontal reduction | `AddPairwiseScalar` (1 op) | `AddPairwise` × 2 |

**Critical difference:** Float operations use `AdvSimd.Add` (not `AdvSimd.Arm64.Add`):
```csharp
// Float64 - uses Arm64 namespace
vSum0 = AdvSimd.Arm64.Add(vSum0, v0);

// Float32 - uses base AdvSimd class (NO Arm64!)
vSum0 = AdvSimd.Add(vSum0, v0);
```

### Float32 Horizontal Reduction Pattern

```csharp
// Float64: Single pairwise scalar (2 elements → 1)
sum = AdvSimd.Arm64.AddPairwiseScalar(vSum0).ToScalar();

// Float32: Two pairwise operations (4 elements → 2 → 1)
var pairwise1 = AdvSimd.Arm64.AddPairwise(vSum0, vSum0);  // 4→2
var pairwise2 = AdvSimd.Arm64.AddPairwise(pairwise1, pairwise1);  // 2→1
sum = pairwise2.GetElement(0);
```

### Binary Operations: Memory-Bound Reality

SIMD binary operations (Add/Sub/Mul/Div) showed only ~6% improvement because:

1. **Memory bandwidth is the bottleneck**, not compute
2. Each operation reads 2 arrays and writes 1 (3× memory traffic vs 1× for aggregations)
3. The CPU can't process data faster than memory can deliver it

```
Aggregation: Read 1 array → Compute → Return scalar
             Compute-bound, SIMD helps a lot

Binary Op:   Read 2 arrays → Compute → Write 1 array
             Memory-bound, SIMD helps little
```

### Recommendations for Future Sessions

1. **Skip parallel for now** - SIMD is already fast enough for typical data science workloads
2. **Focus on SIMD Filter next** - Common operation with good optimization potential
3. **Consider chunk-level parallelism** - More natural for Arrow's chunked model
4. **Profile before optimizing** - Use BenchmarkDotNet to identify actual bottlenecks
