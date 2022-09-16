namespace Workflows.Test;

using System;
using System.IO;

public class MIKE1DModelTestFixture : IDisposable
{
    public MIKE1DModelTestFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var masterFolder = Path.Combine(Root, "Master");
        Directory.CreateDirectory(masterFolder);
        foreach (var file in Directory.GetFiles("..\\..\\..\\..\\..\\..\\Models\\MIKE1D\\Master")) 
        {
            File.Copy(file, Path.Combine(masterFolder, Path.GetFileName(file)));
        }

        File.Copy("..\\..\\..\\..\\..\\..\\Models\\MIKE1D\\TransferTimeSeries.xlsx", Path.Combine(Root, "TransferTimeSeries.xlsx"));
        File.Copy("..\\..\\..\\..\\..\\..\\Models\\MIKE1D\\BuildTimeSeries.xlsx", Path.Combine(Root, "BuildTimeSeries.xlsx"));
    }

    public string Root { get; }

    public void Dispose()
    {
        Directory.Delete(Root, true);
    }
}