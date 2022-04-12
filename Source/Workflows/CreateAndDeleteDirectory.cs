namespace Workflows;

using DHI.Services.Jobs.Workflows;
using DHI.Workflow.Actions.Core;
using Microsoft.Extensions.Logging;

[Timeout("0:30:00")]
[WorkflowName("Awesome workflow for creating and deleting a directory")]
public class CreateAndDeleteDirectory : BaseCodeWorkflow
{
    [WorkflowParameter]
    public string FolderName { get; set; } = "C:\\Temp\\MyFolder";

    public CreateAndDeleteDirectory(ILogger logger)  : base(logger)
    {
    }

    public override void Run()
    {
        new CreateDirectory(Logger)
        {
            Directory = FolderName
        }.Run();

        new DeleteDirectory(Logger)
        {
            Directories = FolderName
        }.Run();
    }
}