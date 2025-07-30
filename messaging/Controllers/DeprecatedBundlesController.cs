using System;
using System.Threading.Tasks;
using messaging.Models;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using messaging.Services;

namespace messaging.Controllers
{
    [Route("{jurisdictionId:length(2)}/Bundles")] // Historical endpoint for backwards compatibility
    [Produces("application/json")]
    [ApiController]
    public class DeprecatedBundlesController : BundleController
    {
        public DeprecatedBundlesController(ILogger<BundleController> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings) : base(logger, context, services, settings) { }

        /// <summary>
        /// (Deprecated) Retrieves outgoing messages for the jurisdiction.
        /// If the optional Certificate Number and Death year parameters are provided, retrieves all messages in history that match those given business ids.
        /// </summary>
        /// <returns>A Bundle of FHIR messages</returns>
        /// <response code="200">Content retrieved successfully</response>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Error, token may have expired</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public override async Task<ActionResult<Bundle>> GetOutgoingMessageItems(string jurisdictionId, string vitalType, string igVersion, int _count, string certificateNumber, string eventYear, string deathYear, DateTime _since = default(DateTime), int page = 1)
        {
            return await base.GetOutgoingMessageItems(jurisdictionId, vitalType, igVersion, _count, certificateNumber, eventYear, deathYear, _since, page);
        }

        // POST: Bundles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        /// <summary>
        /// (Deprecated) Submits a FHIR message to the API for processing.
        /// </summary>
        /// <returns>If a single FHIR Message was submitted, nothing is returned. If a batch Bundle was submitted, a batch response is returned.</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST
        ///
        ///     {
        ///         "resourceType": "Bundle",
        ///         "id": "bffdbf2e-c0db-49cf-9f52-59a6459635b9",
        ///         "type": "message",
        ///         "timestamp": "2022-07-27T15:30:39.5787234+00:00",
        ///         "entry": [
        ///             { ...
        ///             }
        ///         ]
        ///     }
        ///
        /// </remarks>
        /// <response code="204">Content created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Error, token may have expired</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public override async Task<ActionResult<Bundle>> PostIncomingMessageItem(string jurisdictionId, string vitalType, string igVersion, [FromBody] object text, [FromServices] IBackgroundTaskQueue queue)
        {
            return await base.PostIncomingMessageItem(jurisdictionId, vitalType, igVersion, text, queue);
        }
    }
}
