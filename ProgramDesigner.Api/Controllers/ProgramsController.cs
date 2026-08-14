using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProgramDesigner.Api.DTOs;
using ProgramDesigner.Api.Services;

namespace ProgramDesigner.Api.Controllers
{
    [Route("programs")]
    [ApiController]
    public class ProgramsController : ControllerBase
    {
        private readonly IProgramStore _store;
        private readonly ProgramNodeConverter _programNodeConverter;
        public ProgramsController(IProgramStore store, ProgramNodeConverter programNodeConverter)
        {
            _store = store;
            _programNodeConverter = programNodeConverter;
        }

        [HttpPost]
        public IActionResult CreateProgram([FromBody] GroupDto groupDto)
        {
            var group = _programNodeConverter.Convert(groupDto);
            _store.Save(group);
            return Ok(group);
        }

        [HttpGet("{id}")]
        public IActionResult GetProgram(Guid id)
        {
            if (_store.TryGetProgram(id, out var program))
            {
                return Ok(program);
            }
            else
                return NotFound();
        }
    }
}
