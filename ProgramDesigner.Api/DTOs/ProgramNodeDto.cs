using System.Text.Json.Serialization;

namespace ProgramDesigner.Api.DTOs
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(StepDto), "Step")]
    [JsonDerivedType(typeof(GroupDto), "Group")]
    public abstract class ProgramNodeDto
    {
        public required string RefId { get; set; }
        public required string Name { get; set; }
        public string? PrerequisiteRef { get; set; }
    }
}
