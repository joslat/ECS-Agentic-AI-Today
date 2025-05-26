using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.AI;

namespace SKIntroduction;

#pragma warning disable SKEXP0101

public static class D01_SingleAgent
{
    public static async Task Execute()
    {
        var modelDeploymentName = "gpt-4o";
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZUREOPENAI_ENDPOINT");
        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZUREOPENAI_APIKEY");

        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                modelDeploymentName,
                azureOpenAIEndpoint,
                azureOpenAIApiKey)
            .Build();
        kernel.ImportPluginFromType<WhatDateIsIt>();

        //IChatClient chatClient = kernel.GetRequiredService<IChatClient>();
        


        //var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // Define the agent
        ChatCompletionAgent agent = new ChatCompletionAgent
        {
            Name = "Date Inquiry Agent",
            Description = "An agent that provides the current date and significant historical events for that date. it also can tell jokes and edit text.",
            Kernel = kernel,
        };

        await InvokeAgentAsync(agent, "Tell me a joke about a pirate.");
        await InvokeAgentAsync(agent,"Now add some emojis to the joke.");

        //ChatHistory chatHistory = new();

        //var executionSettings = new OpenAIPromptExecutionSettings
        //{
        //    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        //};

        //bool exitnow = false;
        //while (exitnow == false)
        //{
        //    Console.WriteLine("Enter your question or type 'exit' to quit:");
        //    var userInput = Console.ReadLine();
        //    if (userInput == "exit")
        //    {
        //        Console.WriteLine($"Banana!!");
        //        exitnow = true;
        //    }
        //    else
        //    {
        //        chatHistory.AddUserMessage(userInput);

        //        var response = await chatService.GetChatMessageContentAsync(
        //            chatHistory,
        //            executionSettings,
        //            kernel);

        //        Console.WriteLine(response.ToString());
        //        chatHistory.Add(response);
        //    }
        //}
    }

    static async Task InvokeAgentAsync(ChatCompletionAgent agent, string input)
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        Console.WriteLine($"User: {message}");

        await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(message))
        {
            Console.WriteLine($"Agent Response: {response.Message}");
        }
    }

}
