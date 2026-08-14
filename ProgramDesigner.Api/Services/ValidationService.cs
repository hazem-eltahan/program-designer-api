using ProgramDesigner.Api.Models;

namespace ProgramDesigner.Api.Services
{
    public class ValidationService
    {
        public ValidationResult Validate(Group program)
        {
            var impossible = new List<ImpossiblePrerequisite>();
            var warnings = new List<ReachabilityWarning>();

            foreach (var node in GetAllNodes(program))
            {
                if (node.PrerequisiteId == null)
                {
                    continue;
                }

                var prerequisiteId = node.PrerequisiteId.Value;

                if (!IsValidPrerequisite(node.Id, prerequisiteId, program))
                {
                    impossible.Add(new ImpossiblePrerequisite(
                        Code: "IMPOSSIBLE_PREREQUISITE",
                        Description: $"'{node.Name}' has a prerequisite on '{GetNodeName(prerequisiteId, program)}', which is either itself, inside itself, or comes later in the program."
                    ));
                    continue; //already-impossible prerequisite
                }

                if (!IsGuaranteedReachable(prerequisiteId, program))
                {
                    warnings.Add(new ReachabilityWarning(
                        Code: "POTENTIALLY_UNREACHABLE",
                        Description: $"'{node.Name}' has a prerequisite on '{GetNodeName(prerequisiteId, program)}', which is inside a choice branch that some participants may never select."
                    ));
                }
            }

            return new ValidationResult
            {
                ImpossiblePrerequisites = impossible,
                ReachabilityWarnings = warnings
            };
        }
        private string GetNodeName(Guid id, Group root)
        {
            var path = FindPath(root, id, new List<ProgramNode>());
            return path?.Last().Name ?? "Unknown";
        }
        private List<ProgramNode>? FindPath(ProgramNode current, Guid targetID, List<ProgramNode> pathSoFar)
        {
            var newPath = new List<ProgramNode>(pathSoFar) { current };
            if (current.Id == targetID)
            {
                return newPath;
            }

            if (current is Group group)
            {
                foreach (var child in group.Children)
                {
                    var result = FindPath(child, targetID, newPath);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }
        private bool IsValidPrerequisite(Guid dependentNodeId, Guid prerequisiteId, Group root)
        {
            var pathToDependent = FindPath(root, dependentNodeId, new List<ProgramNode>());
            var pathToPrerequisite = FindPath(root, prerequisiteId, new List<ProgramNode>());

            if (pathToPrerequisite == null || pathToDependent == null)
            {
                return false;
            }

            if (dependentNodeId == prerequisiteId)
            {
                return false;
            }

            var minLength = Math.Min(pathToDependent.Count, pathToPrerequisite.Count);
            var divergedAt = -1;

            for(var i = 0; i < minLength; i++)
            {
                if (pathToDependent[i].Id != pathToPrerequisite[i].Id)
                {
                    divergedAt = i;
                    break;
                }
            }

            if(divergedAt == -1)
            {
                return false;
            }

            var sharedParent = pathToDependent[divergedAt - 1] as Group;
            if(sharedParent == null)
            {
                return false;
            }

            var dependentBranch = pathToDependent[divergedAt];
            var prerequisiteBranch = pathToPrerequisite[divergedAt];

            var dependentIndex = sharedParent.Children.IndexOf(dependentBranch);
            var prerequisiteIndex = sharedParent.Children.IndexOf(prerequisiteBranch);

            if(sharedParent.Rule == GroupRule.Choice)
            {
                return false;
            }

            return prerequisiteIndex < dependentIndex;
        }
        private bool IsGuaranteedReachable(Guid targetId, Group root)
        {
            var path = FindPath(root, targetId, new List<ProgramNode>());
            if (path == null)
            {
                return false;
            }

            //checking if next not is inside a Choice group.
            for (var i = 0; i < path.Count - 1; i++)
            {
                if (path[i] is Group group && group.Rule == GroupRule.Choice)
                {

                    return false;
                }
            }

            return true;
        }
        private IEnumerable<ProgramNode> GetAllNodes(ProgramNode node)
        {
            yield return node;

            if (node is Group group)
            {
                foreach (var child in group.Children)
                {
                    foreach (var descendant in GetAllNodes(child))
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }
}
