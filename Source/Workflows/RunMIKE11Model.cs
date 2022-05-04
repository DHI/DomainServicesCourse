namespace Workflows;

using DHI.Services.Provider.OpenXML;
using DHI.Workflow.Actions.Timeseries;
using DHI.Services.Jobs.Workflows;
using DHI.Workflow.Actions.Core;
using DHI.Workflow.Actions.Models;
using Microsoft.Extensions.Logging;

public class RunMIKE11Model : BaseCodeWorkflow
{
    public RunMIKE11Model(ILogger logger) : base(logger)
    {
    }

    [WorkflowParameter]
    public DateTime StartTime { get; set; } = new(1989, 8, 1, 12, 0, 0);

    [WorkflowParameter]
    public DateTime EndTime { get; set; } = new(1989, 8, 2, 12, 0, 0);

    [WorkflowParameter]
    public string? Root { get; set; }

    public override void Run()
    {
        new ReportProgress(Logger)
        {
            Progress = 0,
            ProgressMessage = "Initializing model..."
        }.Run();

        new InitializeModel(Logger)
        {
            EndTimes = new List<DateTime> { EndTime },
            Folder = Root,
            Hotstart = true,
            HotstartElements = new List<string> { "CALI-HD_HOT.RES11" },
            ModelTypes = new List<string> { "MIKE11" },
            ResultElements = new List<string> { "CALI-HD.res11" },
            SimulationFileNames = new List<string> { "cali.sim11" },
            StartTimes = new List<DateTime> { StartTime }
        }.Run();

        // TODO: BuildTimeSeries

        // TODO: TransferTimeSeries

        new ReportProgress(Logger)
        {
            Progress = 10,
            ProgressMessage = @"Executing model..."
        }.Run();

        var runModel = new RunModel(Logger)
        {
            ContinueOnError = false,
            SimulationFileName = Path.Combine(Root!, @"Current\cali.sim11")
        };
        runModel.Run();

        if (runModel.IsSuccess)
        {
            new ReportProgress(Logger)
            {
                Progress = 90,
                ProgressMessage = @"Finalizing model..."
            }.Run();

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
                ProgressMessage = @"Transferring result time series..."
            }.Run();

            new TransferTimeseries(Logger)
            {
                AddMode = TransferTimeseries.AddModeType.DeleteOverlappingValues,
                SpreadsheetRepository = new SpreadsheetRepository(Root),
                SpreadsheetId = "TransferTimeSeries.xlsx",
                SheetId = "MIKE11",
                Replacements = "[id]=test"
            }.Run();

            // TODO: ValidateTimeSeries

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