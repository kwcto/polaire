// Licensed under the MIT License.
// Polaire - High-performance DataFrame library for .NET

using Apache.Arrow;
using Polaire.DataTypes;

namespace Polaire.Compute;

/// <summary>
/// Exponentially weighted moving (EWM) functions for Series.
/// These are commonly used in time series analysis for smoothing data.
/// </summary>
public static class EwmOperations
{
    // ============================================================================
    // Alpha Calculation from Different Parameters
    // ============================================================================

    /// <summary>
    /// Calculates alpha from span. Alpha = 2 / (span + 1).
    /// Common in pandas/polars for specifying decay.
    /// </summary>
    public static double AlphaFromSpan(double span)
    {
        if (span < 1)
            throw new ArgumentException("span must be >= 1", nameof(span));
        return 2.0 / (span + 1.0);
    }

    /// <summary>
    /// Calculates alpha from halflife. Alpha = 1 - exp(-ln(2)/halflife).
    /// Halflife is the period for the weight to decay to half.
    /// </summary>
    public static double AlphaFromHalflife(double halflife)
    {
        if (halflife <= 0)
            throw new ArgumentException("halflife must be > 0", nameof(halflife));
        return 1.0 - Math.Exp(-Math.Log(2.0) / halflife);
    }

    /// <summary>
    /// Calculates alpha from center of mass. Alpha = 1 / (1 + com).
    /// </summary>
    public static double AlphaFromCom(double com)
    {
        if (com < 0)
            throw new ArgumentException("com must be >= 0", nameof(com));
        return 1.0 / (1.0 + com);
    }

    // ============================================================================
    // EWM Mean
    // ============================================================================

    /// <summary>
    /// Exponentially weighted moving average with alpha directly specified.
    /// </summary>
    /// <param name="series">Input series</param>
    /// <param name="alpha">Smoothing factor (0 &lt; alpha &lt;= 1)</param>
    /// <param name="adjust">If true, use bias-corrected formula</param>
    /// <param name="ignoreNulls">If true, skip nulls; if false, propagate nulls</param>
    /// <param name="minPeriods">Minimum observations before producing values</param>
    public static Series EwmMean(Series series, double alpha, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1)
    {
        ValidateAlpha(alpha);

        var builder = new DoubleArray.Builder();

        if (series.Length == 0)
            return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);

        double ewma = 0;
        double sumWeights = 0;
        int validCount = 0;

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                if (ignoreNulls)
                {
                    // Skip this value but output the current EWMA
                    if (validCount >= minPeriods)
                    {
                        if (adjust)
                            builder.Append(ewma / sumWeights);
                        else
                            builder.Append(ewma);
                    }
                    else
                    {
                        builder.AppendNull();
                    }
                }
                else
                {
                    // Propagate null
                    builder.AppendNull();
                }
                continue;
            }

            var val = series[i];
            double x;
            if (val.TryGetDouble(out var d))
                x = d;
            else if (val.TryGetInt64(out var l))
                x = l;
            else
            {
                builder.AppendNull();
                continue;
            }

            validCount++;

            if (adjust)
            {
                // Bias-adjusted formula: EWMA = sum(alpha^(n-i) * x_i) / sum(alpha^(n-i))
                sumWeights = alpha + (1 - alpha) * sumWeights;
                ewma = alpha * x + (1 - alpha) * ewma;

                if (validCount >= minPeriods)
                    builder.Append(ewma / sumWeights);
                else
                    builder.AppendNull();
            }
            else
            {
                // Simple formula: EWMA_t = alpha * x_t + (1-alpha) * EWMA_{t-1}
                if (validCount == 1)
                    ewma = x;
                else
                    ewma = alpha * x + (1 - alpha) * ewma;

                if (validCount >= minPeriods)
                    builder.Append(ewma);
                else
                    builder.AppendNull();
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// EWM mean with span parameter.
    /// </summary>
    public static Series EwmMeanSpan(Series series, double span, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1)
    {
        return EwmMean(series, AlphaFromSpan(span), adjust, ignoreNulls, minPeriods);
    }

    /// <summary>
    /// EWM mean with halflife parameter.
    /// </summary>
    public static Series EwmMeanHalflife(Series series, double halflife, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1)
    {
        return EwmMean(series, AlphaFromHalflife(halflife), adjust, ignoreNulls, minPeriods);
    }

    /// <summary>
    /// EWM mean with center of mass parameter.
    /// </summary>
    public static Series EwmMeanCom(Series series, double com, bool adjust = true, bool ignoreNulls = true, int minPeriods = 1)
    {
        return EwmMean(series, AlphaFromCom(com), adjust, ignoreNulls, minPeriods);
    }

    // ============================================================================
    // EWM Variance
    // ============================================================================

    /// <summary>
    /// Exponentially weighted moving variance.
    /// </summary>
    /// <param name="series">Input series</param>
    /// <param name="alpha">Smoothing factor (0 &lt; alpha &lt;= 1)</param>
    /// <param name="adjust">If true, use bias-corrected formula</param>
    /// <param name="ignoreNulls">If true, skip nulls; if false, propagate nulls</param>
    /// <param name="minPeriods">Minimum observations before producing values</param>
    /// <param name="bias">If true, use biased variance (n denominator); if false, use unbiased (n-1)</param>
    public static Series EwmVar(Series series, double alpha, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        ValidateAlpha(alpha);

        var builder = new DoubleArray.Builder();

        if (series.Length == 0)
            return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);

        // We need at least 2 values for variance
        double ewma = 0;
        double ewmvar = 0;
        double sumWeights = 0;
        double sumWeightsSq = 0;
        int validCount = 0;

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                if (ignoreNulls)
                {
                    if (validCount >= minPeriods)
                        builder.Append(ewmvar);
                    else
                        builder.AppendNull();
                }
                else
                {
                    builder.AppendNull();
                }
                continue;
            }

            var val = series[i];
            double x;
            if (val.TryGetDouble(out var d))
                x = d;
            else if (val.TryGetInt64(out var l))
                x = l;
            else
            {
                builder.AppendNull();
                continue;
            }

            validCount++;

            if (validCount == 1)
            {
                ewma = x;
                ewmvar = 0;
                sumWeights = 1;
                sumWeightsSq = 1;
                builder.AppendNull();  // Need at least 2 for variance
                continue;
            }

            // Update weights
            double oldSumWeights = sumWeights;
            sumWeights = alpha + (1 - alpha) * sumWeights;
            sumWeightsSq = alpha * alpha + (1 - alpha) * (1 - alpha) * sumWeightsSq;

            // Update EWMA
            double oldEwma = ewma;
            ewma = (alpha * x + (1 - alpha) * oldEwma * oldSumWeights) / sumWeights;

            // Update EWM Variance using online algorithm
            // Var = E[X^2] - E[X]^2 form, with exponential weighting
            double delta = x - oldEwma;
            ewmvar = (1 - alpha) * (ewmvar + alpha * delta * delta);

            if (adjust)
            {
                // Bias correction for variance
                double correction = sumWeights * sumWeights / (sumWeights * sumWeights - sumWeightsSq);
                if (!bias && validCount > 1)
                    ewmvar *= correction;
            }

            if (validCount >= minPeriods)
                builder.Append(ewmvar);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// EWM variance with span parameter.
    /// </summary>
    public static Series EwmVarSpan(Series series, double span, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        return EwmVar(series, AlphaFromSpan(span), adjust, ignoreNulls, minPeriods, bias);
    }

    /// <summary>
    /// EWM variance with halflife parameter.
    /// </summary>
    public static Series EwmVarHalflife(Series series, double halflife, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        return EwmVar(series, AlphaFromHalflife(halflife), adjust, ignoreNulls, minPeriods, bias);
    }

    /// <summary>
    /// EWM variance with center of mass parameter.
    /// </summary>
    public static Series EwmVarCom(Series series, double com, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        return EwmVar(series, AlphaFromCom(com), adjust, ignoreNulls, minPeriods, bias);
    }

    // ============================================================================
    // EWM Standard Deviation
    // ============================================================================

    /// <summary>
    /// Exponentially weighted moving standard deviation.
    /// </summary>
    public static Series EwmStd(Series series, double alpha, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        var variance = EwmVar(series, alpha, adjust, ignoreNulls, minPeriods, bias);

        var builder = new DoubleArray.Builder();
        for (int i = 0; i < variance.Length; i++)
        {
            if (variance.IsNull(i))
            {
                builder.AppendNull();
            }
            else
            {
                var v = variance[i].AsFloat64();
                builder.Append(Math.Sqrt(Math.Max(0, v)));
            }
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// EWM standard deviation with span parameter.
    /// </summary>
    public static Series EwmStdSpan(Series series, double span, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        return EwmStd(series, AlphaFromSpan(span), adjust, ignoreNulls, minPeriods, bias);
    }

    /// <summary>
    /// EWM standard deviation with halflife parameter.
    /// </summary>
    public static Series EwmStdHalflife(Series series, double halflife, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        return EwmStd(series, AlphaFromHalflife(halflife), adjust, ignoreNulls, minPeriods, bias);
    }

    /// <summary>
    /// EWM standard deviation with center of mass parameter.
    /// </summary>
    public static Series EwmStdCom(Series series, double com, bool adjust = true, bool ignoreNulls = true, int minPeriods = 2, bool bias = false)
    {
        return EwmStd(series, AlphaFromCom(com), adjust, ignoreNulls, minPeriods, bias);
    }

    // ============================================================================
    // EWM Sum
    // ============================================================================

    /// <summary>
    /// Exponentially weighted moving sum.
    /// </summary>
    public static Series EwmSum(Series series, double alpha, bool ignoreNulls = true, int minPeriods = 1)
    {
        ValidateAlpha(alpha);

        var builder = new DoubleArray.Builder();

        if (series.Length == 0)
            return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);

        double ewmSum = 0;
        int validCount = 0;

        for (int i = 0; i < series.Length; i++)
        {
            if (series.IsNull(i))
            {
                if (ignoreNulls)
                {
                    // Decay the sum but don't add anything
                    ewmSum *= (1 - alpha);
                    if (validCount >= minPeriods)
                        builder.Append(ewmSum);
                    else
                        builder.AppendNull();
                }
                else
                {
                    builder.AppendNull();
                }
                continue;
            }

            var val = series[i];
            double x;
            if (val.TryGetDouble(out var d))
                x = d;
            else if (val.TryGetInt64(out var l))
                x = l;
            else
            {
                builder.AppendNull();
                continue;
            }

            validCount++;

            // EWM Sum: sum_t = x_t + (1-alpha) * sum_{t-1}
            ewmSum = x + (1 - alpha) * ewmSum;

            if (validCount >= minPeriods)
                builder.Append(ewmSum);
            else
                builder.AppendNull();
        }

        return Series.FromArrowArray(series.Name, builder.Build(), DataType.Float64);
    }

    /// <summary>
    /// EWM sum with span parameter.
    /// </summary>
    public static Series EwmSumSpan(Series series, double span, bool ignoreNulls = true, int minPeriods = 1)
    {
        return EwmSum(series, AlphaFromSpan(span), ignoreNulls, minPeriods);
    }

    // ============================================================================
    // Helpers
    // ============================================================================

    private static void ValidateAlpha(double alpha)
    {
        if (alpha <= 0 || alpha > 1)
            throw new ArgumentException("alpha must be in (0, 1]", nameof(alpha));
    }
}
