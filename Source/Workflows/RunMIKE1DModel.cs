namespace Workflows;

using DHI.Services.Provider.OpenXML;
using DHI.Workflow.Actions.Timeseries;
using DHI.Services.Jobs.Workflows;
using DHI.Workflow.Actions.Core;
using DHI.Workflow.Actions.Models;
using Microsoft.Extensions.Logging;

[Timeout("0:30:00")]
[WorkflowName("Run MIKE 1D model (Vida)")]
public class RunMIKE1DModel : BaseCodeWorkflow
{
    public RunMIKE1DModel(ILogger logger) : base(logger)
    {
    }

    [WorkflowParameter]
    public DateTime StartTime { get; set; } = new(1990, 9, 1, 0, 0, 0);

    [WorkflowParameter]
    public DateTime EndTime { get; set; } = new(1990, 9, 3, 0, 0, 0);

    [WorkflowParameter]
    public double DischargeScale { get; set; } = 1;

    [WorkflowParameter]
    public string? Root { get; set; } = @"C:\Work\DHIGitHub\DomainServicesCourse\Models\MIKE1D";

    public override void Run()
    {
        new ReportProgress(Logger)
        {
            Progress = 0,
            ProgressMessage = "Initializing model..."
        }.Run();

        // Prepares the model, sets times, copies hotstart files
        new InitializeModel(Logger)
        {
            Folder = Root,
            Hotstart = true,
            HotstartElements = new List<string> { "Hotstart.res1d" },
            ResultElements = new List<string> { @"Vida_m1d - Result Files\Vida_1BaseDefault_Network_HD.res1d" },
            ModelTypes = new List<string> { "MIKE1D" },
            SimulationFileNames = new List<string> { "Vida.m1dx" },
            StartTimes = new List<DateTime> { StartTime },
            EndTimes = new List<DateTime> { EndTime },
        }.Run();

        // Scales dfs0 boundary condition
        new BuildTimeseries(Logger)
        {
            AddMode = BuildTimeseries.AddModeType.DeleteOverlappingValues,
            SpreadsheetRepository = new SpreadsheetRepository(Root),
            SpreadsheetId = "BuildTimeseries.xlsx",
            SheetIds = new List<string> { "MIKE1D" },
            Replacements = $"[root]={Root}&[dischargeScale]={DischargeScale}"
        }.Run();
        
        // .. OR TransferTimeSeries could have been used here to transfer time series from
        // e.g. MIKE OPERATIONS or MIKE Cloud

        // ModifyModelFiles could be used to do really odd stuff to the model input files

        new ReportProgress(Logger)
        {
            Progress = 10,
            ProgressMessage = @"Executing model..."
        }.Run();

        // Model is run
        // I found that for running the MIKE1D model to have a succesfull license check,
        // I needed to change the service to run as something more than Local System,
        // so it runs in my profile. You can change that in Services, which you then has
        // to Run as Administrator
        var runModel = new RunModel(Logger)
        {
            ContinueOnError = false,
            SimulationFileName = Path.Combine(Root!, @"Current\Vida.m1dx")
        };
        runModel.Run();

        if (runModel.IsSuccess)
        {
            new ReportProgress(Logger)
            {
                Progress = 90,
                ProgressMessage = @"Finalizing model..."
            }.Run();

            // Time series are extracted
            var Initials = "YourInitials";
            new TransferTimeseries(Logger)
            {
                AddMode = TransferTimeseries.AddModeType.DeleteOverlappingValues,
                SpreadsheetRepository = new SpreadsheetRepository(Root),
                SpreadsheetId = "TransferTimeSeries.xlsx",
                SheetId = "MIKE1D",
                Replacements = $"[root]={Root}&[id]={Initials}"
            }.Run();

            // Model is archived in history folder for next run
            new FinalizeModel(Logger)
            {
                EndTime = EndTime,
                Folder = Root,
                Keep = 40,
                StartTime = StartTime,
                Success = true
            }.Run();

            new ReportProgress(Logger)
            {
                Progress = 95,
                ProgressMessage = @"Processing result time series..."
            }.Run();

            // ValidateTimeseries could be used to analyze the resulting time series e.g.
            // for threshold violations

            // SampleTimeseries could be used to extract values to assess forecast
            // performance

            // AlertTimeseries could be used to produce and send reports based on trigger
            // levels for time series

            new ReportProgress(Logger)
            {
                Progress = 100,
                ProgressMessage = @"Workflow completed."
            }.Run();
        }
        else
        {
            new ReportProgress(Logger)
            {
                Progress = 100,
                ProgressMessage = "Model execution failed."
            }.Run();

            throw new Exception("Model execution failed.");
        }
    }
}