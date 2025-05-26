using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;


namespace SKIntroduction;

#pragma warning disable SKEXP0101

public static class D03_FullChatAgent
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

        kernel.ImportPluginFromType<WhatDateIsIt>();


        // Define the agent
        ChatCompletionAgent agent = new ChatCompletionAgent
        {
            Name = "Date Inquiry Agent",
            Description = "An agent that provides the current date and significant historical events for that date. it also can tell jokes and edit text.",
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
        };

        bool exitnow = false;
        while (exitnow == false)
        {
            Console.WriteLine("Enter your question or type 'exit' to quit:");
            var userInput = Console.ReadLine();
            if (userInput == "exit")
            {
                Console.WriteLine($"Banana!!");
                exitnow = true;
            }
            else
            {
                await foreach (AgentResponseItem<Microsoft.SemanticKernel.ChatMessageContent> response in agent.InvokeAsync(userInput))
                {
                    Console.WriteLine($"Agent Response: {response.Message}");
                }
            }
        }
    }

}
