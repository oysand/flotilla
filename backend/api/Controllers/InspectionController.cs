using Api.Controllers.Models;
using Api.Database.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("inspection")]
    public class InspectionController(
            ILogger<InspectionController> logger,
            IInspectionService inspectionService
        ) : ControllerBase
    {
        /// <summary>
        /// Get the inspection data
        /// </summary>
        /// <remarks>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.Admin)]
        [Route("{id}/data")]
        [ProducesResponseType(typeof(Inspection), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Inspection>> GetInspectionData([FromRoute] string id)
        {
            logger.LogInformation("Get inspection by ID '{id}'", id);
            try
            {
                var inspection = await inspectionService.ReadByIsarTaskId(id, readOnly: true);
                if (inspection != null)
                {
                    return Ok(inspection);
                }

            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while finding an inspection with inspection id '{id}'", id);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            return NotFound("Could not find any inspection with the provided '{id}'");
        }

    }


}
