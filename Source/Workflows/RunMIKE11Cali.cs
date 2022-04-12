namespace Workflows;

using DHI.Services.Jobs.Workflows;
using DHI.Workflow.Actions.Core;
using DHI.Workflow.Actions.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

public class RunMIKE11Cali : BaseCodeWorkflow
{
    [WorkflowParameter] 
    public DateTime StartTime { get; set; } = new(1989, 8, 1);

    [WorkflowParameter]
    public DateTime EndTime { get; set; } = new(1989, 8, 2);

    [WorkflowParameter]
    public string Root { get; set; } = @"c:\Temp\Workflows\RunModelMIKE11\";

    public RunMIKE11Cali(ILogger logger) : base(logger)
    {
    }

    public override void Run()
    {
        new DeleteDirectory(Logger)
        {
            Directories = Root + @"Models\MIKE11\Current" + ";" + 
                          Root + @"Models\MIKE11\History",
            KillAnyUsingProcess = false

        }.Run();

        new InitializeModel(Logger)
        {
            EndTimes = new List<DateTime> { EndTime },
            Folder = Root + @"Models\MIKE11",
            Hotstart = true,
            HotstartElements = new List<string> { "CALI-HD_HOT.RES11" },
            ModelTypes = new List<string> { "MIKE11" },
            ResultElements = new List<string> { "CALI-HD.res11" },
            SimulationFileNames = new List<string> { "cali.sim11" },
            StartTimes = new List<DateTime> { StartTime },
        }
        .Run();

        new RunModel(Logger)
        {
            ContinueOnError = false,
            MpiExecProcesses = 16,
            MpiExecUse = false,
            MpiMode = MpiMode.Fixed,
            SimulationFileName = Root + @"Models\MIKE11\Current\cali.sim11"
        }
        .Run();

        new FinalizeModel(Logger)
        {
            EndTime = EndTime,
            Folder = Root + @"Models\MIKE11",
            Keep = 40,
            StartTime = StartTime,
            Success = true
        }
        .Run();
        
    }
}