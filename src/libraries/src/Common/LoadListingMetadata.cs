using Flowly.Core;
using LaddsTech.DigitalFoundry.Common;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry
{
    public class LoadListingMetadata : WorkflowStep
    {
        private const string MetadataFileName = "listing.json";

        public override async ValueTask ExecuteAsync()
        {
            var metadataPath = Path.Combine(Context.WorkingDirectory, MetadataFileName);

            if (!File.Exists(metadataPath))
                throw new InvalidOperationException($"Could not find listing metadata file '{metadataPath}'.");

            var metadataContent = await File.ReadAllTextAsync(metadataPath);

            var metadata = JsonSerializer.Deserialize<ListingMetadata>(metadataContent);

            if (metadata == null)
                throw new InvalidOperationException($"The listing metadata file '{metadataPath}' is empty or invalid.");

            Context.Variables.SetValue(ListingMetadata.VariableKey, metadata);
        }
    }
}
