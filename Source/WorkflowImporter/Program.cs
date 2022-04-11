namespace WorkflowImporter;

using DHI.Services.Jobs.Workflows;
using System;
using System.Reflection;
using DHI.Services.Logging;

internal class Program
{
    private static int Main()
    {
        var logger = new SimpleLogger("log.json");
        try
        {
            var assembly = Assembly.LoadFrom("Workflows.dll");
            var workflowRepository = new CodeWorkflowRepository(assembly.GetName().Name + ".json");
            var workflows = new CodeWorkflowService(workflowRepository);
            workflows.ImportFrom(assembly, true);

            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Log(new LogEntry(LogLevel.Error, e.Message, nameof(WorkflowImporter)));
            return 1;
        }
    }
}