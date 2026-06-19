/// <summary>
/// Module 2: Build Your First Agent (Pro-Code Version)
/// Portal equivalent: Build → Agents → Create Agent + File Search + Code Interpreter
/// 
/// This step creates the Contoso Estimator agent programmatically,
/// uploads rate library documents, and connects File Search + Code Interpreter.
/// </summary>

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace ContosoEstimator.Steps;

public static class Step02_CreateAgent
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 2: Create Agent with File Search (Module 2) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var modelDeployment = config["Foundry:ModelDeployment"]!;

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        // System instructions (same as portal version)
        var instructions = """
            You are the Contoso Estimator Advisor, an AI assistant for Contoso Infrastructure's
            estimation and tendering teams.

            ## Your Role
            - Help estimators look up rates from the company rate library
            - Provide guidance on estimation policies and approval thresholds
            - Perform cost calculations when given quantities and rates

            ## Data Source Policy
            - For rates and policies: ONLY use information from your connected tools.
            - Always cite your source: "According to [document name]..."
            - If you cannot find the answer, say so clearly.

            ## Boundaries
            - Do NOT disclose margin percentages or markup policies
            - Do NOT provide competitor pricing information
            """;

        // Create agent with File Search and Code Interpreter tools
        var agentDefinition = new DeclarativeAgentDefinition(model: modelDeployment)
        {
            Instructions = instructions,
            Tools =
            {
                ResponseTool.CreateFileSearchTool(vectorStoreIds: []),
                ResponseTool.CreateCodeInterpreterTool(
                    new CodeInterpreterToolContainer(
                        new AutomaticCodeInterpreterToolContainerConfiguration()))
            }
        };

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
            agentName: "contoso-estimator-advisor",
            options: new(agentDefinition));

        Console.WriteLine($"Agent created: {agentVersion.Name} (version: {agentVersion.Version})");
        Console.WriteLine($"Model: {modelDeployment}");
        Console.WriteLine($"Tools: File Search, Code Interpreter");
        Console.WriteLine("\n✅ Step 2 complete — agent created programmatically.\n");

        // Note: In production, you'd upload files to the agent's file store here.
        // For this demo, files are uploaded via portal in Module 2.
    }
}
