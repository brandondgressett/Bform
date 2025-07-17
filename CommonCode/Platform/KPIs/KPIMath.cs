using BFormDomain.HelperClasses;

namespace BFormDomain.CommonCode.Platform.KPIs;


/// <summary>
/// KPIMath statically provides functions for KPI math calculations
///     -References:
///         >KPIEvaluator.cs
///     -Funtions:
///         >ComputeMovingAverage
///         >ComputeZScore
///         >DetectTailCrossing
///         >WeightedAdd
///         >WeightedSum
///         >WeightedAvg
///         >Rescale
///         >MapToRange
///         >ForceRange
///         >DetectZScoreSignal
///         >DetectThreshold
/// </summary>
public static class KPIMath
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="period"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static List<double> ComputeMovingAverage(int period, List<double> source)
    {
        period = Math.Min(period, source.Count);
        var ma = new double[source.Count];

        double sum = 0;
        for (int bar = 0; bar < period; bar++)
            sum += source[bar];

        ma[period - 1] = sum / period;

        for (int bar = period; bar < source.Count; bar++)
            ma[bar] = ma[bar - 1] + source[bar] / period
                                  - source[bar - period] / period;

        return ma.ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="period"></param>
    /// <param name="source"></param>
    /// <param name="threshold"></param>
    /// <param name="influence"></param>
    /// <returns></returns>
    public static List<double> ComputeZScore(int period, List<double> source, double threshold = 3.5, double influence = 0.33 )
    {
        var output = ZScore.StartAlgo(source, period, threshold, influence);
        return output.signals.EmptyIfNull().Select(it => (double)it).ToList();
    }
    
    public static int DetectTailCrossing(
        IList<double> main, IList<double> supporting)
    {
        if (main.Count != supporting.Count || main.Count < 2 || supporting.Count < 2)
            return (int) KPISignalType.None;

        var mainTail = main.Skip(main.Count - 2).ToArray();
        var suppTail = supporting.Skip(supporting.Count - 2).ToArray();

        KPISignalType best = KPISignalType.None;

        if (mainTail[0] > suppTail[0] && mainTail[1] < suppTail[1])
            best = KPISignalType.CrossingUp;
        else if(mainTail[0] < suppTail[0] && mainTail[1] > suppTail[1])
            best = KPISignalType.CrossingDown;

        return (int) best;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="wa"></param>
    /// <param name="a"></param>
    /// <param name="wb"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static List<double> WeightedAdd(double wa, List<double> a, double wb, List<double> b)
    {
        return a.Zip(b).Select(z => z.First * wa + z.Second * wb).ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="wa"></param>
    /// <param name="a"></param>
    /// <param name="wb"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static double WeightedSum(double wa, List<double> a, double wb, List<double> b)
    {
        return a.Zip(b).Select(z => z.First * wa + z.Second * wb).Sum();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="wa"></param>
    /// <param name="a"></param>
    /// <param name="wb"></param>
    /// <param name="b"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static List<double> WeightedAvg(double wa, List<double> a, double wb, List<double> b, double scale)
    {
        return a.Zip(b).Select(z => (z.First * wa + z.Second * wb) / scale).ToList();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public static List<double> Rescale(double scale, List<double> input)
    {
        return input.Select(x => x * scale).ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="range1Min"></param>
    /// <param name="range1Max"></param>
    /// <param name="range2Min"></param>
    /// <param name="range2Max"></param>
    /// <param name="clamp"></param>
    /// <returns></returns>
    public static double MapToRange(double value,
            double range1Min, double range1Max,
            double range2Min, double range2Max,
            bool clamp)
    {

        value = range2Min + ((value - range1Min) / (range1Max - range1Min)) * (range2Max - range2Min);

        if (clamp)
        {
            if (range2Min < range2Max)
            {
                if (value > range2Max) value = range2Max;
                if (value < range2Min) value = range2Min;
            }
            // Range that go negative are possible, for example from 0 to -1
            else
            {
                if (value > range2Min) value = range2Min;
                if (value < range2Max) value = range2Max;
            }
        }

        return value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public static List<double> ForceRange(double min, double max, List<double> input)
    {
        var vmin = input.Min();
        var vmax = input.Max();
        return input.Select(x => MapToRange(x, vmin, vmax, min, max, true)).ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="detectOff"></param>
    /// <returns></returns>
    public static int DetectZScoreSignal(List<double> input, bool detectOff = false)
    {
        return (int) (input.Last() > 0.0 ? KPISignalType.ActivationOn : 
                                    detectOff ? KPISignalType.ActivationOff : KPISignalType.None);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public static int DetectThreshold(double min, double max, List<double> input)
    {
        var signal = KPISignalType.None;

        var value = input.Last();
        if (value < min)
            signal = KPISignalType.ThresholdBelow;
        else if (value > max) 
            signal = KPISignalType.ThresholdAbove;

        return (int)signal;
    }
}
