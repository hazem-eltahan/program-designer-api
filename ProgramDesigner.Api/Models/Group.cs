namespace ProgramDesigner.Api.Models
{
    public class Group : ProgramNode
    {
        public GroupRule Rule { get; set; }
        public int? ChoiceCount { get; set; }
        public required List<ProgramNode> Children { get; set; }
    }
}
