using ProgramDesigner.Api.Models;

namespace ProgramDesigner.Api.Services
{
    public interface IProgramStore
    {
        void Save(Group program);
        bool TryGetProgram(Guid id, out Group? program);
    }
}
