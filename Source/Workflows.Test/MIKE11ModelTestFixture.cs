namespace Workflows.Test;

using System.IO;
using System;

public class MIKE11ModelTestFixture : IDisposable
{

    public MIKE11ModelTestFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var masterFolder = Path.Combine(Root, "Master");
        Directory.CreateDirectory(masterFolder);
        foreach (var file in Directory.GetFiles("..\\..\\..\\Data\\MIKE11"))
        {
            File.Copy(file, Path.Combine(masterFolder, Path.GetFileName(file)));
        }
    }

    public string Root { get; }

    public void Dispose()
    {
        Directory.Delete(Root, true);
    }
}