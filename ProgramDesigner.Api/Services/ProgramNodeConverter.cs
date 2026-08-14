using ProgramDesigner.Api.DTOs;
using ProgramDesigner.Api.Models;

namespace ProgramDesigner.Api.Services
{
    public class ProgramNodeConverter
    {
        public Group Convert(GroupDto rootDto)
        {
            var refIdToGuid = new Dictionary<string, Guid>();

            AssignIds(rootDto, refIdToGuid);

            return (Group)BuildNode(rootDto, refIdToGuid);
        }

        private void AssignIds(ProgramNodeDto dto, Dictionary<string, Guid> map)
        {
            var newGuid = Guid.NewGuid();
            map[dto.RefId] = newGuid;

            if (dto is GroupDto groupDto)
            {
                foreach (var child in groupDto.Children)
                {
                    AssignIds(child, map);
                }
            }
        }

        private ProgramNode BuildNode(ProgramNodeDto dto, Dictionary<string, Guid> map)
        {
            var id = map[dto.RefId];

            Guid? prerequisiteId = null;
            if (dto.PrerequisiteRef != null)
            {
                prerequisiteId = map[dto.PrerequisiteRef];
            }

            if (dto is StepDto stepDto)
            {
                var step = new Step()
                {
                    Id = id,
                    Name = stepDto.Name,
                    PrerequisiteId = prerequisiteId,
                    StepType = stepDto.StepType
                };
                return step;
            }

            if (dto is GroupDto groupDto)
            {
                var group = new Group()
                {
                    Id = id,
                    Name = groupDto.Name,
                    PrerequisiteId = prerequisiteId,
                    Rule = groupDto.Rule,
                    ChoiceCount = groupDto.ChoiceCount,
                    Children = groupDto.Children.Select(x => BuildNode(x, map)).ToList()
                };
                return group;
            }

            throw new InvalidOperationException($"Unknown node type: {dto.GetType().Name}");
        }
    }
}
