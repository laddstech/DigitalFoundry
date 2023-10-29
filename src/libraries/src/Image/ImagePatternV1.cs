using Flowly.Core;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry
{

    public class ImagePatternOptions
    {
        public string SourceFilePattern { get; set; } = "*.png";
        public bool PreProcess { get; set; } = true;
        public int OutputResolution { get; set; } = 3600;
        public int OutputQuality { get; set; } = 100;
        public int PreviewResolution { get; set; } = 1024;
        public int PreviewQuality { get; set; } = 75;
    }

    public class ImagePatternV1 : WorkflowStep<ImagePatternOptions>
    {
        public override async ValueTask ExecuteAsync()
        {
            var basePath = Context.WorkingDirectory;

            if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                throw new ArgumentException(nameof(basePath));

            int index = 1;

            var tasks = new List<Task>();

            foreach (var file in Directory.GetFiles(basePath, Options.SourceFilePattern))
            {
                tasks.Add(Task.Run(() => ProcessImage(file, index++)));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(2500);
        }

        private async Task ProcessImage(string path, int index)
        {
            var output = Path.Combine(Path.GetDirectoryName(path), "processed");
            var previewOutput = Path.Combine(Path.GetDirectoryName(path), "preview");

            Directory.CreateDirectory(output);
            Directory.CreateDirectory(previewOutput);


            using var inputImage = new MagickImage(path);
            inputImage.Quality = 100;

            if (Options.PreProcess)
            {
                inputImage.FilterType = FilterType.Hermite;
                inputImage.AdaptiveSharpen(0.0, 1.0);
            }
            
            inputImage.Resize(new Percentage(200.0));

            using var images = new MagickImageCollection();
            for (int i = 0; i < 9; i++)
                images.Add(new MagickImage(inputImage));

            var _montageSettings = new MontageSettings()
            {
                BackgroundColor = MagickColors.None,
                Shadow = false,
                Geometry = new MagickGeometry(0, 0, new Percentage(100.0), new Percentage(100.0)),
                TileGeometry = new MagickGeometry(0, 0, 3, 3),
            };

            var result = (IMagickImage)images.Montage(_montageSettings);
            var image = result;

            image.Format = MagickFormat.Jpeg;
            image.FilterType = FilterType.Hermite;
            image.Density = new Density(300, DensityUnit.PixelsPerInch);
            image.Resize(Options.OutputResolution, Options.OutputResolution);

            image.ColorSpace = ColorSpace.sRGB;
            image.SetProfile(ColorProfile.SRGB);
            image.Quality = Options.OutputQuality;
            await image.WriteAsync(Path.Combine(output, $"image_{index:000}_fullsize.jpeg"));

            image.Resize(Options.PreviewResolution, Options.PreviewResolution);
            image.Quality = Options.PreviewQuality;

            await image.WriteAsync(Path.Combine(previewOutput, $"image_{index:000}_preview.jpeg"));
        }


    }
}
