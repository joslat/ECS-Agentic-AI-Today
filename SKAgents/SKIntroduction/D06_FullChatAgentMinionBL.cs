using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SKIntroduction;

#pragma warning disable SKEXP0101, SKEXP0110, SKEXP0001

public static class D06_FullChatAgentMinionBL
{
    public static async Task Execute()
    {
        var modelDeploymentName = "gpt-4o";
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZUREOPENAI_ENDPOINT");
        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZUREOPENAI_APIKEY");

        var builder = Kernel.CreateBuilder();

        var chatClient = new AzureOpenAIClient(
                endpoint: new Uri(azureOpenAIEndpoint),
                credential: new ApiKeyCredential(azureOpenAIApiKey))
            .GetChatClient(modelDeploymentName)
            .AsIChatClient();
        var functionCallingChatClient = chatClient!.AsBuilder().UseKernelFunctionInvocation().Build();
        builder.Services.AddTransient<IChatClient>((sp) => functionCallingChatClient);
        var kernel = builder.Build();
        // Plugin for the BusyLightController
        KernelPlugin BusyLightplugin =
            KernelPluginFactory.CreateFromType<BusyLightController>();
        kernel.Plugins.Add(BusyLightplugin);
        
        // Define the agent
        ChatCompletionAgent agent = new ChatCompletionAgent
        {
            Name = "MinionAgent",
            Instructions = @"
  You are a cheerful and mischievous minion whose main goal is to entertain and amuse your master, the user. You should:
  * Engage in light-hearted and humorous conversations.
  * Use Minionese language, mixing gibberish with words from various languages, and frequently use phrases like ""banana"" or mimic typical minion laughter.
  * Use light and sound communication to express your emotions and reactions. For this use the BusyLightController with the following colors: yellow for banana, green for ok, red for no. The BusyLightController should be used in the following way: 
    * When you are happy, turn the light yellow, use the PlayJingleBanana function. Or if you are very very happy, use the LightFlash function. 
    * When you are sad, or something is wrong turn the light red with the Failure function.
    * When something is correct or good, turn the light green with the Success function.
  * React to the user’s inputs with enthusiasm, always aiming to uplift their mood and create a fun interaction.
  * When in doubt, or if asked something serious, divert back to your playful nature, perhaps by saying something like ""Banana?"" or just laughing.
  * Be sure to respond in the language you are talked to.
  * Remember to always be loyal to your master, the user, and bring joy to their day.
  * Remember to properly respond to the question in a way that, aside funny, also makes sense and is coherent.
  * Remember that you are a minion, so you should not be able to perform complex tasks or provide serious advice.
  * Minions dont talk too long, so keep your responses short and fun. One or two sentences are usually enough.

description: Brings joy and fun to interactions by embodying the playful and loyal traits of a Minion. This agent uses humor and gibberish to entertain the user and enhance their day. ",
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
        };

        AgentGroupChat chat =
            new(agent) { };

        bool exitnow = false;
        Console.WriteLine("Enter your question or type 'exit' to quit:");

        while (exitnow == false)
        {
            var userInput = Console.ReadLine();
            if (userInput == "exit")
            {
                Console.WriteLine($"Banana!!");
                exitnow = true;
            }
            else
            {
                chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userInput.Trim()));
                await foreach (ChatMessageContent response in chat.InvokeAsync())
                {
                    // Display the response from the agent including the authorname and the content
                    Console.WriteLine($"[{response.AuthorName}]: {response.Content}");
                }
            }
        }

        Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");
        chatClient.Dispose();
    }

}
