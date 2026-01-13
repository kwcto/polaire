# Polaire

**Blazingly fast DataFrames for .NET, inspired by [Polars](https://pola.rs)**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

---

> *"I love C# and I don't want to write Python."*

Polaire brings the power and elegance of Polars to the .NET ecosystem. No more compromises. No more context switching. Just pure, expressive, high-performance data manipulation in the language you love.

## Why Polaire?

The .NET ecosystem has lacked a truly modern DataFrame library. While Python developers enjoy Polars' lightning-fast performance and elegant API, C# developers have been left behind—until now.

Polaire is a ground-up implementation inspired by Polars, built specifically for .NET with:

- **Apache Arrow** columnar memory format for zero-copy interop
- **SIMD-optimized** operations using `System.Numerics.Vector<T>`
- **Lazy evaluation** with query optimization (predicate pushdown, projection pushdown, constant folding)
- **Expression-based API** that's both powerful and readable
- **Full null handling** with validity bitmaps (1 bit per value)

## Quick Start

```csharp
using Polaire;
using static Polaire.Pl;

// Create a DataFrame
var df = DataFrame(
    Series("name", new[] { "Alice", "Bob", "Charlie", "Diana" }),
    Series("age", new[] { 25, 30, 35, 28 }),
    Series("score", new[] { 85.5, 92.0, 78.5, 95.0 })
);

// Query with expressions
var result = df
    .Lazy()
    .Filter(Col("age") > 26)
    .Select(Col("name"), Col("score") * 1.1)
    .Collect();

// Group and aggregate
var summary = df
    .Lazy()
    .GroupBy(Col("age") > 30)
    .Agg(
        Col("score").Mean().As("avg_score"),
        Col("name").Count().As("count")
    )
    .Collect();
```

## Features

### Data Types
Full support for 29 data types matching Polars:
- Integers: `Int8`, `Int16`, `Int32`, `Int64`, `Int128`
- Unsigned: `UInt8`, `UInt16`, `UInt32`, `UInt64`, `UInt128`
- Floats: `Float32`, `Float64`
- `Boolean`, `String`, `Binary`, `Decimal`
- Temporal: `Date`, `Time`, `DateTime`, `Duration`
- Nested: `List`, `Struct`, `Array`
- Special: `Categorical`, `Enum`, `Object`, `Null`, `Unknown`

### Operations
```csharp
// Arithmetic (SIMD-optimized)
var total = series1 + series2;
var scaled = series * 2.5;

// Aggregations
var sum = series.Sum();
var mean = series.Mean();
var std = series.Std();

// String operations
var upper = series.Str.ToUpper();
var contains = series.Str.Contains("pattern");

// DateTime operations
var year = series.Dt.Year();
var weekday = series.Dt.DayOfWeek();

// Comparisons
var mask = series > 100;
var between = series.Between(10, 20);
```

### Lazy Evaluation & Query Optimization

```csharp
var optimized = df
    .Lazy()
    .Filter(Col("status") == "active")    // Predicate pushdown
    .Select(Col("id"), Col("name"))       // Projection pushdown
    .Sort(Col("name"))
    .Collect();

// Inspect the query plan
df.Lazy()
    .Filter(Col("x") > 10)
    .Explain();  // Shows optimized plan
```

### Joins
```csharp
var joined = left.Join(
    right,
    Col("key"),
    Col("key"),
    JoinType.Inner
);
```

### GroupBy Aggregations
```csharp
var grouped = df
    .GroupBy("category")
    .Agg(new Dictionary<string, List<string>> {
        ["value"] = new() { "sum", "mean", "count" },
        ["score"] = new() { "min", "max" }
    });
```

## Installation

```bash
dotnet add package Polaire
```

## Performance

Polaire leverages modern .NET performance features:

- **SIMD vectorization** for arithmetic and aggregations
- **64-byte aligned memory** for optimal AVX-512 support
- **Memory pooling** to reduce allocation pressure
- **Chunked arrays** for cache-friendly access patterns
- **Lazy evaluation** to minimize unnecessary computation

## Roadmap

- [x] Core data types and Series
- [x] DataFrame operations
- [x] Expression system
- [x] Lazy evaluation with query optimization
- [x] GroupBy aggregations
- [x] Join operations
- [x] String and DateTime operations
- [ ] CSV/Parquet/JSON I/O
- [ ] SQL interface
- [ ] Streaming/chunked processing
- [ ] GPU acceleration (CUDA/Metal)

## Contributing

Contributions are welcome! Whether it's:

- Bug reports and feature requests
- Performance optimizations
- New operations and data types
- Documentation improvements
- Test coverage

## Philosophy

Polaire aims to be:

1. **Familiar** - If you know Polars, you know Polaire
2. **Fast** - Performance is a feature, not an afterthought
3. **Idiomatic** - Feels natural in C#, not like a port
4. **Complete** - Feature parity is the goal

## License

MIT License - see [LICENSE](LICENSE) for details.

---

*Built with determination and a refusal to write Python.*
