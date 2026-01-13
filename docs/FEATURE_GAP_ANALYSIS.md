# Polaire Feature Gap Analysis & Test Porting Plan

Generated: January 2025

## Executive Summary

This document catalogs the feature gaps between Polaire (current C# implementation) and Polars (reference Rust/Python library), along with a comprehensive test porting strategy.

**Current State:**
- Polaire: 255 tests passing
- Polars: ~5,000-10,000+ tests across 200+ test files
- Estimated feature coverage: ~30-40% of Polars functionality

## Polars Test Suite Structure

```
py-polars/tests/unit/
├── cloud/                  # Cloud storage tests
├── constructors/           # DataFrame/Series construction
├── dataframe/              # DataFrame-specific tests
├── datatypes/              # Data type handling
├── expr/                   # Expression system (9 files)
│   ├── test_binary.py
│   ├── test_dunders.py
│   ├── test_expr_apply_eval.py
│   ├── test_exprs.py
│   ├── test_literal.py
│   ├── test_meta.py
│   ├── test_serde.py
│   └── test_udfs.py
├── functions/              # Top-level functions (14+ files)
│   ├── as_datatype/
│   ├── range/
│   ├── test_business_day_count.py
│   ├── test_col.py
│   ├── test_concat.py
│   ├── test_horizontal.py
│   ├── test_lit.py
│   ├── test_when_then.py
│   └── ...
├── interchange/            # Data interchange
├── interop/                # Python interop
├── io/                     # I/O operations (30+ files)
│   ├── test_csv.py
│   ├── test_parquet.py
│   ├── test_json.py
│   ├── test_lazy_csv.py
│   └── ...
├── lazyframe/              # LazyFrame tests
├── meta/                   # Metadata tests
├── ml/                     # Machine learning
├── operations/             # Core operations (72+ files)
│   ├── aggregation/        # 6 files, ~60 tests
│   ├── arithmetic/
│   ├── map/
│   ├── namespaces/         # String, temporal, list, etc.
│   │   ├── string/         # 4 files, ~90 tests
│   │   ├── temporal/       # 10 files
│   │   ├── list/           # 5 files
│   │   └── array/
│   ├── rolling/
│   ├── unique/
│   ├── test_filter.py
│   ├── test_group_by.py
│   ├── test_join.py
│   ├── test_sort.py
│   ├── test_window.py
│   └── ...
├── series/                 # Series tests (14 files)
├── sql/                    # SQL interface
├── streaming/              # Streaming processing
├── testing/                # Test utilities
└── utils/                  # Utility tests
```

## Feature Gap Analysis

### 1. Data Types (Current Coverage: ~70%)

| Feature | Polaire | Polars | Gap |
|---------|---------|--------|-----|
| Int8/16/32/64 | ✅ | ✅ | - |
| UInt8/16/32/64 | ✅ | ✅ | - |
| Float32/64 | ✅ | ✅ | - |
| Boolean | ✅ | ✅ | - |
| String/LargeString | ✅ | ✅ | - |
| Date | ✅ | ✅ | - |
| DateTime | ✅ | ✅ | - |
| Duration | ❌ | ✅ | **Missing** |
| Time | ❌ | ✅ | **Missing** |
| Categorical | ❌ | ✅ | **Missing** |
| Enum | ❌ | ✅ | **Missing** |
| Decimal | ❌ | ✅ | **Missing** |
| List/Array | ❌ | ✅ | **Missing** |
| Struct | ❌ | ✅ | **Missing** |
| Object | ❌ | ✅ | **Missing** |
| Null | ✅ | ✅ | - |

### 2. Series Operations (Current Coverage: ~50%)

| Operation | Polaire | Polars | Gap |
|-----------|---------|--------|-----|
| Basic aggregations (sum, mean, etc.) | ✅ | ✅ | - |
| SIMD-optimized aggregations | ✅ | ✅ | - |
| Arithmetic (+, -, *, /, %) | ✅ | ✅ | - |
| Comparisons (eq, lt, gt, etc.) | ✅ | ✅ | - |
| Boolean (and, or, xor, not) | ✅ | ✅ | - |
| Sort/ArgSort | ✅ | ✅ | - |
| Unique/ValueCounts | ✅ | ✅ | - |
| FillNull/DropNulls | ✅ | ✅ | - |
| Cast | ✅ | ✅ | - |
| Slice/Head/Tail | ✅ | ✅ | - |
| Quantile | ❌ | ✅ | **Missing** |
| Product | ❌ | ✅ | **Missing** |
| CumSum/CumProd/CumMin/CumMax | ❌ | ✅ | **Missing** |
| Diff | ❌ | ✅ | **Missing** |
| PctChange | ❌ | ✅ | **Missing** |
| Shift | ❌ | ✅ | **Missing** |
| Clip | ❌ | ✅ | **Missing** |
| Abs | ❌ | ✅ | **Missing** |
| Sqrt/Log/Exp/Sin/Cos | ❌ | ✅ | **Missing** |
| Round/Floor/Ceil | ❌ | ✅ | **Missing** |
| Sign | ❌ | ✅ | **Missing** |
| Interpolate | ❌ | ✅ | **Missing** |
| Rolling operations | ❌ | ✅ | **Missing** |
| EWM operations | ❌ | ✅ | **Missing** |
| Rank | ❌ | ✅ | **Missing** |
| Hash | ❌ | ✅ | **Missing** |
| SearchSorted | ❌ | ✅ | **Missing** |
| Gather/Scatter | ❌ | ✅ | **Missing** |
| Explode | ❌ | ✅ | **Missing** |
| Implode | ❌ | ✅ | **Missing** |
| RLE (Run Length Encoding) | ❌ | ✅ | **Missing** |
| Mode | ❌ | ✅ | **Missing** |
| Sample | ✅ | ✅ | - |
| Shuffle | ❌ | ✅ | **Missing** |
| SetSorted | ❌ | ✅ | **Missing** |

### 3. String Operations (Current Coverage: ~40%)

| Operation | Polaire | Polars | Gap |
|-----------|---------|--------|-----|
| to_lowercase/uppercase | ✅ | ✅ | - |
| to_titlecase | ✅ | ✅ | - |
| strip/lstrip/rstrip | ✅ | ✅ | - |
| pad_start/pad_end | ✅ | ✅ | - |
| zfill | ✅ | ✅ | - |
| slice/head/tail | ✅ | ✅ | - |
| contains | ✅ | ✅ | - |
| starts_with/ends_with | ✅ | ✅ | - |
| replace/replace_all | ✅ | ✅ | - |
| split | ✅ | ✅ | - |
| extract | ✅ | ✅ | - |
| lengths/n_chars | ✅ | ✅ | - |
| count_matches | ✅ | ✅ | - |
| json_path_match | ✅ | ✅ | - |
| concat_str | ✅ | ✅ | - |
| reverse | ❌ | ✅ | **Missing** |
| encode/decode (hex, base64) | ❌ | ✅ | **Missing** |
| json_decode | ❌ | ✅ | **Missing** |
| to_integer/to_decimal | ❌ | ✅ | **Missing** |
| normalize (unicode) | ❌ | ✅ | **Missing** |
| find | ❌ | ✅ | **Missing** |
| strip_chars | ❌ | ✅ | **Missing** |
| strip_prefix/suffix | ❌ | ✅ | **Missing** |
| ljust/rjust/center | ❌ | ✅ | **Missing** |
| extract_groups | ❌ | ✅ | **Missing** |
| split_exact/splitn | ❌ | ✅ | **Missing** |
| join | ❌ | ✅ | **Missing** |
| repeat | ❌ | ✅ | **Missing** |
| replace_many | ❌ | ✅ | **Missing** |
| contains_any | ❌ | ✅ | **Missing** |

### 4. DateTime Operations (Current Coverage: ~50%)

| Operation | Polaire | Polars | Gap |
|-----------|---------|--------|-----|
| year/month/day | ✅ | ✅ | - |
| hour/minute/second | ✅ | ✅ | - |
| day_of_week/day_of_year | ✅ | ✅ | - |
| week_of_year | ✅ | ✅ | - |
| quarter | ✅ | ✅ | - |
| truncate | ❌ | ✅ | **Missing** |
| round | ❌ | ✅ | **Missing** |
| offset_by | ❌ | ✅ | **Missing** |
| replace | ❌ | ✅ | **Missing** |
| strftime | ❌ | ✅ | **Missing** |
| strptime | ❌ | ✅ | **Missing** |
| timestamp | ❌ | ✅ | **Missing** |
| epoch | ❌ | ✅ | **Missing** |
| total_seconds/minutes/etc. | ❌ | ✅ | **Missing** |
| is_leap_year | ❌ | ✅ | **Missing** |
| month_start/end | ❌ | ✅ | **Missing** |
| is_business_day | ❌ | ✅ | **Missing** |
| add_business_days | ❌ | ✅ | **Missing** |
| combine | ❌ | ✅ | **Missing** |
| convert_time_zone | ❌ | ✅ | **Missing** |

### 5. DataFrame Operations (Current Coverage: ~60%)

| Operation | Polaire | Polars | Gap |
|-----------|---------|--------|-----|
| Select | ✅ | ✅ | - |
| Filter | ✅ | ✅ | - |
| With_columns | ✅ | ✅ | - |
| Sort | ✅ | ✅ | - |
| Group_by | ✅ | ✅ | - |
| Join (inner, left, outer) | ✅ | ✅ | - |
| Concat (vstack, hstack) | ✅ | ✅ | - |
| Unique | ✅ | ✅ | - |
| Drop | ✅ | ✅ | - |
| Rename | ✅ | ✅ | - |
| Head/Tail | ✅ | ✅ | - |
| Sample | ✅ | ✅ | - |
| Describe | ✅ | ✅ | - |
| Melt/Unpivot | ✅ | ✅ | - |
| Pivot | ❌ | ✅ | **Missing** |
| Join_asof | ❌ | ✅ | **Missing** |
| Cross_join | ❌ | ✅ | **Missing** |
| Semi_join/Anti_join | ❌ | ✅ | **Missing** |
| Inequality_join | ❌ | ✅ | **Missing** |
| Group_by_dynamic | ❌ | ✅ | **Missing** |
| Rolling (on DataFrame) | ❌ | ✅ | **Missing** |
| Transpose | ❌ | ✅ | **Missing** |
| Explode | ❌ | ✅ | **Missing** |
| Partition_by | ❌ | ✅ | **Missing** |
| Rechunk | ❌ | ✅ | **Missing** |
| Clone | ❌ | ✅ | **Missing** |
| Clear | ❌ | ✅ | **Missing** |
| Extend | ❌ | ✅ | **Missing** |
| Insert_at_idx | ❌ | ✅ | **Missing** |
| Replace | ❌ | ✅ | **Missing** |
| Shrink_to_fit | ❌ | ✅ | **Missing** |
| Unnest | ❌ | ✅ | **Missing** |
| Top_k | ❌ | ✅ | **Missing** |
| Approx_n_unique | ❌ | ✅ | **Missing** |
| Merge_sorted | ❌ | ✅ | **Missing** |
| Map_rows | ❌ | ✅ | **Missing** |

### 6. Expression System (Current Coverage: ~50%)

| Feature | Polaire | Polars | Gap |
|---------|---------|--------|-----|
| col/cols | ✅ | ✅ | - |
| lit | ✅ | ✅ | - |
| Binary operations | ✅ | ✅ | - |
| Unary operations | ✅ | ✅ | - |
| Aggregations in expr | ✅ | ✅ | - |
| when/then/otherwise | ✅ | ✅ | - |
| alias | ✅ | ✅ | - |
| cast | ✅ | ✅ | - |
| sort_by | ✅ | ✅ | - |
| fill_null | ✅ | ✅ | - |
| over (window) | Partial | ✅ | **Incomplete** |
| String expr namespace | ✅ | ✅ | - |
| DateTime expr namespace | ✅ | ✅ | - |
| List expr namespace | ❌ | ✅ | **Missing** |
| Struct expr namespace | ❌ | ✅ | **Missing** |
| Cat expr namespace | ❌ | ✅ | **Missing** |
| Binary expr namespace | ❌ | ✅ | **Missing** |
| Arr expr namespace | ❌ | ✅ | **Missing** |
| Name expr namespace | ❌ | ✅ | **Missing** |
| all/exclude/first/last | ✅ | ✅ | - |
| arg_sort/arg_min/arg_max | ✅ | ✅ | - |
| rank | ❌ | ✅ | **Missing** |
| rolling_* | ❌ | ✅ | **Missing** |
| ewm_* | ❌ | ✅ | **Missing** |
| cumulative_* | ❌ | ✅ | **Missing** |
| shift | ❌ | ✅ | **Missing** |
| diff | ❌ | ✅ | **Missing** |
| pct_change | ❌ | ✅ | **Missing** |
| interpolate | ❌ | ✅ | **Missing** |
| map/apply | ❌ | ✅ | **Missing** |

### 7. I/O Operations (Current Coverage: ~60%)

| Format | Read | Write | Scan | Gap |
|--------|------|-------|------|-----|
| CSV | ✅ | ✅ | ✅ | - |
| Parquet | ✅ | ✅ | ✅ | - |
| JSON | ✅ | ❌ | ✅ | **Write missing** |
| NDJSON | ✅ | ❌ | ✅ | **Write missing** |
| IPC/Arrow | ❌ | ❌ | ❌ | **Missing** |
| Avro | ❌ | ❌ | ❌ | **Missing** |
| Excel | ❌ | ❌ | ❌ | **Missing** |
| Delta Lake | ❌ | ❌ | ❌ | **Missing** |
| Iceberg | ❌ | ❌ | ❌ | **Missing** |
| Database | ❌ | ❌ | ❌ | **Missing** |

### 8. LazyFrame Operations (Current Coverage: ~40%)

| Feature | Polaire | Polars | Gap |
|---------|---------|--------|-----|
| Select | ✅ | ✅ | - |
| Filter | ✅ | ✅ | - |
| With_columns | ✅ | ✅ | - |
| Sort | ✅ | ✅ | - |
| Group_by | ✅ | ✅ | - |
| Join | ✅ | ✅ | - |
| Collect | ✅ | ✅ | - |
| Predicate pushdown | ✅ | ✅ | - |
| Projection pushdown | ✅ | ✅ | - |
| Slice pushdown | ❌ | ✅ | **Missing** |
| Common subexpr elimination | ❌ | ✅ | **Missing** |
| Streaming execution | ❌ | ✅ | **Missing** |
| Explain | ❌ | ✅ | **Missing** |
| Show_graph | ❌ | ✅ | **Missing** |
| Profile | ❌ | ✅ | **Missing** |
| Cache | ❌ | ✅ | **Missing** |
| Sink_* | ❌ | ✅ | **Missing** |
| Fetch | ❌ | ✅ | **Missing** |

### 9. Advanced Features (Current Coverage: ~10%)

| Feature | Polaire | Polars | Gap |
|---------|---------|--------|-----|
| SQL interface | ❌ | ✅ | **Missing** |
| Window functions | Partial | ✅ | **Incomplete** |
| UDF support | ❌ | ✅ | **Missing** |
| Plugins | ❌ | ✅ | **Missing** |
| GPU acceleration | ❌ | ✅ | **Missing** |
| Distributed | ❌ | ✅ | **Missing** |
| Schema evolution | ❌ | ✅ | **Missing** |
| Config system | ❌ | ✅ | **Missing** |

## Test Porting Strategy

### Phase 1: Core Operations (Weeks 1-2)
**Goal: Port ~500 tests**

1. **Aggregation Tests** (`operations/aggregation/`)
   - `test_aggregations.py` - 57 tests
   - `test_folds.py`
   - `test_horizontal.py`
   - `test_implode.py`
   - `test_vertical.py`

2. **Series Tests** (`series/`)
   - `test_series.py` - Core series operations
   - `test_all_any.py`
   - `test_append.py`
   - `test_equals.py`
   - `test_getitem.py`

3. **Basic DataFrame Tests** (`dataframe/`)
   - `test_df.py` - Core DataFrame operations

### Phase 2: String & Temporal Operations (Weeks 3-4)
**Goal: Port ~400 tests, implement missing string/datetime operations**

1. **String Tests** (`operations/namespaces/string/`)
   - `test_string.py` - 90+ tests
   - `test_concat.py`
   - `test_pad.py`

2. **Temporal Tests** (`operations/namespaces/temporal/`)
   - `test_datetime.py`
   - `test_truncate.py`
   - `test_round.py`
   - `test_offset_by.py`

### Phase 3: Core Operations (Weeks 5-8)
**Goal: Port ~1,500 tests, implement missing operations**

1. **Filter/Sort/Unique** (`operations/`)
   - `test_filter.py`
   - `test_sort.py`
   - `test_unique/`

2. **Group By** (`operations/`)
   - `test_group_by.py`
   - `test_group_by_dynamic.py`

3. **Joins** (`operations/`)
   - `test_join.py`
   - `test_join_asof.py`
   - `test_cross_join.py`
   - `test_inequality_join.py`

4. **Transformations**
   - `test_cast.py`
   - `test_clip.py`
   - `test_diff.py`
   - `test_shift.py`
   - `test_interpolate.py`

### Phase 4: Advanced Operations (Weeks 9-12)
**Goal: Port ~1,000 tests, implement window/rolling/ewm**

1. **Window Functions** (`operations/`)
   - `test_window.py`
   - `test_over.py`

2. **Rolling Operations** (`operations/rolling/`)
   - All rolling tests

3. **EWM Operations** (`operations/`)
   - `test_ewm.py`
   - `test_ewm_by.py`

### Phase 5: I/O & LazyFrame (Weeks 13-16)
**Goal: Port ~800 tests**

1. **I/O Tests** (`io/`)
   - All format-specific tests
   - Lazy scan tests

2. **LazyFrame Tests** (`lazyframe/`)
   - Optimization tests
   - Execution tests

### Phase 6: Expression System & SQL (Weeks 17-20)
**Goal: Port ~500 tests, implement SQL**

1. **Expression Tests** (`expr/`)
   - All expression tests

2. **SQL Tests** (`sql/`)
   - SQL parsing tests
   - SQL execution tests

### Phase 7: Advanced Features (Weeks 21-24)
**Goal: Port remaining ~800 tests**

1. **List/Struct/Categorical** (`datatypes/`, `operations/namespaces/`)
2. **Streaming** (`streaming/`)
3. **Interoperability** (`interop/`, `interchange/`)

## Implementation Priority

Based on frequency of use in real-world data analysis:

### P0 - Critical (Implement First)
1. Cumulative operations (cumsum, cumprod, etc.)
2. Shift/Diff/PctChange
3. Window functions (basic)
4. Quantile aggregation
5. Clip operation
6. More string operations (find, reverse, encode/decode)

### P1 - High Priority
1. Rolling operations
2. EWM operations
3. Rank
4. Interpolate
5. SQL interface
6. Additional datetime operations

### P2 - Medium Priority
1. List/Array type and operations
2. Struct type and operations
3. AsOf joins
4. Cross joins
5. Streaming execution
6. IPC format

### P3 - Lower Priority
1. Categorical type
2. Enum type
3. Delta Lake/Iceberg
4. GPU acceleration
5. Plugins
6. Distributed execution

## Testing Approach

### 1. Direct Port
- Translate Python pytest tests to xUnit
- Maintain same test names for traceability
- Use `[Fact]` for single tests, `[Theory]` for parameterized

### 2. Property-Based Testing
- Add FsCheck for hypothesis-style testing
- Generate random DataFrames with specified schemas
- Test invariants and edge cases

### 3. Performance Regression Tests
- Benchmark critical paths
- Compare against Polars Python for parity
- Track performance over time

### 4. Integration Tests
- End-to-end workflows
- Real-world data scenarios
- Memory and performance profiling

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Test count | 255 | 5,000+ |
| Feature coverage | ~35% | 95%+ |
| Performance vs Polars | ~parity | ~parity |
| Code coverage | Unknown | 90%+ |

## Next Steps

1. Begin Phase 1: Port aggregation tests
2. Implement missing aggregation operations as tests reveal gaps
3. Continue iteratively through each phase
4. Track progress in CLAUDE.md session notes
