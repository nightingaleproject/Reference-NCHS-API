using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using messaging.Models;
using messaging.Services;
using Hl7.Fhir.Model;
using VRDR;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace messaging.Controllers
{
    [Route("{jurisdictionId}/[controller]")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider Services;
        private readonly AppSettings _settings;

        public BundlesController(ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings)
        {
            _context = context;
            Services = services;
            _settings = settings.Value;
        }

        // GET: Bundles
        [HttpGet]
        public async Task<ActionResult<Bundle>> GetOutgoingMessageItems(string jurisdictionId, DateTime lastUpdated = default(DateTime))
        {
            var messageTasks = _context.OutgoingMessageItems.Where(message => message.CreatedDate >= lastUpdated && message.JurisdictionId == jurisdictionId).ToList()
                                                            .Select(message => System.Threading.Tasks.Task.Run(() => BaseMessage.Parse(message.Message, true)));
            Bundle responseBundle = new Bundle();
            responseBundle.Type = Bundle.BundleType.Searchset;
            responseBundle.Timestamp = DateTime.Now;
            var messages = await System.Threading.Tasks.Task.WhenAll(messageTasks);
            foreach (var message in messages)
            {
                responseBundle.AddResourceEntry((Bundle)message, "urn:uuid:" + message.MessageId);
            }
            return responseBundle;
        }

        // GET: Bundles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IncomingMessageItem>> GetIncomingMessageItem(string jurisdictionId, long id)
        {
            var IncomingMessageItem = await _context.IncomingMessageItems.Where(x => x.Id == id && x.JurisdictionId == jurisdictionId).FirstAsync();

            if (IncomingMessageItem == null)
            {
                return NotFound();
            }

            return IncomingMessageItem;
        }

        // POST: Bundles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public ActionResult PostIncomingMessageItem(string jurisdictionId, [FromBody] object text, [FromServices] IBackgroundTaskQueue queue)
        {
            // Check page 35 of the messaging document for full flow
            // Change over to 1 entry in the database per message
            try {
                BaseMessage message = BaseMessage.Parse(text.ToString());
                IncomingMessageItem item = new IncomingMessageItem();
                item.Message = text.ToString();
                item.MessageId = message.MessageId;
                item.MessageType = message.GetType().Name;
                item.JurisdictionId = jurisdictionId;
                item.EventYear = message.DeathYear;
                if (message.CertificateNumber == null)
                {
                    item.CertificateNumber = null;
                }
                else
                {
                    uint certNo = (uint)message.CertificateNumber;
                    string certNoFmt = certNo.ToString("D6");
                    item.CertificateNumber = certNoFmt;
                }
                item.EventType = getEventType(message);
                _context.IncomingMessageItems.Add(item);
                _context.SaveChanges();

                if(_settings.AckAndIJEConversion) {
                    queue.QueueConvertToIJE(item.Id);
                }
            } catch {
                return BadRequest();
            }

            // return HTTP status code 204 (No Content)
            return NoContent();
        }

        // getEventType generates an EventType string "MOR", "NAT", or "FET"
        // for debugging and tracking records in the db
        // For now we only have "MOR" records but we could add "NAT" and "FET" here later
        private string getEventType(BaseMessage message)
        {
            switch (message.MessageType)
            {
                case "http://nchs.cdc.gov/vrdr_submission":
                    return "MOR";
                    break;
                case "http://nchs.cdc.gov/vrdr_submission_update":
                    return "MOR";
                    break;
                case "http://nchs.cdc.gov/vrdr_acknowledgement":
                    return "MOR";
                    break;
                case "http://nchs.cdc.gov/vrdr_submission_void":
                    return "MOR";
                    break;
                case "http://nchs.cdc.gov/vrdr_coding":
                    return "MOR";
                    break;
                case "http://nchs.cdc.gov/vrdr_coding_update":
                    return "MOR";
                    break;
                case "http://nchs.cdc.gov/vrdr_extraction_error":
                    return "MOR";
                    break;
                default:
                    return "UNK";
            }
        }
    }
}
