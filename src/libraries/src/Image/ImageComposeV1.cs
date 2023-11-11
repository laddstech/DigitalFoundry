using Flowly.Core;
using ImageMagick;
using OpenAI.ObjectModels.ResponseModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
                if (string.IsNullOrEmpty(layer.LayerImagePath)) {
                    continue;
                }

                var layerImage = CreateLayer(layer);
                baseImage.Composite(layerImage, CompositeOperator.Multiply);
            }



            var outputPath = Path.Combine(Context.WorkingDirectory, Options.OutputPath);
            await baseImage.WriteAsync(outputPath);
        }

        private IMagickImage CreateLayer(Layer layer)
        {
            if (string.IsNullOrEmpty(layer.LayerImagePath)) 
                throw new ArgumentNullException(nameof(layer.LayerImagePath));

            var readSettings = new MagickReadSettings
            {
                FillColor = MagickColors.Transparent,
                BackgroundColor = MagickColors.Transparent,
                Width = Options.ImageWidth,
                Height = Options.ImageHeight
            };

            var image = new MagickImage("xc:transparent", readSettings);

            var layerImage = new MagickImage(layer.LayerImagePath);
            
            if (layer.Rotation.HasValue)
                layerImage.Rotate(layer.Rotation.Value);

            if (layer.Scale.HasValue)
                layerImage.Scale(new Percentage(layer.Scale.Value));

            if (layer.Distort != null)
                layerImage.Distort(DistortMethod.Perspective, layer.Distort);

            image.Composite(layerImage, layer.OffsetX, layer.OffsetY, CompositeOperator.Over);

            //image.BackgroundColor = MagickColors.Transparent;   

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
