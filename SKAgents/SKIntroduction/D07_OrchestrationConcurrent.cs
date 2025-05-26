using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace SKIntroduction;

#pragma warning disable SKEXP0101, SKEXP0110, SKEXP0001

public static class D07_OrchestrationConcurrent
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

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

        ChatCompletionAgent agent1 = CreateThemeAgent(kernel);
        ChatCompletionAgent agent2 = CreateSentimentAgent(kernel);
        ChatCompletionAgent agent3 = CreateEntityAgent(kernel);

        // Define the orchestration with transform
        StructuredOutputTransform<Analysis> outputTransform =
            new(kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings { ResponseFormat = typeof(Analysis) });

        ConcurrentOrchestration<string, Analysis> orchestration =
            new(agent1, agent2, agent3)
            {
                ResultTransform = outputTransform.TransformAsync,
            };

        // Start the runtime
        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        // Run the orchestration
        string input = GetECSSummitReport(); 
        OrchestrationResult<Analysis> result = await orchestration.InvokeAsync(input, runtime);

        Analysis output = await result.GetValueAsync(TimeSpan.FromSeconds(60 * 2));
        Console.WriteLine($"\n# RESULT:\n{JsonSerializer.Serialize(output, s_options)}");

        await runtime.RunUntilIdleAsync();

    }

    private static ChatCompletionAgent CreateThemeAgent(Kernel kernel) => new()
    {
        Name = "ThemeExtractor",
        Description = "An expert in identifying themes in articles.",
        Instructions = "Given an article, identify the main themes.",
        Kernel = kernel
    };

    private static ChatCompletionAgent CreateSentimentAgent(Kernel kernel) => new()
    {
        Name = "SentimentAnalyzer",
        Description = "An expert in sentiment analysis.",
        Instructions = "Given an article, identify the sentiment.",
        Kernel = kernel
    };

    private static ChatCompletionAgent CreateEntityAgent(Kernel kernel) => new()
    {
        Name = "EntityRecognizer",
        Description = "An expert in entity recognition.",
        Instructions = "Given an article, extract the entities.",
        Kernel = kernel
    };

    private sealed class Analysis
    {
        public IList<string> Themes { get; set; } = [];
        public IList<string> Sentiments { get; set; } = [];
        public IList<string> Entities { get; set; } = [];
    }

    public static string GetECSSummitReport()
    {
        return @"
        ## Summary
        The European AI & Cloud Summit 2025 takes place in Düsseldorf, Germany from May 26–28 at the CCD Congress Center Düsseldorf, bringing together over 3,000 attendees for more than 250 sessions on AI, Microsoft Azure, OpenAI, cloud security, Microsoft 365, Power Platform, and more.

        ## Overview
        Organized by ecs.events OÜ (a Südwind company) with support from global AI, Azure, and cloud communities. Co-located with the European Collaboration Summit and European BizApps Summit for a unified experience.

        ## Topics & Tracks
        - **Artificial Intelligence & Azure**: Sessions on AI in business, the AI landscape, and the latest Azure innovations.  
        - **Cloud Security & Governance**: Best practices for securing cloud workloads, data protection in Azure SQL, and AI responsibility.  
        - **Modern Work & Collaboration**: Deep dives into Microsoft 365, Teams, SharePoint, and Copilot.  
        - **Power Platform & Fabric**: Tutorials on citizen development, Microsoft Fabric, and low-code/no-code applications.

        ## Sessions & Tutorials
        - **Full-day Workshops** by experts like Vesa Juvonen, Julie M Turner, Derek Cash-Peterson, Thomas Goelles, Stephan Bisser, Paolo Pialorsi, David Warner II, and Hugo Bernier.  
        - **Formats**: Hands-on labs, power classes, deep dives, panels, and lightning talks for developers, architects, security pros, and business leaders.

        ## Keynotes & Speakers
        - **Keynotes** by Microsoft executives, Regional Directors, and MVPs on adaptive, secure, and scalable cloud technologies.  
        - **Featured Speakers** include Carlotta Castelluccio, Ivan Vagunin, Daniel Costea, and others.

        ## Startup & Community
        - **Startup Stage**: Showcases innovative Azure and OpenAI startups, including an AI competition judged by industry veterans.  
        - **Community**: In collaboration with global user groups, meetups, and CollabDays under the #CommunityRocks banner.

        ## Sponsors & Partners
        Major sponsors include YASH Technologies, Capgemini, ITENOS, and other leaders in cloud infrastructure and AI services.

        ## Venue & Logistics
        - **Location**: CCD Congress Center Düsseldorf (Stockumer Kirchstraße 61, 40474 Düsseldorf).  
        - **Language**: English. Networking events include social gatherings and community meetups.

        ";
    }

}
