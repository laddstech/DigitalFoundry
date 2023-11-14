using Flowly.Core;
using ImageMagick;
using System;
using System.IO;
using System.Threading.Tasks;
using static LaddsTech.DigitalFoundry.ImageComposeOptions;

namespace LaddsTech.DigitalFoundry
{
    public class ImageComposeOptions
    {
        public int ImageWidth { get; set; } = 2048;
        public int ImageHeight { get; set; } = 2048;

        public string OutputPath { get; set; } = "test.png";
        public Layer[] Layers { get; set; } = Array.Empty<Layer>();

        public class Layer
        {
            public string? LayerImagePath { get; set; }
            public string? LayerAlphaMaskPath { get; set; }
            public double? Rotation { get; set; }
            public double? Scale { get; set; }
            public int OffsetX { get; set; } = 0;
            public int OffsetY { get; set; } = 0;
            public double[]? Distort { get; set; }

           public Label? Label { get; set; }

            public CompositeOperator Operator { get; set; } = CompositeOperator.Over;
        }

        public class Label
        {
            public string Text { get; set; }
            public double FontSize { get; set; } = 16;
            public string? Font { get; set; }
            public int? Width { get; set; }
            public int? Height { get; set; }
            public int FontWeight { get; set; } = 500;
            public string Color { get; set; } = "#000000";
            public Gravity Gravity { get; set; } = Gravity.Center; 
            public int X { get; set; }
            public int Y { get; set; }
        }
    }

    public class ImageComposeV1 : WorkflowStep<ImageComposeOptions>
    {
        public override async ValueTask ExecuteAsync()
        {
            var readSettings = new MagickReadSettings
            {
                FillColor = MagickColors.Blue,
                BackgroundColor = MagickColors.Transparent,
                Width = Options.ImageWidth,
                Height = Options.ImageHeight
            };

            var baseImage = new MagickImage("xc:white", readSettings);
            baseImage.Format = MagickFormat.Png;

            foreach(var layer in Options.Layers)
            {
                var layerImage = CreateLayer(layer);
                baseImage.Composite(layerImage, layer.Operator);
            }



            var outputPath = Path.Combine(Context.WorkingDirectory, Options.OutputPath);
            await baseImage.WriteAsync(outputPath);
        }

        private IMagickImage CreateLayer(Layer layer)
        {
            var readSettings = new MagickReadSettings
            {
                FillColor = MagickColors.Transparent,
                BackgroundColor = MagickColors.Transparent,
                Width = Options.ImageWidth,
                Height = Options.ImageHeight
            };

            var image = new MagickImage("xc:transparent", readSettings);

            if (!string.IsNullOrEmpty(layer.LayerImagePath))
            {
                if (!Path.IsPathRooted(layer.LayerImagePath))
                    layer.LayerImagePath = Path.Combine(Context.WorkingDirectory, layer.LayerImagePath);

                var layerImage = new MagickImage(layer.LayerImagePath);
                image.Composite(layerImage, layer.OffsetX, layer.OffsetY, CompositeOperator.Over);
            }
            

            if (layer.Label != null)
            {
                readSettings = new MagickReadSettings
                {
                    FillColor = new MagickColor(layer.Label.Color),
                    BackgroundColor = MagickColors.Transparent,
                    TextGravity = layer.Label.Gravity,
                    Font = layer.Label.Font,
                    FontWeight = (FontWeight)layer.Label.FontWeight,
                    Width= layer.Label.Width,
                    Height = layer.Label.Height,
                    FontPointsize = layer.Label.FontSize

                };

                var labelText = layer.Label.Text;
                var labelGravity = (Gravity)layer.Label.Gravity;
                if (!string.IsNullOrEmpty(labelText))
                {
                    var countText = new MagickImage($"caption:{labelText}", readSettings);
                    image.Composite(countText, labelGravity, layer.Label.X, layer.Label.Y, CompositeOperator.Over);
                }
            }
            
            
            if (layer.Rotation.HasValue)
                image.Rotate(layer.Rotation.Value);

            if (layer.Scale.HasValue)
                image.Scale(new Percentage(layer.Scale.Value));

            if (layer.Distort != null)
                image.Distort(DistortMethod.Perspective, layer.Distort);

            if (!string.IsNullOrEmpty(layer.LayerAlphaMaskPath))
            {
                var alphaMask = new MagickImage(layer.LayerAlphaMaskPath);
                alphaMask.Transparent(MagickColors.Black);
                image.Composite(alphaMask, CompositeOperator.CopyAlpha);
            }

            return image;
        }
    }
}
