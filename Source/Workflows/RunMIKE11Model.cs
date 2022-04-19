namespace Workflows;

using System.ComponentModel.DataAnnotations;
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
    [Required]
    public string? Root { get; set; }

    public override void Run()
    {
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

        // Build TimeSeries

        // Transfer TimeSeries

        new RunModel(Logger)
        {
            ContinueOnError = false,
            SimulationFileName = Path.Combine(Root!, @"Current\cali.sim11")
        }.Run();

        //new TransferTimeseries(Logger)
        //{
        //    AddMode = TransferTimeseries.AddModeType.DeleteOverlappingValues,
        //    RepositoryType = 
        //}

        new FinalizeModel(Logger)
        {
            EndTime = EndTime,
            Folder = Root,
            Keep = 40,
            StartTime = StartTime,
            Success = true
        }.Run();
    }
}