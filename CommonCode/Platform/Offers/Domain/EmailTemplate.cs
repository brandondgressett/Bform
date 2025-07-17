using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BFormDomain.CommonCode.Platform.Offers.Domain
{
    /// <summary>
    /// Represents an email template for promotional offer communications
    /// </summary>
    public class EmailTemplate
    {
        /// <summary>
        /// Email subject line, may contain template variables
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// HTML content of the email with {{variable}} placeholders
        /// </summary>
        public string HtmlContent { get; set; } = string.Empty;

        /// <summary>
        /// List of available variables that can be used in the template
        /// </summary>
        public List<string> Variables { get; set; } = new();

        /// <summary>
        /// Default variables that are always available
        /// </summary>
        public static readonly List<string> DefaultVariables = new()
        {
            "userName",
            "userEmail",
            "offerName",
            "offerDescription",
            "serviceUnitCount",
            "purchaseDate",
            "purchaseAmount",
            "dashboardLink",
            "bookingLink",
            "supportEmail",
            "companyName"
        };

        /// <summary>
        /// Renders the HTML content with the provided variable values
        /// </summary>
        public string RenderHtml(Dictionary<string, string> values)
        {
            var result = HtmlContent;
            
            // Replace all variables with their values
            foreach (var kvp in values)
            {
                var pattern = $@"{{{{{kvp.Key}}}}}";
                result = result.Replace(pattern, kvp.Value ?? string.Empty);
            }

            // Remove any unreplaced variables
            result = Regex.Replace(result, @"{{.*?}}", string.Empty);

            return result;
        }

        /// <summary>
        /// Renders the subject with the provided variable values
        /// </summary>
        public string RenderSubject(Dictionary<string, string> values)
        {
            var result = Subject;
            
            foreach (var kvp in values)
            {
                var pattern = $@"{{{{{kvp.Key}}}}}";
                result = result.Replace(pattern, kvp.Value ?? string.Empty);
            }

            // Remove any unreplaced variables
            result = Regex.Replace(result, @"{{.*?}}", string.Empty);

            return result;
        }

        /// <summary>
        /// Extracts all variable names used in the template
        /// </summary>
        public List<string> ExtractUsedVariables()
        {
            var variables = new HashSet<string>();
            var pattern = @"{{(\w+)}}";
            
            // Extract from HTML content
            var htmlMatches = Regex.Matches(HtmlContent, pattern);
            foreach (Match match in htmlMatches)
            {
                if (match.Groups.Count > 1)
                    variables.Add(match.Groups[1].Value);
            }

            // Extract from subject
            var subjectMatches = Regex.Matches(Subject, pattern);
            foreach (Match match in subjectMatches)
            {
                if (match.Groups.Count > 1)
                    variables.Add(match.Groups[1].Value);
            }

            return variables.ToList();
        }

        /// <summary>
        /// Validates the email template
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Subject))
                errors.Add("Email subject is required");

            if (string.IsNullOrWhiteSpace(HtmlContent))
                errors.Add("Email HTML content is required");

            // Check for basic HTML structure
            if (!string.IsNullOrWhiteSpace(HtmlContent))
            {
                if (!HtmlContent.Contains("<") || !HtmlContent.Contains(">"))
                    errors.Add("Email content must be valid HTML");
            }

            // Validate that used variables are declared
            var usedVars = ExtractUsedVariables();
            var allAvailableVars = Variables.Concat(DefaultVariables).ToList();
            var undeclaredVars = usedVars.Where(v => !allAvailableVars.Contains(v)).ToList();
            
            if (undeclaredVars.Any())
                errors.Add($"Undefined variables used in template: {string.Join(", ", undeclaredVars)}");

            return errors.Count == 0;
        }

        /// <summary>
        /// Creates a default welcome email template
        /// </summary>
        public static EmailTemplate CreateDefaultWelcomeTemplate()
        {
            return new EmailTemplate
            {
                Subject = "Welcome to {{companyName}}! Your {{offerName}} is ready",
                HtmlContent = @"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h1>Welcome, {{userName}}!</h1>
                        <p>Thank you for purchasing our <strong>{{offerName}}</strong> package!</p>
                        <p>You now have {{serviceUnitCount}} service unit(s) to use at your convenience.</p>
                        <div style='margin: 30px 0;'>
                            <a href='{{dashboardLink}}' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Go to Dashboard
                            </a>
                        </div>
                        <p>If you have any questions, please contact us at {{supportEmail}}.</p>
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #666; font-size: 12px;'>
                            This email was sent to {{userEmail}} on {{purchaseDate}}.
                        </p>
                    </div>",
                Variables = new List<string>()
            };
        }
    }
}