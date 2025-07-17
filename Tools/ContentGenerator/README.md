# BForm Content Generator

An AI-powered tool to generate BForm content JSON templates based on natural language descriptions.

## Overview

The BForm Content Generator uses Large Language Models (LLMs) to create valid BForm content templates including:
- Form Templates
- Work Item Templates  
- Work Set Templates
- Table Templates
- KPI Templates
- Report Templates
- HTML Templates
- Scheduled Event Templates
- Table Query Templates
- Table Summarization Templates
- Business Rules

## Features

- **Interactive CLI**: Simple menu-driven interface for content generation
- **Schema Validation**: Automatically validates generated content against JSON schemas
- **Retry Logic**: Attempts to fix validation errors automatically (up to 3 retries)
- **LLM Abstraction**: Easily swap between different LLM providers
- **Context-Aware**: Includes relevant schemas, examples, and documentation in prompts

## Setup

### Prerequisites

- .NET 6.0 SDK or later
- Gemini API key (get one from https://makersuite.google.com/app/apikey)

### Installation

1. Clone the repository and navigate to the tool directory:
```bash
cd BFormDomain/Tools/ContentGenerator
```

2. Set your Gemini API key:
```bash
# Windows
set GEMINI_API_KEY=your-api-key-here

# Linux/Mac
export GEMINI_API_KEY=your-api-key-here
```

3. Build the project:
```bash
dotnet build
```

## Usage

Run the tool:
```bash
dotnet run
```

Follow the interactive prompts to:
1. Select the type of content to generate
2. Describe what you want to create
3. Review the generated content
4. Optionally save to a file

### Example Prompts

**Form Template:**
> Create a customer registration form with fields for name, email, phone, company, and address. Include validation for email format and required fields.

**Work Item Template:**
> Create a bug tracking work item with fields for severity, affected version, steps to reproduce, and expected behavior. Include status workflow from New to Resolved.

**Table Template:**
> Create an inventory table with columns for product name, SKU, quantity, unit price, and last updated timestamp.

**KPI Template:**
> Create a customer satisfaction KPI that calculates average rating from survey responses, with thresholds for red (below 3), yellow (3-4), and green (above 4).

**Rule:**
> Create a rule that sends an email notification when a high-priority work item is created, including the assignee and description in the email.

## Configuration

Edit `appsettings.json` to configure:
- Logging levels
- Gemini model settings (temperature, max tokens)

## Architecture

The tool follows a clean architecture with:
- **Abstractions**: Interfaces for LLM providers
- **Providers**: Concrete implementations (currently Gemini)
- **Services**: Business logic for content generation, validation, and prompt building
- **Models**: Request/response DTOs

## Adding New LLM Providers

To add a new LLM provider:

1. Implement the `ILlmProvider` interface
2. Register your provider in `Program.cs`
3. Configure any necessary settings

Example:
```csharp
public class OpenAIProvider : ILlmProvider
{
    public string Name => "OpenAI";
    
    public async Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken)
    {
        // Your implementation here
    }
}
```

## Troubleshooting

**API Key Issues:**
- Ensure GEMINI_API_KEY environment variable is set
- Check that the API key has proper permissions

**Validation Failures:**
- The tool will retry up to 3 times to fix validation errors
- If it still fails, try simplifying your request or being more specific

**Network Issues:**
- Check your internet connection
- Verify firewall settings allow HTTPS requests to generativelanguage.googleapis.com

## Future Enhancements

- Support for additional LLM providers (OpenAI, Anthropic, etc.)
- Batch generation from CSV/JSON input
- Template library with common patterns
- Fine-tuning support for domain-specific generation
- Web UI interface