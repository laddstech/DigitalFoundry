using LaddsTech.DigitalFoundry.Common;
using LaddsTech.DigitalFoundry.Gpt;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry
{
    public class GptGenerateListingOptions
    {
        public string TitleFormat { get; set; } = "{0}";
        public string DescriptionFormat { get; set; } = "{0}";
        public string[] Prompts { get; set; } = Array.Empty<string>();
        public string[] Examples { get; set; } = Array.Empty<string>();
        public float? TopP { get; set; }
        public float? Temperature { get; set; }
    }

    public class GptGenerateListing : GptBaseStep<GptGenerateListingOptions>
    {
        protected override async ValueTask GenerateAsync(OpenAIService openAiService)
        {
            var listingMetadata = Context.Variables.GetValue<ListingMetadata>(ListingMetadata.VariableKey);

            if (listingMetadata == null || listingMetadata.IsComplete || listingMetadata.GenerationMetadata == null)
                return;

            if (!Options.Prompts.Any())
                return;

            await PopulateProductListingAsync(openAiService, listingMetadata);
        }

        private async Task PopulateProductListingAsync(IOpenAIService service, ListingMetadata listingMetadata)
        {
            var requestMessages = new List<ChatMessage>();
            requestMessages.AddRange(Options.Prompts.Select(prompt => ChatMessage.FromSystem(prompt)));
            requestMessages.AddRange(new ChatMessage[]
            {
                ChatMessage.FromSystem("Make sure to provide your response as JSON."),
                ChatMessage.FromSystem("Format the output as JSON, given the following example: "),
                ChatMessage.FromSystem("{\"title\": \"..\", \"description\": \"..\", \"short_title\": \"..\", \"keywords\": [\"..\", \"..\"] } ")
            });

            requestMessages.AddRange(Options.Examples.Select(prompt => ChatMessage.FromAssistant(prompt)));

            requestMessages.Add(ChatMessage.FromUser(listingMetadata.GenerationMetadata.Description));

            var request = new ChatCompletionCreateRequest
            {
                Messages = requestMessages,
                Model = Models.Gpt_3_5_Turbo_16k,
                TopP = Options.TopP,
                Temperature = Options.Temperature
            };

            var response = await GetJsonCompletionAsync<ProductListingCompletionResult>(service, request);

            if (response == null)
                return;

            listingMetadata.Title = string.Format(Options.TitleFormat, response.Title);
            listingMetadata.Description = string.Format(Options.DescriptionFormat, response.Description);
            listingMetadata.ShortTitle = response.ShortTitle;
            listingMetadata.Keywords = response.Keywords;
        }

    }

    class ProductListingCompletionResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("short_title")]
        public string ShortTitle { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();
    }
}
