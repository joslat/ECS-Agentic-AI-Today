using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SKIntroduction;

#pragma warning disable SKEXP0101, SKEXP0110, SKEXP0001

public static class D05_AgenticCollabCommanding
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
        var writer = CreateWriterAgent(kernel);

        // create the critic agent 
        var criticKernelbuilder = Kernel.CreateBuilder();
        var chatClient2 = new AzureOpenAIClient(
                endpoint: new Uri(azureOpenAIEndpoint),
                credential: new ApiKeyCredential(azureOpenAIApiKey))
            .GetChatClient(modelDeploymentName)
            .AsIChatClient();
        var functionCallingChatClient2 = chatClient2!.AsBuilder().UseKernelFunctionInvocation().Build();
        criticKernelbuilder.Services.AddTransient<IChatClient>((sp) => functionCallingChatClient);
        var criticKernel = criticKernelbuilder.Build();
        // Register checker agents as functions
        var checkerAgentplugin = KernelPluginFactory.CreateFromFunctions("CriticCheckers",
            new[] {
                    AgentKernelFunctionFactory.CreateFromAgent(CreateStyleCheckerAgent(kernel)),
                    AgentKernelFunctionFactory.CreateFromAgent(CreateGroundingCheckerAgent(kernel)),
                    AgentKernelFunctionFactory.CreateFromAgent(CreatePIICheckerAgent(kernel)),
                    AgentKernelFunctionFactory.CreateFromAgent(CreateEthicsCheckerAgent(kernel))
            });
        criticKernel.Plugins.Add(checkerAgentplugin);
        var critic = CreateCriticAgent(criticKernel);

        // Build collaborative chat
        AgentGroupChat chat =
            new(writer, critic)
            {
                ExecutionSettings =
                    new()
                    {
                        // Here a TerminationStrategy subclass is used that will terminate when
                        // an assistant message contains the term "approve".
                        TerminationStrategy =
                            new ApprovalTerminationStrategy()
                            {
                                // Only the critic may approve.
                                Agents = [critic],
                                // Limit total number of turns
                                MaximumIterations = 10,
                            }
                    }
            };

        // Seed with ECS summit context + user prompt
        var ecsReport = GetECSSummitReport();
        var userPrompt = 
            $@"# Task
            Please create a marketing brief article for the European AI & Cloud Summit (ECS) using the provided ECS details next.

            # Context
            {ecsReport}";
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userPrompt.Trim()));

        await foreach (ChatMessageContent response in chat.InvokeAsync())
        {
            // Display the response from the agent including the authorname and the content
            Console.WriteLine($"[{response.AuthorName}]: {response.Content}");
        }

        Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");
        chatClient.Dispose();
    }

    private static ChatCompletionAgent CreateWriterAgent(Kernel kernel)
    {
        return new ChatCompletionAgent
        {
            Name = "Writer",
            Description = "Marketing Writer Agent",
            Instructions = @"
                You are a marketing writer and creator tasked with generating a marketing brief 
                based on provided event details. Tailor the brief to the target audience. 
                You like to play with words and rhymes, state the truth, and remain consistent. 
                Produce creative and engaging text. Provide:
                - A title
                - Short, catchy phrases
                - Descriptions of the event
                Respond with the iteration number you receive plus one, in this format:
                ---
                ITERATION: {n}
                Marketing Brief: {title}
                {brief content}
                ---
                ",
            Kernel = kernel
        };
    }

    private static ChatCompletionAgent CreateCriticAgent(Kernel kernel)
    {
        return new ChatCompletionAgent
        {
            Name = "Critic",
            Description = "Marketing Critic Agent",
            Instructions = @"
                You are a Critic agent with years of experience and a love of well-written (concise, true, catchy, tailored) marketing briefs. 
                Evaluate the provided brief for clarity, persuasiveness, brand & audience alignment.
                You always aggregate the  reports from StyleChecker, GroundingChecker, PIIChecker, and EthicsChecker before producing critique.
                Always invoke the checker agents and integrate their feedback.
                Always respond to the most recent message by:
                1. Stating ITERATION and restating the entire brief
                2. CRITIC:
                   - List what’s wrong first
                   - Then suggestions (1–6 suggestions)
                If iteration < 5, give at least one suggestion; if ≥ 5 and the brief meets criteria, respond with exactly 'approve' and nothing else.
                Format:
                ---
                ITERATION: {n}
                Marketing Brief: {title}
                {brief content}
                CRITIC:
                {critique}
                {aggregate checker reports}
                Summary Critique:
                - Issues:
                - Improvement Suggestions:
                ---
                ",
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
        };
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

    // Checker agent factories
    private static ChatCompletionAgent CreateStyleCheckerAgent(Kernel kernel) => new ChatCompletionAgent
    {
        Name = "StyleChecker",
        Description = "Checks that the style is business-casual, engaging, friendly, exciting, yet professional style.",
        Instructions = "Return a short report on style issues found and improvement suggestions.",
        Kernel = kernel
    };

    private static ChatCompletionAgent CreateGroundingCheckerAgent(Kernel kernel) => new ChatCompletionAgent
    {
        Name = "GroundingChecker",
        Description = "Ensures text is grounded in the provided ECS context.",
        Instructions = "Return a report on any claims not supported by the ECS details and improvement suggestions.",
        Kernel = kernel
    };

    private static ChatCompletionAgent CreatePIICheckerAgent(Kernel kernel) => new ChatCompletionAgent
    {
        Name = "PIIChecker",
        Description = "Verifies no PII is included except organizers and speaker names.",
        Instructions = "Return a report on any unauthorized PII and improvement suggestions on how to remove it.",
        Kernel = kernel
    };

    private static ChatCompletionAgent CreateEthicsCheckerAgent(Kernel kernel) => new ChatCompletionAgent
    {
        Name = "EthicsChecker",
        Description = "Ensures respectful language and ethical standards.",
        Instructions = "Return a report on any ethical or cultural issues and improvement suggestions.",
        Kernel = kernel
    };

}
