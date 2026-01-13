// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Polaire;


using Polaire.Expressions;
using static Polaire.Expressions.Expr;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class SeriesArithmeticBenchmarks
{
    private Series _a = null!;
    private Series _b = null!;

    [Params(1000, 10000, 100000, 1000000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var valuesA = Enumerable.Range(0, N).Select(_ => random.NextDouble() * 1000).ToArray();
        var valuesB = Enumerable.Range(0, N).Select(_ => random.NextDouble() * 1000).ToArray();

        _a = Series.FromValues("a", valuesA);
        _b = Series.FromValues("b", valuesB);
    }

    [Benchmark]
    public Series Addition() => _a + _b;

    [Benchmark]
    public Series Multiplication() => _a * _b;

    [Benchmark]
    public Series ScalarMultiplication() => _a * 2.0;
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class SeriesAggregationBenchmarks
{
    private Series _series = null!;

    [Params(1000, 10000, 100000, 1000000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var values = Enumerable.Range(0, N).Select(_ => random.NextDouble() * 1000).ToArray();
        _series = Series.FromValues("data", values);
    }

    [Benchmark]
    public object Sum() => _series.Sum();

    [Benchmark]
    public object Mean() => _series.Mean();

    [Benchmark]
    public object Min() => _series.Min();

    [Benchmark]
    public object Max() => _series.Max();

    [Benchmark]
    public object Std() => _series.Std();
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class DataFrameOperationsBenchmarks
{
    private DataFrame _df = null!;

    [Params(1000, 10000, 100000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var a = Enumerable.Range(0, N).Select(_ => random.NextDouble() * 1000).ToArray();
        var b = Enumerable.Range(0, N).Select(_ => random.NextDouble() * 1000).ToArray();
        var c = Enumerable.Range(0, N).Select(_ => random.Next(10)).ToArray();
        var groups = Enumerable.Range(0, N).Select(i => $"group_{i % 100}").ToArray();

        _df = new DataFrame(
            Series.FromValues("a", a),
            Series.FromValues("b", b),
            Series.FromValues("c", c),
            Series.FromValues("group", groups)
        );
    }

    [Benchmark]
    public DataFrame Select() => _df.Select("a", "b");

    [Benchmark]
    public DataFrame FilterGtMean()
    {
        var mean = _df["a"].Mean();
        var mask = _df["a"].Gt(mean);
        return _df.Filter(mask);
    }

    [Benchmark]
    public DataFrame Sort() => _df.Sort("a");

    [Benchmark]
    public DataFrame GroupBySum() => _df.GroupBy("group").Sum();

    [Benchmark]
    public DataFrame Head() => _df.Head(100);
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class LazyFrameBenchmarks
{
    private DataFrame _df = null!;

    [Params(10000, 100000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var a = Enumerable.Range(0, N).Select(_ => random.NextDouble() * 1000).ToArray();
        var b = Enumerable.Range(0, N).Select(_ => random.NextDouble() * 1000).ToArray();
        var groups = Enumerable.Range(0, N).Select(i => $"group_{i % 100}").ToArray();

        _df = new DataFrame(
            Series.FromValues("a", a),
            Series.FromValues("b", b),
            Series.FromValues("group", groups)
        );
    }

    [Benchmark]
    public DataFrame LazySelectFilterCollect()
    {
        return _df.Lazy()
            .Select(Col("a"), Col("b"), (Col("a") + Col("b")).As("sum"))
            .Filter(Col("sum").Gt(1000))
            .Collect();
    }

    [Benchmark]
    public DataFrame LazyChainedOperations()
    {
        return _df.Lazy()
            .Filter(Col("a").Gt(500))
            .WithColumns((Col("a") * Col("b")).As("product"))
            .Sort(Col("product"))
            .Head(100)
            .Collect();
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class JoinBenchmarks
{
    private DataFrame _left = null!;
    private DataFrame _right = null!;

    [Params(1000, 10000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var leftKeys = Enumerable.Range(0, N).ToArray();
        var leftValues = Enumerable.Range(0, N).Select(i => $"left_{i}").ToArray();

        var rightKeys = Enumerable.Range(N / 2, N).ToArray(); // 50% overlap
        var rightValues = Enumerable.Range(0, N).Select(i => $"right_{i}").ToArray();

        _left = new DataFrame(
            Series.FromValues("key", leftKeys),
            Series.FromValues("left_val", leftValues)
        );

        _right = new DataFrame(
            Series.FromValues("key", rightKeys),
            Series.FromValues("right_val", rightValues)
        );
    }

    [Benchmark]
    public DataFrame InnerJoin() => _left.Join(_right, "key");

    [Benchmark]
    public DataFrame LeftJoin() => _left.LeftJoin(_right, "key");
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class StringOperationsBenchmarks
{
    private Series _strings = null!;

    [Params(1000, 10000, 100000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var words = new[] { "hello", "world", "testing", "benchmark", "polaire", "dataframe" };
        var values = Enumerable.Range(0, N)
            .Select(_ => $"{words[random.Next(words.Length)]}_{random.Next(1000)}")
            .ToArray();

        _strings = Series.FromValues("strings", values);
    }

    [Benchmark]
    public Series ToLowerCase() => _strings.Str.ToLowerCase();

    [Benchmark]
    public Series ToUpperCase() => _strings.Str.ToUpperCase();

    [Benchmark]
    public Series Contains() => _strings.Str.Contains("test");

    [Benchmark]
    public Series Replace() => _strings.Str.Replace("hello", "hi");

    [Benchmark]
    public Series Lengths() => _strings.Str.Lengths();
}
