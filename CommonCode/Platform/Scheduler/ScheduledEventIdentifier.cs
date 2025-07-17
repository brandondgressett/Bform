namespace BFormDomain.CommonCode.Platform.Scheduler;

public record ScheduledEventIdentifier(string TriggerGroup, string TriggerName, string JobGroup, string JobId);
