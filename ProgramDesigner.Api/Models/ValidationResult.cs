namespace ProgramDesigner.Api.Models
{
    public record ImpossiblePrerequisite(string Code, string Description);
    public record ReachabilityWarning(string Code,string Description);
    public class ValidationResult
    {
        public bool IsValid => ImpossiblePrerequisites.Count == 0;
        public List<ImpossiblePrerequisite> ImpossiblePrerequisites { get; init; } = [];
        public List<ReachabilityWarning> ReachabilityWarnings { get; init; } = [];
    }
}
