using Hl7.Fhir.Model;
using messaging.Models;
using messaging.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
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
        protected override IEnumerable<OutgoingMessageItem> ExcludeRetrieved(IEnumerable<OutgoingMessageItem> source)
        {
            return source.Where(message => message.SteveRetrievedAt == null);
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
            IncomingMessageItem item;
            try
            {
                item = ParseIncomingMessageItem(jurisdictionId, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred while parsing the incoming message: {ex}");
                return BadRequest();
            }

            // Mark the item as coming from STEVE rather than directly from the jurisdiction
            item.Source = "STV";

            try
            {
                SaveIncomingMessageItem(item, queue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred while saving the incoming message: {ex}");
                return StatusCode(500);
            }

            // return HTTP status code 204 (No Content)
            return NoContent();
        }


    }
}
