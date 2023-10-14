using ACO;
using ACO.Algorithms.AV;
using ACO.Algorithms.DSR;
using ACO.Algorithms.EACSSA;
using ACO.DataLoading;
using ACO.Problems.CVRP.CVRP;
using ACO.Problems.CVRProblem;
using ACO.Shared;
using ACO.Shared.Logging;
using System.Diagnostics;

const string workingDirectory = "data";

var task = args[0];

if (task == "ini" || task == "both")
{
    RunIni();
    if (task != "both")
    {
        return;
    }
}
if (task != "reg" && task != "both")
{
    return;
}

var problem = args[1];
var iterations = int.Parse(args[2]);

if (problem is null)
    throw new NullReferenceException();

RunRegular();

Console.WriteLine("finished all");
Console.ReadKey();

void RunRegular()
{
    var inputFolderPath = Path.Combine(workingDirectory, "input", problem.ToUpper());
    var files = Directory.GetFiles(inputFolderPath);

    foreach (var file in files)
    {
        RunFromPath(file);
    }
}

void RunIni()
{
    var inputFolderPath = Path.Combine(workingDirectory, "input", "SOP");
    var files = Directory.GetFiles(inputFolderPath);

    MeasureEACSSAInitialization(files);
}

void RunFromPath(string inputPath)
{
    switch (problem)
    {
        case "cvrp":
            RunDSRACO(inputPath, iterations);
            break;

        case "tsp":
            RunACOAV(inputPath, iterations);
            break;

        case "sop":
            RunEACSSA(inputPath, iterations);
            break;
    };
}

void MeasureEACSSAInitialization(IEnumerable<string> instancePaths)
{
    var path = Path.Combine(workingDirectory, "output", "SOP");
    var logger = new Logger(path, "intialization_times.csv");
    Parallel.ForEach(instancePaths, instancePath =>
    {
        var data = ProblemDataLoader.LoadVRPD(instancePath);
        var problemParameters = new TSPParameters(data);
        var eacsParameters = new EACSParameters(data.Dimension);

        var stopwatch = Stopwatch.StartNew();
        _ = new EACS(problemParameters, eacsParameters);
        stopwatch.Stop();

        logger.Log(new LogDataBase(data.Name, stopwatch.Elapsed));
    });
}

void RunEACSSA(string instancePath, int iterations)
{
    var data = ProblemDataLoader.LoadVRPD(instancePath);
    var problemParameters = new TSPParameters(data);
    var eacsParameters = new EACSParameters(data.Dimension);
    var eacs = new EACS(problemParameters, eacsParameters);

    var logger = new Logger(data, Path.Combine(workingDirectory, "output", "SOP"));

    Run(() => eacs.Solve(), iterations, logger, data);
}

void RunACOAV(string instancePath, int iterations)
{
    var data = ProblemDataLoader.LoadXml(instancePath);
    var problemParameters = new TSPParameters(data);
    var dsracoParameters = new ACOAVParameters();
    var acoav = new ACOAV(problemParameters, dsracoParameters);

    var logger = new Logger(data, Path.Combine(workingDirectory, "output", "TSP"));

    Run(() => acoav.Solve(), iterations, logger, data);
}

void RunDSRACO(string instancePath, int iterations)
{
    var data = ProblemDataLoader.LoadVRPD(instancePath);
    var problemParameters = new CVRPParameters(data);
    var dsracoParameters = new DSRACOParameters()
    {
        PopulationSize = 10 * data.Dimension
    };
    var dsraco = new DSRACO(problemParameters, dsracoParameters);

    var logger = new Logger(data, Path.Combine(workingDirectory, "output", "CVRP"));

    Run(() => dsraco.Solve(), iterations, logger, data);
}

void Run(Func<Result> solveFunc, int iterations, Logger logger, ProblemData data)
{
    var loopWatch = Stopwatch.StartNew();

    ParallelOptions options = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount,
    };

    Parallel.For(0, iterations, options, i =>
    {
        var stopWatch = Stopwatch.StartNew();
        try
        {
            var result = solveFunc.Invoke();
            stopWatch.Stop();
            var error = (result.Length - data.Optimum) / (double)data.Optimum;
            var logData = LogDataFactory.GetLogData(stopWatch.Elapsed, data.Optimum, error, data.Name, result);
            logger.Log(logData);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    });

    loopWatch.Stop();

    Console.WriteLine($"Finished. Time elapsed: {loopWatch.Elapsed.TotalSeconds} s");
}