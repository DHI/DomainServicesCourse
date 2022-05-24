namespace Workflows.Test;

using System;
using System.Collections.Generic;
using System.IO;
using DHI.Services.Provider.OpenXML;
using DHI.Services.Spreadsheets;

public class MIKE11ModelTestFixture : IDisposable
{
    public MIKE11ModelTestFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var masterFolder = Path.Combine(Root, "Master");
        Directory.CreateDirectory(masterFolder);
        foreach (var file in Directory.GetFiles("..\\..\\..\\..\\Models\\MIKE11\\Master"))
            File.Copy(file, Path.Combine(masterFolder, Path.GetFileName(file)));

        File.Copy("..\\..\\TransferTimeSeriesTemplate.xlsx", Path.Combine(Root, "TransferTimeSeriesTemplate.xlsx"));

        // Modify spreadsheet source path to actual path (Root)
        var spreadsheetService = new SpreadsheetService(new SpreadsheetRepository(Root));
        const string sheetName = "MIKE11";
        var templateSheet = spreadsheetService.GetUsedRange("TransferTimeSeriesTemplate.xlsx", sheetName);
        var sheet = GetResolvedSheet(templateSheet);
        var spreadsheet = new Spreadsheet<string>("TransferTimeSeries.xlsx", "TransferTimeSeries.xlsx", null)
        {
            Metadata = {["SheetNames"] = new List<string> {sheetName}}
        };
        spreadsheet.Data.Add(sheet);
        spreadsheetService.Add(spreadsheet);
    }

    public string Root { get; }

    public void Dispose()
    {
        Directory.Delete(Root, true);
    }

    private object[,] GetResolvedSheet(object[,] templateSheet)
    {
        var rowCount = templateSheet.GetLength(0);
        var colCount = templateSheet.GetLength(1);
        var sheet = new object[rowCount, colCount];
        for (var row = 0; row < rowCount; row++)
        for (var col = 0; col < colCount; col++)
            if (templateSheet[row, col] is string s)
                sheet[row, col] = s.Replace("[root]", Root);
            else
                sheet[row, col] = templateSheet[row, col];

        return sheet;
    }
}