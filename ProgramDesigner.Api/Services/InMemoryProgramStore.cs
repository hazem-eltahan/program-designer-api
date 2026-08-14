using ProgramDesigner.Api.Models;

namespace ProgramDesigner.Api.Services
{
    public class InMemoryProgramStore : IProgramStore
    {
        private readonly Dictionary<Guid, Group> _programs = new();
        public void Save(Group program)
        {
            _programs[program.Id] = program;
        }

        public bool TryGetProgram(Guid id, out Group? program)
        {
            return _programs.TryGetValue(id, out program);
        }
    }
}
