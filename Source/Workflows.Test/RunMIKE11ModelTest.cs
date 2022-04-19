namespace Workflows.Test;

using System;
using Xunit;

[TestCaseOrderer("Workflows.Test.AlphabeticalOrderer", "Workflows.Test")]
public class RunMIKE11ModelTest : IClassFixture<MIKE11ModelTestFixture>
{
    private readonly string _root;

    public RunMIKE11ModelTest(MIKE11ModelTestFixture fixture)
    {
        _root = fixture.Root;
    }

    [Fact]
    public void RunMIKE11Model_1_IsOk()
    {
        var logger = new FakeLogger();
        var workflow = new RunMIKE11Model(logger)
        {
            Root = _root
        };
        workflow.Run();

        Assert.Contains(logger.Lines, s => s.Contains("No history folder found"));
        Assert.Contains(logger.Lines, s => s.Contains("Simulation Started"));
        Assert.Contains(logger.Lines, s => s.Contains("End running successfully"));
    }

    [Fact]
    public void RunMIKE11Model_2_IsOk()
    {
        var logger = new FakeLogger();
        var workflow = new RunMIKE11Model(logger)
        {
            Root = _root,
            StartTime = new DateTime(1989, 8, 1, 13, 0, 0),
            EndTime = new DateTime(1989, 8, 2, 13, 0, 0)
        };
        workflow.Run();

        Assert.Contains(logger.Lines, s => s.Contains("History folder is"));
        Assert.Contains(logger.Lines, s => s.Contains("MIKE 11 HotstartTime for hd"));
        Assert.Contains(logger.Lines, s => s.Contains("Simulation Started"));
        Assert.Contains(logger.Lines, s => s.Contains("End running successfully"));
    }
}