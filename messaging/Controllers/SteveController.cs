using Hl7.Fhir.Model;
using messaging.Models;
using messaging.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace messaging.Controllers
{
    [Route("STEVE/{jurisdictionId}/Bundles")]
    [ApiController]
    public class SteveController : BundlesController
    {

        public SteveController(ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings) : base(context, services, settings) { }

        // GET: /STEVE/{jurisdictionId}/Bundles
        [HttpGet]
        public async Task<ActionResult<Bundle>> GetOutgoingMessageItemsForSteve(string jurisdictionId, DateTime _since = default(DateTime))
        {
            return await base.GetOutgoingMessageItems(jurisdictionId, _since);
        }
        protected override void MarkAsRetrieved(OutgoingMessageItem omi, DateTime retrieved)
        {
            omi.SteveRetrievedAt = retrieved;
        }

        // GET: /STEVE/{jurisdictionId}/Bundles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<IncomingMessageItem>> GetIncomingMessageItemForSteve(string jurisdictionId, long id)
        {
            return await base.GetIncomingMessageItem(jurisdictionId, id);
        }

        // POST: /STEVE/{jurisdictionId}/Bundles
        [HttpPost]
        public ActionResult PostIncomingMessageItemForSteve(string jurisdictionId, [FromBody] object text, [FromServices] IBackgroundTaskQueue queue)
        {
            // TODO: NVSS-365: Mark message as coming from STEVE
            // Return 501 Not Implemented for now
            return StatusCode(501);
            //return await base.PostIncomingMessageItem(jurisdictionId, text, queue);
        }


    }
}
