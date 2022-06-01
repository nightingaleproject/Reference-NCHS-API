using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        public async Task<ActionResult<Bundle>> GetOutgoingMessageItems(string jurisdictionId, DateTime _since = default(DateTime))
        {
            // TODO only allow the since param in development
            // if _since is the default value, then apply the retrieved at logic
            List<OutgoingMessageItem> outgoingMessages = new List<OutgoingMessageItem>();
            IEnumerable<System.Threading.Tasks.Task<VRDR.BaseMessage>> messageTasks;
            if (_since == default(DateTime))
            {
                // This uses the general FHIR parser and then sees if the json is a Bundle of BaseMessage Type
                // this will improve performance and prevent vague failures on the server, clients will be responsible for identifying incorrect messages
                outgoingMessages = _context.OutgoingMessageItems.Where(message => message.RetrievedAt == null && message.JurisdictionId == jurisdictionId).ToList();
                messageTasks = outgoingMessages.Select(message => System.Threading.Tasks.Task.Run(() => BaseMessage.ParseGenericMessage(message.Message, true))); 
            }
            else
            {
                outgoingMessages = _context.OutgoingMessageItems.Where(message => message.CreatedDate >= _since && message.JurisdictionId == jurisdictionId).ToList();
                messageTasks = outgoingMessages.Select(message => System.Threading.Tasks.Task.Run(() => BaseMessage.ParseGenericMessage(message.Message, true)));            
            }

            // create bundle to hold the response
            Bundle responseBundle = new Bundle();
            responseBundle.Type = Bundle.BundleType.Searchset;
            responseBundle.Timestamp = DateTime.Now;
            var messages = await System.Threading.Tasks.Task.WhenAll(messageTasks);
            DateTime retrievedTime = DateTime.UtcNow;
            
            // Add messages to the bundle
            foreach (var message in messages)
            {
                responseBundle.AddResourceEntry((Bundle)message, "urn:uuid:" + message.MessageId);
            }

            // update each outgoing message's RetrievedAt field
            outgoingMessages.ForEach(msgItem => msgItem.RetrievedAt = retrievedTime);
            _context.SaveChanges();
            return responseBundle;
        }

        // GET: Bundles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IncomingMessageItem>> GetIncomingMessageItem(string jurisdictionId, long id)
        {
            var IncomingMessageItem = await _context.IncomingMessageItems.Where(x => x.Id == id && x.JurisdictionId == jurisdictionId).FirstOrDefaultAsync();

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

                if (message.CertNo == null)
                {
                    item.CertificateNumber = null;
                }
                else
                {
                    uint certNo = (uint)message.CertNo;
                    string certNoFmt = certNo.ToString("D6");
                    item.CertificateNumber = certNoFmt;
                }
                item.EventType = getEventType(message);
                _context.IncomingMessageItems.Add(item);
                _context.SaveChanges();

                if(_settings.AckAndIJEConversion) {
                    queue.QueueConvertToIJE(item.Id);
                }
            } catch (Exception ex){
                Console.WriteLine($"An exception occurred while parsing the incoming message: {ex}");
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
                case "http://nchs.cdc.gov/vrdr_submission_update":
                case "http://nchs.cdc.gov/vrdr_acknowledgement":
                case "http://nchs.cdc.gov/vrdr_submission_void":
                case "http://nchs.cdc.gov/vrdr_coding":
                case "http://nchs.cdc.gov/vrdr_coding_update":
                case "http://nchs.cdc.gov/vrdr_extraction_error":
                    return "MOR";
                default:
                    return "UNK";
            }
        }
    }
}
