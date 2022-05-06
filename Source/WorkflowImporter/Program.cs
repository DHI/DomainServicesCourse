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
            var fileName = $"{assembly.GetName().Name}.json";
            var workflowRepository = new CodeWorkflowRepository(fileName);
            var workflows = new CodeWorkflowService(workflowRepository);
            workflows.ImportFrom(assembly, true);
            Console.WriteLine($"Workflows from assembly '{assembly.GetName().Name}' imported into {fileName}.");
            Console.WriteLine("This file must be copied into the App_Data folder of the Web API.");

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