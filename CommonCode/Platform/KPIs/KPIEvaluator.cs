using BFormDomain.CommonCode.Platform.Tables;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using CodingSeb.ExpressionEvaluator;
using Microsoft.Extensions.Logging;
using System.Text;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.KPIs;





/// <summary>
/// KPIEvaluator is used to evaluate data and use KPIMath to calulate math for charts
///     -References:
///         >KPILogic.cs
///     -Funtions:
///         >ComputeSamplePoints
///         >QueryKPISource
///         >MakeScript
///         >EvalKPICompute
///         >EvalKPISignal
///         >BuildEnvironment
///         >ComputeKPI
/// </summary>
public class KPIEvaluator
{

    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly TableLogic _tables;
    private readonly ILogger<KPIEvaluator> _log;
    
    public KPIEvaluator(
        ILogger<KPIEvaluator> log,
        IApplicationAlert alerts,
        IApplicationTerms terms,
        TableLogic tables)
    {
        _log = log;
        _alerts = alerts;
        _terms = terms; 
        _tables = tables;
        
    }

    private record SampleSpace(List<DateTime> Points, DateTime Begin, DateTime End, double WalkSeconds);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="template"></param>
    /// <returns></returns>
    private static SampleSpace ComputeSamplePoints(
        KPITemplate template, 
        DateTime? endTime)
    {
        template.SampleCount.Requires().IsGreaterThan(1);

        // date range to query
        var end = endTime ?? DateTime.UtcNow;
        var begin = template.ComputeTimeFrame.BackFrom(end);
                

        var walkTime = (end - begin).TotalSeconds / template.SampleCount;
        var points = new List<DateTime>();
        for (DateTime itTime = begin; itTime < end; itTime = itTime.AddSeconds(walkTime))
            points.Add(itTime);

        return new SampleSpace(Points: points, Begin: begin, End: end, WalkSeconds: walkTime);

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sampleSpace"></param>
    /// <param name="template"></param>
    /// <param name="instance"></param>
    /// <param name="source"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    /// <exception cref="KPIInsufficientDataException"></exception>
    private async Task<bool> QueryKPISource(
        SampleSpace sampleSpace,
        KPITemplate template,
        KPIInstance instance,
        KPISource source,
        Dictionary<string,object> env)
    {
        using (PerfTrack.Stopwatch(nameof(QueryKPISource)))
        {
            try
            {
                

                // set up filter
                var query = new TableQueryCommand
                {
                    GtDateFilter = sampleSpace.Begin,
                    LtDateFilter = sampleSpace.End,
                    Ordering = QueryOrdering.Date,
                    UserFilter = (source.UserSubject && instance.SubjectUser is not null)
                                    ? instance.SubjectUser : null,
                    WorkItemFilter = source.WorkItemSubject ?
                                        instance.SubjectWorkItem : null,
                    WorkSetFilter = source.WorkSetSubject ?
                                        instance.SubjectWorkSet : null
                };

                if (!string.IsNullOrWhiteSpace(source.TagSubject))
                    query.MatchAllTags = EnumerableEx.OfOne(source.TagSubject).ToList();

                var data = await _tables.QueryDataTableAll(source.TableTemplate, query);
                if (data.RawData.Count < source.MinimumSamples)
                    throw new KPIInsufficientDataException(source.SourceName);
             
                if(!data.RawData.Any())// must have source data to compute KPI.
                {
                    throw new KPIInsufficientDataException();
                }
                
                var rawData = data.RawData;

                var interpolatedSamples = new List<double>();

                if (rawData.Count > 1) //BDG when approaching the end of the data set the raw data only has 1 record inside so we add this direct value in else
                {
                    // force data onto sample points using linear interpolation.

                    foreach (var itTime in sampleSpace.Points)
                    {
                        DateTime nextPoint = itTime.AddSeconds(sampleSpace.WalkSeconds);

                        var pairs = rawData.Zip(rawData.Skip(1));

                        // find the first data point that fits into the sample point
                        foreach (var (left, right) in pairs)
                        {
                            // sample time between left and right?
                            if (left.KeyDate <= itTime && right.KeyDate > itTime)
                            {
                                // get value and date interpolated between left and right
                                double leftValue = left.KeyNumeric!.Value;
                                double value = leftValue;
                                DateTime leftDate = left.KeyDate!.Value;
                                double rightValue = right.KeyNumeric!.Value;
                                DateTime rightDate = right.KeyDate!.Value;

                                var ts = (rightDate - leftDate).TotalSeconds; // total range between left and right
                                var placement = Math.Abs((itTime - leftDate).TotalSeconds); // placement in seconds from left

                                double diff = Math.Abs(rightValue - leftValue); // total range of values between left and right
                                if (diff > 0.001) // if we're sitting on the left value, just adopt left value
                                {
                                    var scale = placement / ts; // as a fraction, amount of placement between left and right
                                    value = leftValue + (scale * diff); // interpolate value
                                }

                                interpolatedSamples.Add(value);

                                break; // we found the placement for this sample, don't need to continue searching.
                            }
                        }
                    }

                    if (interpolatedSamples.Count < template.SampleCount) // off by one? add the last point
                        interpolatedSamples.Add(interpolatedSamples.Last());

                    interpolatedSamples.Count.Guarantees().Equals(template.SampleCount);
                }
                else
                {
                    interpolatedSamples.Add(rawData.First().KeyNumeric!.Value);
                }

                lock (env)
                    env.Add(source.SourceName, interpolatedSamples); // add this value to the current variables for later usage.

                return true;

            } 
            catch(KPIInsufficientDataException kpiEx)
            {
                _log.Log(LogLevel.Warning,new EventId(),kpiEx,"Could not comput KPI due to insufficient data. Error: " + kpiEx.Message);
                return false;
            }
            catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, LogLevel.Information,
                    ex.TraceInformation());
                throw;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scriptLines"></param>
    /// <returns></returns>
    private static string MakeScript(IList<string> scriptLines)
    {
        // glom script lines together into one script.
        // this way, the json that contains the script can look more readable.
        var sb = new StringBuilder();
        foreach(var line in scriptLines)
            sb.AppendLine(line);
        return sb.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="interpreter"></param>
    /// <param name="cs"></param>
    /// <param name="env"></param>
    private static void EvalKPICompute(
        ExpressionEvaluator interpreter,
        KPIComputeStage cs,
        Dictionary<string, object> env)
    {
        // Compute stages transform sources and previous compute stages.
        // Script is expected to use previously loaded / created variables
        // and return a result that will be saved into the given named variable.
        var script = MakeScript(cs.ScriptLines);
        interpreter.ScriptEvaluate(script);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="interpreter"></param>
    /// <param name="sampleSpace"></param>
    /// <param name="ss"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    private static KPISignal? EvalKPISignal(
        ExpressionEvaluator interpreter,
        SampleSpace sampleSpace,
        KPISignalStage ss,
        Dictionary<string, object> env)
    {
        var script = MakeScript(ss.ScriptLines);
        KPISignal? result = null!;

        // Prep a function delegate that creates a KPISignal.
        // Add to available environment for our signal eval script
        // to call.
        Action<int, List<double>> Signal =
            (itype, value) =>
            {
                var type = (KPISignalType)itype;

                if(type != KPISignalType.None)
                result = new KPISignal
                {
                    SignalId = ss.SignalId,
                    SignalName = ss.SignalName,
                    SignalValue = value.Last(),
                    SignalType = type,
                    Title = ss.Title,
                    SignalTime = sampleSpace.End
                };
            };

        env[nameof(Signal)] = Signal;

        // invoke the script.
        interpreter.ScriptEvaluate(script);
        

        // remove prepped delegate -- it's hard-wired to be relevant for this 
        // signal stage, subsequent stages get their own.
        env.Remove(nameof(Signal));

        return result;
    }

    /// <summary>
    /// Add standard functions to the environment
    /// </summary>
    /// <returns>A script environment ready for KPI evaluation.</returns>
    private Dictionary<string,object> BuildEnvironment()
    {
        Dictionary<string, object> env = new();
        env.Add("MAvg", 
                new Func<int, List<double>, List<double>>
                    ((period, input) => KPIMath.ComputeMovingAverage(period, input)));

        env.Add("ZScore", 
                new Func<int, List<double>, double, double, List<double>>
                    ((period, input, threshold, influence) => KPIMath.ComputeZScore(period, input, threshold, influence)));
                
        env.Add("WAdd", new Func<double, List<double>, double, List<double>, List<double>>
            ((wa,a,wb,b)=> KPIMath.WeightedAdd(wa,a,wb,b)));

        env.Add("WSum", new Func<double, List<double>, double, List<double>, double>
            ((wa,a,wb,b)=> KPIMath.WeightedSum(wa,a,wb,b)));

        env.Add("Scale", new Func<double, List<double>, List<double>>
            ((scale, input) => KPIMath.Rescale(scale, input)));

        env.Add("Map", new Func<double, double, double, double, double, bool, double>
            ((v, min1, max1, min2, max2, cl) => KPIMath.MapToRange(
                v, min1, max1, min2, max2, cl)));

        env.Add("Force", new Func<double, double, List<double>, List<double>>
            ((min, max, input) => KPIMath.ForceRange(min, max, input)));



        env.Add("Crossing",
                new Func<List<double>, List<double>, int>
                    ((main, supporting) => KPIMath.DetectTailCrossing(main, supporting)));
        env.Add("ZSignal",
                new Func<List<double>, bool, int>
                    ((input, detectOff) => KPIMath.DetectZScoreSignal(input, detectOff)));
        env.Add("Threshold",
                new Func<double,double,List<double>,int>
                ((min,max,input)=> KPIMath.DetectThreshold(min,max,input)));

        return env;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="template"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public async Task<KPIData?> ComputeKPI(
        KPITemplate template,
        KPIInstance instance,
        DateTime? endTime = null)
    {

        using (PerfTrack.Stopwatch(nameof(ComputeKPI)))
        {
            try
            {
                var interpreter = new ExpressionEvaluator(); // https://github.com/codingseb/ExpressionEvaluator/wiki/Variables-and-Functions
                interpreter.OptionVariablesPersistenceCustomComparer = true;
                var env = BuildEnvironment();
                interpreter.Variables = env;

                var sampleSpace = ComputeSamplePoints(template, endTime);

                var samples = new List<KPISample>();
                var signals = new List<KPISignal>();

                // query and sample source data.
                var work = new List<Task<bool>>();

                foreach (var source in template.Sources)
                    work.Add(QueryKPISource(sampleSpace, template, instance, source, env));

                await Task.WhenAll(work);

                if (work.Any(t => !t.Result)) // data collection failed
                    return null;

                // run each compute stage in sequence
                foreach (var cs in template.ComputeStages)
                {

                    EvalKPICompute(interpreter, cs, env);

                    if (cs.ComputeType != KPIComputeType.Intermediate)
                    {
                        // store kpi sample value for saving
                        samples.Add(new KPISample
                        {
                            Id = cs.SampleId,
                            Value = ((IList<double>) env[cs.ResultName]).Last()
                        });
                    }

                }

                // generate signals
                foreach (var ss in template.SignalStages)
                {
                    var signal = EvalKPISignal(interpreter, sampleSpace, ss, env);

                    if (signal is not null)
                        signals.Add(signal);
                }

                // describe KPI Data
                var kpi = new KPIData
                {
                    Id = Guid.NewGuid(),
                    KPIInstanceId = instance.Id,
                    KPITemplateName = template.Name,
                    Samples = samples,
                    Signals = signals,
                    SampleTime = sampleSpace.End
                };

                return kpi;

            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General, LogLevel.Information,
                    ex.TraceInformation());
                throw;
            }

        }


       
    }


}
