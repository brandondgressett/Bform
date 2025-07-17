using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BFormDomain.Tools.ContentGenerator.Abstractions;
using BFormDomain.Tools.ContentGenerator.Models;
using BFormDomain.Tools.ContentGenerator.Providers;
using BFormDomain.Tools.ContentGenerator.Services;

namespace BFormDomain.Tools.ContentGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>(optional: true)
                .Build();

            // Setup DI container
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                await RunInteractiveAsync(serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Application error");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.Configure<GeminiConfiguration>(config =>
            {
                configuration.GetSection("Gemini").Bind(config);
                // Override with environment variable if present
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    config.ApiKey = apiKey;
                }
            });

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            // HTTP Client for Gemini
            services.AddHttpClient<GeminiProvider>();

            // Services
            services.AddSingleton<ILlmProvider, GeminiProvider>();
            services.AddSingleton<ISchemaValidationService, SchemaValidationService>();
            services.AddSingleton<IPromptBuilderService, PromptBuilderService>();
            services.AddSingleton<IContentGeneratorService, ContentGeneratorService>();
        }

        private static async Task RunInteractiveAsync(IServiceProvider serviceProvider)
        {
            var generator = serviceProvider.GetRequiredService<IContentGeneratorService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            Console.WriteLine("===========================================");
            Console.WriteLine("    BForm Content Generator");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("What type of content would you like to create?");
                Console.WriteLine();
                Console.WriteLine("1.  Form Template");
                Console.WriteLine("2.  Work Item Template");
                Console.WriteLine("3.  Work Set Template");
                Console.WriteLine("4.  Table Template");
                Console.WriteLine("5.  KPI Template");
                Console.WriteLine("6.  Report Template");
                Console.WriteLine("7.  HTML Template");
                Console.WriteLine("8.  Scheduled Event Template");
                Console.WriteLine("9.  Table Query Template");
                Console.WriteLine("10. Table Summarization Template");
                Console.WriteLine("11. Business Rule");
                Console.WriteLine();
                Console.WriteLine("0.  Exit");
                Console.WriteLine();
                Console.Write("Enter your choice (0-11): ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                if (choice == "0")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (!int.TryParse(choice, out var choiceNum) || choiceNum < 1 || choiceNum > 11)
                {
                    Console.WriteLine("Invalid choice. Please try again.");
                    Console.WriteLine();
                    continue;
                }

                var contentType = (BFormContentType)(choiceNum - 1);
                
                Console.WriteLine($"Creating {contentType} content...");
                Console.WriteLine();
                Console.WriteLine("Please describe what you want to create:");
                Console.WriteLine("(Be specific about fields, validation rules, workflows, etc.)");
                Console.WriteLine();
                Console.Write("> ");
                
                var userPrompt = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userPrompt))
                {
                    Console.WriteLine("No description provided. Returning to menu.");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine();
                Console.WriteLine("Generating content... This may take a moment.");
                Console.WriteLine();

                try
                {
                    var request = new ContentGenerationRequest
                    {
                        ContentType = contentType,
                        UserPrompt = userPrompt
                    };

                    var response = await generator.GenerateContentAsync(request);

                    if (response.Success)
                    {
                        Console.WriteLine("✓ Content generated successfully!");
                        if (response.RetryCount > 0)
                        {
                            Console.WriteLine($"  (Required {response.RetryCount} retries to pass validation)");
                        }
                        Console.WriteLine();
                        Console.WriteLine("Generated JSON:");
                        Console.WriteLine("```json");
                        Console.WriteLine(response.GeneratedContent);
                        Console.WriteLine("```");
                        Console.WriteLine();

                        Console.WriteLine("Would you like to:");
                        Console.WriteLine("1. Save this content to a file");
                        Console.WriteLine("2. Generate another version");
                        Console.WriteLine("3. Return to main menu");
                        Console.Write("Choice (1-3): ");
                        
                        var saveChoice = Console.ReadLine();
                        if (saveChoice == "1")
                        {
                            await SaveContentAsync(response.GeneratedContent, contentType);
                        }
                        else if (saveChoice == "2")
                        {
                            continue; // Loop will restart with same content type
                        }
                    }
                    else
                    {
                        Console.WriteLine("✗ Failed to generate valid content after multiple attempts.");
                        Console.WriteLine();
                        Console.WriteLine("Validation errors:");
                        foreach (var error in response.ValidationErrors)
                        {
                            Console.WriteLine($"  - {error}");
                        }
                        Console.WriteLine();
                        Console.WriteLine("The generated content was:");
                        Console.WriteLine("```json");
                        Console.WriteLine(response.GeneratedContent);
                        Console.WriteLine("```");
                        Console.WriteLine();
                        Console.WriteLine("You may want to try with a simpler request or provide more specific details.");
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
                {
                    Console.WriteLine("✗ Error: Gemini API key not configured.");
                    Console.WriteLine();
                    Console.WriteLine("Please set the GEMINI_API_KEY environment variable:");
                    Console.WriteLine("  Windows:  set GEMINI_API_KEY=your-api-key");
                    Console.WriteLine("  Linux:    export GEMINI_API_KEY=your-api-key");
                    Console.WriteLine();
                    Console.WriteLine("You can get an API key from: https://makersuite.google.com/app/apikey");
                    Console.WriteLine();
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error generating content");
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    Console.WriteLine();
                }

                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
                Console.Clear();
            }
        }

        private static async Task SaveContentAsync(string content, BFormContentType contentType)
        {
            Console.WriteLine();
            Console.Write("Enter filename (without extension): ");
            var filename = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = $"{contentType.ToString().ToLower()}-{DateTime.Now:yyyyMMdd-HHmmss}";
            }

            var outputDir = "GeneratedContent";
            Directory.CreateDirectory(outputDir);
            
            var filepath = Path.Combine(outputDir, $"{filename}.json");
            await File.WriteAllTextAsync(filepath, content);
            
            Console.WriteLine($"✓ Content saved to: {filepath}");
            Console.WriteLine();
        }
    }
}