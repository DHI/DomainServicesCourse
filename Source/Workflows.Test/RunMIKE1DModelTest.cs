namespace Workflows.Test;

using System;
using Xunit;

[TestCaseOrderer("Workflows.Test.AlphabeticalOrderer", "Workflows.Test")]
public class RunMIKE1DModelTest : IClassFixture<MIKE1DModelTestFixture>
{
    private string _root;

    public RunMIKE1DModelTest(MIKE1DModelTestFixture fixture)
    {
        _root = fixture.Root;
    }

    [Fact]
    public void RunMIKE1DModelIsOk()
    {
        _root = @"C:\Work\DHIGitHub\DomainServicesCourse\Models\MIKE1D";

        var logger = new FakeLogger();
        var workflowFirst = new RunMIKE1DModel(logger)
        {
            Root = _root,
            StartTime = new(1990, 9, 1, 0, 0, 0),
            EndTime = new(1990, 9, 3, 0, 0, 0),
            DischargeScale = 1.4
        };
        workflowFirst.Run();

        Assert.Contains(logger.Lines, s => s.Contains("No history folder found"));
        Assert.Contains(logger.Lines, s => s.Contains("Executing model..."));
        Assert.Contains(logger.Lines, s => s.Contains("Copying current folder to"));

        logger = new FakeLogger();
        var workflowSecond = new RunMIKE1DModel(logger)
        {
            Root = _root,
            StartTime = new(1990, 9, 2, 0, 0, 0),
            EndTime = new(1990, 9, 4, 0, 0, 0),
            DischargeScale = 2.1
        };
        workflowSecond.Run();

        Assert.Contains(logger.Lines, s => s.Contains("History folder is"));
        Assert.Contains(logger.Lines, s => s.Contains("Executing model..."));
        Assert.Contains(logger.Lines, s => s.Contains("Copying current folder to"));

        logger = new FakeLogger();
        var workflowThird = new RunMIKE1DModel(logger)
        {
            Root = _root,
            StartTime = new(1990, 9, 3, 0, 0, 0),
            EndTime = new(1990, 9, 5, 0, 0, 0),
            DischargeScale = 2.6
        };
        workflowThird.Run();

        Assert.Contains(logger.Lines, s => s.Contains("History folder is"));
        Assert.Contains(logger.Lines, s => s.Contains("Executing model..."));
        Assert.Contains(logger.Lines, s => s.Contains("Copying current folder to"));
    }
}