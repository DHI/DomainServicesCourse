namespace Workflows.Test;

using System.Collections.Generic;
using DHI.Services.Provider.OpenXML;
using DHI.Services.Spreadsheets;
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

        File.Copy("..\\..\\..\\Data\\TransferTimeSeriesTemplate.xlsx", Path.Combine(Root, "TransferTimeSeriesTemplate.xlsx"));

        var spreadsheetService = new SpreadsheetService(new SpreadsheetRepository(Root));

        const string sheetName = "MIKE11";
        var templateSheet = spreadsheetService.GetUsedRange("TransferTimeSeriesTemplate.xlsx", "MIKE11");
        var rowCount = templateSheet.GetLength(0);
        var colCount = templateSheet.GetLength(1);
        var sheet = new object[rowCount, colCount];
        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {
                if (templateSheet[row, col] is string s)
                {
                    sheet[row, col] = s.Replace("[root]", Root);
                }
                else
                {
                    sheet[row, col] = templateSheet[row, col];
                }
            }
        }

        var spreadsheet = new Spreadsheet<string>("TransferTimeSeries.xlsx", "TransferTimeSeries.xlsx", null);
        spreadsheet.Metadata["SheetNames"] = new List<string> { sheetName };
        spreadsheet.Data.Add(sheet);
        spreadsheetService.Add(spreadsheet);

    }

    public string Root { get; }

    public void Dispose()
    {
        Directory.Delete(Root, true);
    }
}