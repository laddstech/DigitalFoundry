using Flowly.Core;
using LaddsTech.DigitalFoundry.Common;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry
{
    public class SaveListingMetadata : WorkflowStep
    {
        private const string MetadataFileName = "listing.json";
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true };
        public override ValueTask ExecuteAsync()
        {
            var metadataPath = Path.Combine(Context.WorkingDirectory, MetadataFileName);

            var metadata = Context.Variables.GetValue<ListingMetadata>(ListingMetadata.VariableKey);

            if (metadata == null)
                throw new InvalidOperationException($"The listing metadata is not available. Make sure the '{ListingMetadata.VariableKey}' job variable is set before executing this step.");

            var metadataContent = JsonSerializer.Serialize(metadata, SerializerOptions);

            File.WriteAllText(metadataPath, metadataContent);

            return new ValueTask();
        }
    }
}
