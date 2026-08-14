using System.Text.Json.Serialization;

namespace ProgramDesigner.Api.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(Step), "Step")]
    [JsonDerivedType(typeof(Group), "Group")]
    public abstract class ProgramNode
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public Guid? PrerequisiteId { get; set; }
    }
}
