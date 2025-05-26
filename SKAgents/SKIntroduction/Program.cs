using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKIntroduction;

Console.WriteLine("Hello, Semantic Kernel!");

//await BasicSK.Execute();
//await BasicSKChat.Execute();

//await D01_SingleAgent.Execute();
//await D02_ChatClientAgent.Execute();
//await D03_FullChatAgent.Execute();
//await D04_AgenticCollaboration.Execute();
//await D05_AgenticCollabCommanding.Execute();

//await D06_FullChatAgentMinionBL.Execute();
await D07_OrchestrationConcurrent.Execute();




Console.WriteLine("Type enter to finish");
Console.ReadLine();


