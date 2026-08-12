using ProgramDesigner.Api.Models;

namespace ProgramDesigner.Api.DTOs
{
    public class GroupDto : ProgramNodeDto
    {
        public GroupRule Rule { get; set; }
        public int? ChoiceCount { get; set; }
        public required List<ProgramNodeDto> Children { get; set; }
    }
}
