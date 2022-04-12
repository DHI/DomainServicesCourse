namespace Workflows.Test;

using System;
using System.IO;
using Xunit;

public class WorkflowsTest
{
    [Fact]
    public void CreateAndDeleteDirectoryIsOk()
    {
        var logger = new FakeLogger();
        var folderName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var workflow = new CreateAndDeleteDirectory(logger)
        {
            FolderName = folderName
        };
        workflow.Run();

        Assert.Contains($"Folder '{folderName}' has been created", logger.Lines);
        Assert.False(Directory.Exists(folderName));
    }
}