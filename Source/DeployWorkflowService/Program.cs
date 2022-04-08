namespace DeployWorkflowService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    internal class Program
    {
        private static void Main(string[] args)
        {
            // Define roots
            var sourceFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var destinationFolder = $"{sourceFolder}\\..\\Deployment";

            // Find folders and files
            var folders = new List<string> { sourceFolder };
            folders.AddRange(Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories));
            var allFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);

            // Build list of files to exclude
#warning Modify list of files to exclude if necessary
            var excludedFiles = Directory.GetFiles(sourceFolder, "*.xml").Where(path => !new List<string> { "eum.xml" }.Contains(Path.GetFileName(path).ToLower())).ToList();
            var exeFile = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
            excludedFiles.AddRange(Directory.GetFiles(sourceFolder, $"{exeFile}.*"));
            var files = allFiles.Except(excludedFiles);

            // Create folders unless they already exist. Make sure they are empty
            foreach (var folder in folders)
            {
                var directoryInfo = Directory.CreateDirectory(folder.Replace(sourceFolder, destinationFolder));
                foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                {
                    fileInfo.Delete();
                }
            }

            // Copy files
            foreach (var file in files)
            {
                File.Copy(file, file.Replace(sourceFolder, destinationFolder), true);
            }

            // Open destination folder
            if (args.Length == 0)
            {
                Console.WriteLine($"Files copied to \'{destinationFolder}\'");
                Console.WriteLine("Open destination folder? y/n");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    var processStartInfo = new ProcessStartInfo { FileName = destinationFolder, UseShellExecute = true };
                    Process.Start(processStartInfo);
                }
            }
        }
    }
}