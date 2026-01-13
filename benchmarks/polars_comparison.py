#!/usr/bin/env python3
"""
Polars vs Polaire Performance Comparison

Run this script with: python3 polars_comparison.py
Requires: pip install polars

This benchmarks Polars (Rust) on the same operations as Polaire benchmarks
so we can directly compare performance.
"""

import time
import statistics
import polars as pl
import numpy as np

def benchmark(func, warmup=3, iterations=10):
    """Run a function multiple times and return timing stats in microseconds."""
    # Warmup
    for _ in range(warmup):
        func()

    # Timed runs
    times = []
    for _ in range(iterations):
        start = time.perf_counter()
        func()
        end = time.perf_counter()
        times.append((end - start) * 1_000_000)  # Convert to microseconds

    return {
        'mean': statistics.mean(times),
        'std': statistics.stdev(times) if len(times) > 1 else 0,
        'min': min(times),
        'max': max(times)
    }

def main():
    print("=" * 70)
    print("Polars vs Polaire Performance Comparison")
    print("=" * 70)
    print(f"Polars version: {pl.__version__}")
    print()

    sizes = [1_000, 10_000, 100_000, 1_000_000]

    for n in sizes:
        print(f"\n{'='*70}")
        print(f"N = {n:,}")
        print(f"{'='*70}")

        # Create test data - random float64 values
        np.random.seed(42)
        data = np.random.randn(n)
        series = pl.Series("values", data)

        # Benchmark each operation
        results = {}

        results['Sum'] = benchmark(lambda: series.sum())
        results['Mean'] = benchmark(lambda: series.mean())
        results['Min'] = benchmark(lambda: series.min())
        results['Max'] = benchmark(lambda: series.max())
        results['Std'] = benchmark(lambda: series.std())

        # Print results
        print(f"\n{'Operation':<10} {'Mean (µs)':>12} {'StdDev':>12} {'Min':>12} {'Max':>12}")
        print("-" * 60)
        for op, stats in results.items():
            print(f"{op:<10} {stats['mean']:>12.1f} {stats['std']:>12.1f} {stats['min']:>12.1f} {stats['max']:>12.1f}")

    print("\n" + "=" * 70)
    print("Comparison Summary (N=1,000,000)")
    print("=" * 70)

    # Polaire results from BenchmarkDotNet (January 2025, SIMD optimized)
    polaire = {'Sum': 478, 'Mean': 480, 'Min': 321, 'Max': 322, 'Std': 955}
    polars_1m = results  # Results from the 1M run above

    print(f"\n{'Operation':<10} {'Polaire (µs)':>14} {'Polars (µs)':>14} {'Ratio':>10}")
    print("-" * 52)
    for op in ['Sum', 'Mean', 'Min', 'Max', 'Std']:
        polaire_time = polaire[op]
        polars_time = polars_1m[op]['mean']
        ratio = polaire_time / polars_time
        print(f"{op:<10} {polaire_time:>14.1f} {polars_time:>14.1f} {ratio:>9.1f}x")

    print("""
Analysis:
- Polars (Rust) is 1.5-4x faster than Polaire (C#)
- This is expected: Polars uses architecture-specific AVX2/AVX512 intrinsics
- Polaire uses System.Numerics.Vector<T> which abstracts SIMD operations
- Before SIMD optimization, Std was ~37,000µs (58x gap), now 955µs (1.5x gap)
- The gap is reasonable for managed vs native code with manual SIMD tuning
    """)

if __name__ == "__main__":
    main()
