using Flowly.Core;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry
{
    public class ImageListingPreviewOptions
    {
        public int ImageWidth { get; set; } = 2700;
        public int ImageHeight { get; set; } = 2050;
    }


    public class ImageListingPreviewV1 : WorkflowStep<ImageListingPreviewOptions>
    {
        protected virtual Dictionary<int, int> Configurations { get; } = new Dictionary<int, int>
        {
            { 16, 3 },
            { 13, 3 },
            { 12, 4 },
            { 10, 3 },
            { 8, 4 },
            { 7, 3 },
            { 4, 4 }
        };

        protected virtual List<PreviewSettings> PreviewSettings { get; } = new List<PreviewSettings>
        {
            new PreviewSettings
            {
                X = -1024,
                Y = 1000,
                Rotation = -32
            },
            new PreviewSettings
            {
                X = 1024,
                Y = 1000,
                Rotation = -65
            },
            new PreviewSettings
            {
                X = -30,
                Y = -1500,
                Rotation = 37
            },
        };

        public override async ValueTask ExecuteAsync()
        {
            var basePath = Context.WorkingDirectory;

            if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                throw new ArgumentException(nameof(basePath));

            var previewOutput = Path.Combine(basePath, "preview");
            var images = new List<string>(Directory.GetFiles(previewOutput, "*.jpeg"));

            int totalImages = images.Count;
            int previewIndex = 0;

            var tasks = new List<Task>();

            while (totalImages > 0)
            {
                var numImages = Configurations.ContainsKey(totalImages) ? Configurations[totalImages] : 1;
                var previewImages = images.Take(numImages);

                var collection = new MagickImageCollection();
                foreach (var image in previewImages)
                {
                    collection.Add(image);
                }

                tasks.Add(Task.Run(async () =>
                {
                    var result = await CreatePreviewImageAsync(collection);

                    result.Quality = 100;
                    await result.WriteAsync(Path.Combine(basePath, $"etsy_product_listing_image_{++previewIndex}.jpeg"));
                }));

                images.RemoveRange(0, numImages);
                totalImages -= numImages;
            }

            await Task.WhenAll(tasks);
            await Task.Delay(2500);
        }

        protected virtual Task<IMagickImage> CreatePreviewImageAsync(MagickImageCollection collection)
        {
            IMagickImage image = new MagickImage(MagickColors.None, Options.ImageWidth, Options.ImageHeight);
            var first = collection[0];
            first.Resize(new MagickGeometry()
            {
                FillArea = true,
                Width = Options.ImageWidth,
                Height = Options.ImageHeight
            });
            image.Composite(first, CompositeOperator.Copy);

            for (int i = 1; i < collection.Count; i++)
            {
                var previewSettings = PreviewSettings[Math.Min(i - 1, PreviewSettings.Count - 1)];
                var second = collection[i];
                second.BackgroundColor = MagickColors.Transparent;

                var shadow = second.Clone();
                shadow.Shadow(0, 0, 25, new Percentage(50), MagickColors.Black);
                shadow.Composite(second, CompositeOperator.Over);
                shadow.Rotate(previewSettings.Rotation);
                image.Composite(shadow, Gravity.Center, previewSettings.X, previewSettings.Y, CompositeOperator.Over);
            }

            return Task.FromResult(image);
        }
    }

    public struct PreviewSettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double Rotation { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
