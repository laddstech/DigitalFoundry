using Flowly.Core;
using ImageMagick;
using LaddsTech.DigitalFoundry.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry
{
    public class ImageListingPrimaryOptions
    {
        public int ImageWidth { get; set; } = 2700;
        public int ImageHeight { get; set; } = 2050;
        public string? OverlayImagePath { get; set; }
        public string? LabelFont { get; set; }
        public int LabelFontWeight { get; set; } = 500;
        public int LabelFontHeight { get; set; } = 125;
        public string LabelColor { get; set; } = "#000000";
        public int LabelGravity { get; set; } = (int)Gravity.Center;
        public int LabelX { get; set; }
        public int LabelY { get; set; }
        public int MontageX { get; set; } = 10;
        public int MontageY { get; set; } = 1;
    }


    public class ImageListingPrimaryV1 : WorkflowStep<ImageListingPrimaryOptions>
    {
        public override async ValueTask ExecuteAsync()
        {
            var basePath = Context.WorkingDirectory;

            if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                throw new ArgumentException(nameof(basePath));

            var previewOutput = Path.Combine(basePath, "preview");
            var images = new List<string>(Directory.GetFiles(previewOutput, "*.jpeg"));

            var collection = new MagickImageCollection();
            foreach (var image in images)
            {
                collection.Add(image);
            }

            var _montageSettings = new MontageSettings()
            {
                BackgroundColor = MagickColors.White,
                Shadow = false,
                Geometry = new MagickGeometry
                {
                    Width = 100,
                    Height = 100,
                    IsPercentage = true,
                    X = 0
                },
                TileGeometry = new MagickGeometry(0, 0, Options.MontageX, Options.MontageY),

            };

            float division = 100f / (collection.Count / Options.MontageY * 1f);

            foreach (var image in collection)
            {
                image.Crop(new MagickGeometry(new Percentage(division), new Percentage(100)));
            }

            var result = (IMagickImage)collection.Montage(_montageSettings);


            result.Resize(new MagickGeometry
            {
                Width = Options.ImageWidth
            });
            result.Crop(new MagickGeometry { Width = Options.ImageWidth, Height = Options.ImageHeight }, Gravity.Center);


            if (!string.IsNullOrEmpty(Options.OverlayImagePath))
            {
                var overlay = new MagickImage(Options.OverlayImagePath);
                overlay.Resize(new MagickGeometry
                {
                    Width = result.Width
                });
                result.Composite(overlay, Gravity.Center, CompositeOperator.Over);
            }

            var listingMetadata = Variables.GetValue<ListingMetadata>(ListingMetadata.VariableKey);
            if (!string.IsNullOrEmpty(listingMetadata?.ShortTitle))
            {
                var readSettings = new MagickReadSettings
                {
                    FillColor = new MagickColor(Options.LabelColor),
                    BackgroundColor = MagickColors.Transparent,
                    TextGravity = Gravity.West,
                    Font = Options.LabelFont,
                    FontWeight = (FontWeight)Options.LabelFontWeight,
                    Height = Options.LabelFontHeight
                };

                var labelText = listingMetadata.ShortTitle;
                var labelGravity = (Gravity)Options.LabelGravity;
                if (!string.IsNullOrEmpty(labelText))
                {
                    var countText = new MagickImage($"label:{labelText}", readSettings);
                    result.Composite(countText, labelGravity, Options.LabelX, Options.LabelY, CompositeOperator.Over);
                }
            }

            result.Quality = 100;
            await result.WriteAsync(Path.Combine(basePath, $"etsy_product_listing_1.jpeg"));

            await Task.Delay(2500);
        }
    }
}
