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
        private readonly ValidationService _validationService;

        public ProgramsController(IProgramStore store, ProgramNodeConverter programNodeConverter, ValidationService validationService)
        {
            _store = store;
            _programNodeConverter = programNodeConverter;
            _validationService = validationService;
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

        [HttpPost("{id}/validate")]
        public IActionResult ValidateProgram(Guid id)
        {
            if(!_store.TryGetProgram(id, out var program))
            {
                return NotFound();
            }
            
            var result = _validationService.Validate(program!);
            return Ok(result);

        }
    }
}
