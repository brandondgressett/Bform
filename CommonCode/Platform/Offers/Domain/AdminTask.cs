using System;
using System.Collections.Generic;
using System.Linq;

namespace BFormDomain.CommonCode.Platform.Offers.Domain
{
    /// <summary>
    /// Represents an administrative task that should be triggered when an offer is purchased
    /// </summary>
    public class AdminTask
    {
        /// <summary>
        /// Unique identifier for the task
        /// </summary>
        public string TaskId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Description of the task to be performed
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Email addresses of administrators who should be notified
        /// </summary>
        public List<string> NotifyEmails { get; set; } = new();

        /// <summary>
        /// Whether to include user details in the notification
        /// </summary>
        public bool IncludeUserDetails { get; set; } = true;

        /// <summary>
        /// Priority level of the task
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>
        /// Category of the task for grouping and filtering
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// Optional deadline for task completion
        /// </summary>
        public TimeSpan? CompletionDeadline { get; set; }

        /// <summary>
        /// Additional metadata for the task
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Validates the admin task
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Description))
                errors.Add("Task description is required");

            if (!NotifyEmails.Any())
                errors.Add("At least one notification email is required");

            // Validate email addresses
            foreach (var email in NotifyEmails)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    errors.Add("Empty email address found in notification list");
                }
                else if (!IsValidEmail(email))
                {
                    errors.Add($"Invalid email address: {email}");
                }
            }

            if (CompletionDeadline.HasValue && CompletionDeadline.Value.TotalMinutes < 1)
                errors.Add("Completion deadline must be at least 1 minute");

            return errors.Count == 0;
        }

        /// <summary>
        /// Basic email validation
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a notification email content for this task
        /// </summary>
        public string CreateNotificationHtml(string offerName, string tenantName, Dictionary<string, string>? userDetails = null)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333;'>New Administrative Task Required</h2>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 0 0 10px 0;'><strong>Task:</strong> {Description}</p>
                        <p style='margin: 0 0 10px 0;'><strong>Priority:</strong> {Priority}</p>
                        <p style='margin: 0 0 10px 0;'><strong>Category:</strong> {Category}</p>
                        <p style='margin: 0 0 10px 0;'><strong>Offer:</strong> {offerName}</p>
                        <p style='margin: 0;'><strong>Tenant:</strong> {tenantName}</p>";

            if (CompletionDeadline.HasValue)
            {
                html += $@"
                        <p style='margin: 10px 0 0 0; color: #dc3545;'>
                            <strong>Deadline:</strong> Complete within {FormatTimeSpan(CompletionDeadline.Value)}
                        </p>";
            }

            html += @"
                    </div>";

            if (IncludeUserDetails && userDetails != null && userDetails.Any())
            {
                html += @"
                    <h3 style='color: #333; margin-top: 30px;'>Customer Details</h3>
                    <table style='width: 100%; border-collapse: collapse;'>";

                foreach (var detail in userDetails)
                {
                    html += $@"
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd; font-weight: bold; width: 30%;'>{detail.Key}:</td>
                            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{detail.Value}</td>
                        </tr>";
                }

                html += @"
                    </table>";
            }

            if (Metadata.Any())
            {
                html += @"
                    <h3 style='color: #333; margin-top: 30px;'>Additional Information</h3>
                    <ul style='list-style-type: none; padding-left: 0;'>";

                foreach (var meta in Metadata)
                {
                    html += $@"
                        <li style='margin-bottom: 5px;'>• <strong>{meta.Key}:</strong> {meta.Value}</li>";
                }

                html += @"
                    </ul>";
            }

            html += @"
                    <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #666; font-size: 12px;'>
                        This is an automated notification from the Promotional Offers system.
                        Task ID: " + TaskId + @"
                    </p>
                </div>";

            return html;
        }

        /// <summary>
        /// Formats a TimeSpan for display
        /// </summary>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} day(s)";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hour(s)";
            return $"{(int)timeSpan.TotalMinutes} minute(s)";
        }
    }

    /// <summary>
    /// Priority levels for admin tasks
    /// </summary>
    public enum TaskPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
}