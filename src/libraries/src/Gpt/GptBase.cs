using Flowly.Core;
using OpenAI;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaddsTech.DigitalFoundry.Gpt
{
    public abstract class GptBaseStep<T> : WorkflowStep<T> where T : class, new()
    {
        public override async ValueTask ExecuteAsync()
        {
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Context.Variables.GetValue<string>("OPEN_AI_API_KEY") ?? Environment.GetEnvironmentVariable("OPEN_AI_API_KEY")
            });

            await GenerateAsync(openAiService);
        }

        protected abstract ValueTask GenerateAsync(OpenAIService openAiService);

        protected async Task<string> GetCompletionAsync(IOpenAIService service, ChatCompletionCreateRequest request)
        {
            var completionResult = await service.ChatCompletion.CreateCompletion(request);

            if (completionResult.Successful)
            {
                return completionResult.Choices.First().Message.Content;
            }

            return null;
        }

        protected async Task<T?> GetJsonCompletionAsync<T>(IOpenAIService service, ChatCompletionCreateRequest request) where T : class
        {
            var result = await GetCompletionAsync(service, request);

            if (!string.IsNullOrEmpty(result))
                return JsonSerializer.Deserialize<T>(result);

            return null;
        }
    }
}
