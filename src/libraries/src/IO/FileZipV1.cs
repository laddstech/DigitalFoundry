using Flowly.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry
{
    public class FileZipOptions
    {
        public string SearchPattern { get; set; } = "*_fullsize.jpeg";
        public int MaxArchiveSize { get; set; } = 20 * 1024 * 1024;
    }

    public class FileZipV1 : WorkflowStep<FileZipOptions>
    {
        public override ValueTask ExecuteAsync()
        {
            var basePath = Context.WorkingDirectory;

            if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                throw new ArgumentException(nameof(basePath));

            if (Directory.GetFiles(basePath, "*.zip").Any())
                return new ValueTask();

            var directory = basePath;

            var files = Directory.GetFiles(directory, Options.SearchPattern, SearchOption.AllDirectories).ToList();

            int archiveIndex = 1;

            while (files.Count > 0)
            {
                var sourceFilePath = files.First();
                var sourceFileName = Path.GetFileName(sourceFilePath);

                var sourceFileLength = new FileInfo(sourceFilePath).Length;
                var archiveFileLength = File.Exists(Path.Combine(directory, $"output_{archiveIndex}.zip")) ? new FileInfo(Path.Combine(directory, $"output_{archiveIndex}.zip")).Length : 0;

                if (archiveFileLength + sourceFileLength > Options.MaxArchiveSize)
                    archiveIndex++;

                var archivePath = Path.Combine(directory, $"output_{archiveIndex}.zip");
                using var currentArchive = ZipFile.Open(archivePath, ZipArchiveMode.Update);

                currentArchive.CreateEntryFromFile(sourceFilePath, sourceFileName, CompressionLevel.Optimal);
                files.RemoveAt(0);
            }

            return new ValueTask();
        }
    }
}
