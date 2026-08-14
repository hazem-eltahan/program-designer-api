using ProgramDesigner.Api.Models;
using ProgramDesigner.Api.Services;

namespace ProgramDesigner.Tests
{
    public class ValidationServiceTests
    {
        private readonly ValidationService _service = new();

        // Builds the full "Computer Science" program from the challenge spec.
        // Used as the base scenario for multiple tests below, since it exercises
        // nested groups, both InOrder and Choice rules, and cross-branch prerequisites.
        private Group BuildComputerScienceProgram()
        {
            // --- Foundations: a simple InOrder group with two plain steps ---
            var introComputing = new Step { Id = Guid.NewGuid(), Name = "Introduction to Computing", StepType = "Session" };
            var mathComputing = new Step { Id = Guid.NewGuid(), Name = "Mathematics for Computing", StepType = "Session" };
            var foundations = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Foundations",
                Rule = GroupRule.InOrder,
                Children = new List<ProgramNode> { introComputing, mathComputing }
            };

            // --- AI branch: contains a nested Choice group (Electives) and a
            //     capstone step whose prerequisite points inside that Choice group.
            //     This is the pairing that should trigger a reachability WARNING,
            //     not a rejection - see Validate_PrerequisiteInsideChoiceBranch_GeneratesWarning ---
            var mlBasics = new Step { Id = Guid.NewGuid(), Name = "Machine Learning Basics", StepType = "Session" };
            var computerVision = new Step { Id = Guid.NewGuid(), Name = "Computer Vision", StepType = "Session" };
            var nlp = new Step { Id = Guid.NewGuid(), Name = "Natural Language Processing", StepType = "Session" };
            var robotics = new Step { Id = Guid.NewGuid(), Name = "Robotics", StepType = "Session" };
            var electives = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Electives",
                Rule = GroupRule.Choice,     // pick 2 of 3 - participants may skip any one of these
                ChoiceCount = 2,
                Children = new List<ProgramNode> { computerVision, nlp, robotics }
            };
            var aiCapstone = new Step
            {
                Id = Guid.NewGuid(),
                Name = "AI Capstone",
                StepType = "Test",
                PrerequisiteId = electives.Id   // <- prerequisite target lives inside a Choice group
            };
            var ai = new Group
            {
                Id = Guid.NewGuid(),
                Name = "AI",
                Rule = GroupRule.InOrder,
                Children = new List<ProgramNode> { mlBasics, electives, aiCapstone }
            };

            // --- IT branch: plain InOrder group, no prerequisites of its own ---
            var networksSecurity = new Step { Id = Guid.NewGuid(), Name = "Networks & Security", StepType = "Session" };
            var sysAdmin = new Step { Id = Guid.NewGuid(), Name = "Systems Administration", StepType = "Session" };
            var it = new Group
            {
                Id = Guid.NewGuid(),
                Name = "IT",
                Rule = GroupRule.InOrder,
                Children = new List<ProgramNode> { networksSecurity, sysAdmin }
            };

            // --- Programming branch: plain InOrder group, no prerequisites of its own ---
            var algorithms = new Step { Id = Guid.NewGuid(), Name = "Algorithms & Data Structures", StepType = "Session" };
            var softwareEng = new Step { Id = Guid.NewGuid(), Name = "Software Engineering", StepType = "Session" };
            var programming = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Programming",
                Rule = GroupRule.InOrder,
                Children = new List<ProgramNode> { algorithms, softwareEng }
            };

            // --- Major: a Choice group (pick 1 of AI/IT/Programming), with a
            //     prerequisite on Foundations. This prerequisite is a plain sibling
            //     reference (Foundations comes before Major at the root level) -
            //     nothing choice-related about this specific prerequisite itself. ---
            var major = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Major",
                Rule = GroupRule.Choice,
                ChoiceCount = 1,
                PrerequisiteId = foundations.Id,
                Children = new List<ProgramNode> { ai, it, programming }
            };

            // --- Final Capstone: prerequisite points at Major ITSELF (not a branch
            //     inside Major). This is the "safe" Choice-adjacent case - finishing
            //     ANY one branch of Major satisfies this, so no warning should fire. ---
            var finalCapstone = new Step
            {
                Id = Guid.NewGuid(),
                Name = "Final Capstone",
                StepType = "Test",
                PrerequisiteId = major.Id
            };

            // --- Root: the whole program is just a top-level InOrder group ---
            return new Group
            {
                Id = Guid.NewGuid(),
                Name = "Computer Science",
                Rule = GroupRule.InOrder,
                Children = new List<ProgramNode> { foundations, major, finalCapstone }
            };
        }

        // Scenario: a single step whose PrerequisiteId points at its own Id.
        // This is the simplest "impossible prerequisite" case from the spec
        // (a step/group pointing at itself) and should always be rejected.
        [Fact]
        public void Validate_SelfReferencingPrerequisite_IsRejected()
        {
            var stepId = Guid.NewGuid();
            var step = new Step { Id = stepId, Name = "Self Referencing Step", StepType = "Test", PrerequisiteId = stepId };
            var root = new Group { Id = Guid.NewGuid(), Name = "Root", Rule = GroupRule.InOrder, Children = new List<ProgramNode> { step } };

            var result = _service.Validate(root);

            Assert.False(result.IsValid);
            Assert.Single(result.ImpossiblePrerequisites);
        }

        // Scenario: the full, correctly-structured Computer Science program from
        // the spec. Confirms every prerequisite in it is structurally valid -
        // i.e. nothing is flagged as an impossible/self-referencing/forward-pointing
        // prerequisite. (It's still expected to produce ONE reachability warning -
        // see the next test - warnings don't affect IsValid.)
        [Fact]
        public void Validate_ComputerScienceScenario_HasNoImpossiblePrerequisites()
        {
            var program = BuildComputerScienceProgram();

            var result = _service.Validate(program);

            Assert.True(result.IsValid);
            Assert.Empty(result.ImpossiblePrerequisites);
        }

        // Scenario: AI Capstone's prerequisite (Electives) sits inside a Choice
        // group (Major) that a participant might never enter (if they pick IT or
        // Programming instead of AI). This should generate a WARNING, not a
        // rejection - the program is still considered valid overall.
        [Fact]
        public void Validate_PrerequisiteInsideChoiceBranch_GeneratesWarning()
        {
            var program = BuildComputerScienceProgram();

            var result = _service.Validate(program);

            Assert.True(result.IsValid); // warnings don't invalidate the program
            Assert.Single(result.ReachabilityWarnings);
            Assert.Contains("AI Capstone", result.ReachabilityWarnings[0].Description);
        }

        // Scenario: two sibling steps whose prerequisites point at each other
        // (A requires B, B requires A) - a direct cycle. At least one direction
        // of this pair is a forward reference (depends on something that comes
        // later), so it must be rejected.
        [Fact]
        public void Validate_DirectPrerequisiteCycle_IsRejected()
        {
            var stepAId = Guid.NewGuid();
            var stepBId = Guid.NewGuid();

            var stepA = new Step { Id = stepAId, Name = "Step A", StepType = "Test", PrerequisiteId = stepBId };
            var stepB = new Step { Id = stepBId, Name = "Step B", StepType = "Test", PrerequisiteId = stepAId };

            var root = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Root",
                Rule = GroupRule.InOrder,
                Children = new List<ProgramNode> { stepA, stepB }
            };

            var result = _service.Validate(root);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.ImpossiblePrerequisites);
        }
    }
}