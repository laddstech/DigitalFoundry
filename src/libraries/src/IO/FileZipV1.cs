using Flowly.Core;
using System;
using System.Collections.Generic;
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

            var fileSizes = new Dictionary<string, long>();
            var archives = new List<List<string>>();


            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                fileSizes.Add(file, fileInfo.Length);
            }

            fileSizes = fileSizes.OrderByDescending(_ => _.Value)
                .ToDictionary(_ => _.Key, _ => _.Value);

            NextFit(fileSizes, archives);

            int archiveIndex = 1;
            foreach ( var archive in archives)
            {
                var archivePath = Path.Combine(directory, $"output_{archiveIndex++}.zip");
                using var currentArchive = ZipFile.Open(archivePath, ZipArchiveMode.Update);

                foreach ( var file in archive)
                {
                    var sourceFileName = Path.GetFileName(file);
                    currentArchive.CreateEntryFromFile(file, sourceFileName, CompressionLevel.Optimal);
                }
            }

            return new ValueTask();
        }

        private int NextFit(Dictionary<string, long> files, List<List<string>> archives)
        {
            archives.Clear();

            long capacity = Options.MaxArchiveSize;
            var currentArchive = new List<string>();
            foreach(var file in files) 
            {
                if (file.Value > capacity)
                {
                    capacity = Options.MaxArchiveSize - file.Value;
                    currentArchive = new List<string>();
                    currentArchive.Add(file.Key);
                    archives.Add(currentArchive);
                } 
                else
                {
                    currentArchive.Add(file.Key);   
                    capacity -= file.Value;
                }
            }

            if (currentArchive.Any())
                archives.Add(currentArchive);

            return archives.Count;
        }
    }
}
