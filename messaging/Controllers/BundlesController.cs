using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using messaging.Models;
using messaging.Services;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
using VRDR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace messaging.Controllers
{
    [Route("{jurisdictionId:length(2)}/Bundles")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider Services;
        private readonly AppSettings _settings;
        private readonly ILogger<BundlesController> _logger;

        public BundlesController(ILogger<BundlesController> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings)
        {
            _context = context;
            Services = services;
            _settings = settings.Value;
            _logger = logger;
        }

        // GET: Bundles
        [HttpGet]
        public async Task<ActionResult<Bundle>> GetOutgoingMessageItems(string jurisdictionId, DateTime _since = default(DateTime))
        {
            try
            {
                // Limit results to the jurisdiction's messages; note this just builds the query but doesn't execute until the result set is enumerated
                IEnumerable<OutgoingMessageItem> outgoingMessagesQuery = _context.OutgoingMessageItems.Where(message => message.JurisdictionId == jurisdictionId);

                // Further scope the search to either unretrieved messages (or all since a specific time)
                // TODO only allow the since param in development
                // if _since is the default value, then apply the retrieved at logic
                if (_since == default(DateTime))
                {
                    outgoingMessagesQuery = ExcludeRetrieved(outgoingMessagesQuery);
                }
                else
                {
                    outgoingMessagesQuery = outgoingMessagesQuery.Where(message => message.CreatedDate >= _since);
                }

                // Convert to list to execute the query, capture the result for re-use
                List<OutgoingMessageItem> outgoingMessages = outgoingMessagesQuery.ToList();

                // This uses the general FHIR parser and then sees if the json is a Bundle of BaseMessage Type
                // this will improve performance and prevent vague failures on the server, clients will be responsible for identifying incorrect messages
                IEnumerable<System.Threading.Tasks.Task<VRDR.BaseMessage>> messageTasks = outgoingMessages.Select(message => System.Threading.Tasks.Task.Run(() => BaseMessage.ParseGenericMessage(message.Message, true)));

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
                outgoingMessages.ForEach(msgItem => MarkAsRetrieved(msgItem, retrievedTime));
                _context.SaveChanges();
                return responseBundle;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while retrieving the response messages: {ex}");
                return StatusCode(500);
            }

        }

        // Allows overriding by STEVE controller to filter off different field
        /// <summary>
        /// Applies a filter (e.g. calls Where) to reduce the source to unretrieved messages. Should NOT iterate result set/execute query
        /// </summary>
        protected virtual IEnumerable<OutgoingMessageItem> ExcludeRetrieved(IEnumerable<OutgoingMessageItem> source)
        {
            return source.Where(message => message.RetrievedAt == null);
        }

        // Allows overriding by STEVE controller to mark different field
        protected virtual void MarkAsRetrieved(OutgoingMessageItem omi, DateTime retrieved)
        {
            omi.RetrievedAt = retrieved;
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
        public async Task<ActionResult<Bundle>> PostIncomingMessageItem(string jurisdictionId, [FromBody] object text, [FromServices] IBackgroundTaskQueue queue)
        {
            // Check page 35 of the messaging document for full flow
            // Change over to 1 entry in the database per message
            Bundle responseBundle = new Bundle();
            try {

                // check whether the bundle is a message or a batch
                Bundle bundle = BaseMessage.ParseGenericBundle(text.ToString(), true);
                if (bundle?.Type == Bundle.BundleType.Batch)
                {   
                    responseBundle = new Bundle();
                    responseBundle.Type = Bundle.BundleType.BatchResponse;
                    responseBundle.Timestamp = DateTime.Now;

                    // For Batch Processing: 
                    // Process each entry as an individual BaseMessage.
                    // One invalid message should not prevent the successful submission 
                    // of a separate, valid message in the bundle.
                    // Capture the each messsage's result in an entry and add to the response bundle.
                    foreach (var entry in bundle.Entry)
                    {
                        Bundle.EntryComponent respEntry = await InsertBatchMessages(entry, jurisdictionId, queue);
                        responseBundle.Entry.Add(respEntry);
                    }
                    return responseBundle;
                } 
                else
                {

                    IncomingMessageItem item;
                    try
                    {
                        item = ParseIncomingMessageItem(jurisdictionId, text);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"An exception occurred while parsing the incoming message: {ex}");
                        return BadRequest("Failed to parse message. Please verify that it is consistent with the current Vital Records Messaging FHIR Implementation Guide.");
                    }

                    // Pre-check some minimal requirements for validity. Specifically, if there are problems with the message that will lead to failure when
                    // attempting to insert into the database (e.g. missing MessageId), catch that here to return a 400 instead of a 500 on DB error
                    // Message errors SHOULD result in an ExtractionError response; this check is just to catch things that can't make it that far
                    if (item.MessageId == null)
                    {
                        _logger.LogDebug("Rejecting message with no MessageId");
                        return BadRequest("Message was missing required field MessageId");
                    }
                    if (item.MessageType == null)
                    {
                        _logger.LogDebug("Rejecting message with no MessageType.");
                        return BadRequest("Message was missing required field MessageType");
                    }

                    item.Source = GetMessageSource();

                    try
                    {
                        await SaveIncomingMessageItem(item, queue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"An exception occurred while saving the incoming message: {ex}");
                        return StatusCode(500);
                    }
                }

                // return HTTP status code 204 (No Content)
                return NoContent();
            } catch (Exception ex){
                Console.WriteLine($"An exception occurred while parsing the incoming message: {ex}");
                return BadRequest();
            }
        }

        // InsertBatchMessages handles a single message in a batch upload submission
        // Each message is handled independent of the other messages. A status code is generated for
        // each message and is returned in the response bundle
        private async Task<Bundle.EntryComponent> InsertBatchMessages( Bundle.EntryComponent msgBundle, string jurisdictionId, IBackgroundTaskQueue queue)
        {
            Bundle.EntryComponent entry = new Bundle.EntryComponent();
            entry.Resource = msgBundle.Resource;
            IncomingMessageItem item;

            try
            {
                BaseMessage message = BaseMessage.Parse<BaseMessage>((Hl7.Fhir.Model.Bundle)msgBundle.Resource);
                item = ParseIncomingMessageItem(jurisdictionId, message.ToJSON());
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while parsing the incoming message: {ex}");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "400";
                entry.Response.Etag = "W/1";
                entry.Response.LastModified = DateTime.UtcNow;      
                return entry;

            }

            // Pre-check some minimal requirements for validity. Specifically, if there are problems with the message that will lead to failure when
            // attempting to insert into the database (e.g. missing MessageId), catch that here to return a 400 instead of a 500 on DB error
	        // Message errors SHOULD result in an ExtractionError response; this check is just to catch things that can't make it that far
            if (item.MessageId == null)
            {
                _logger.LogDebug("Rejecting message with no MessageId");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "400";
                entry.Response.Etag = "W/1";
                entry.Response.LastModified = DateTime.UtcNow;      
                return entry;
            }
            if (item.MessageType == null)
            {
                _logger.LogDebug("Rejecting message with no MessageType.");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "400";
                entry.Response.Etag = "W/1";
                entry.Response.LastModified = DateTime.UtcNow;      
                return entry;
            }
            item.Source = GetMessageSource();
            try
            {
                await SaveIncomingMessageItem(item, queue);
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while saving the incoming message: {ex}");
                entry.Response = new Bundle.ResponseComponent();
                entry.Response.Status = "500";
                entry.Response.Etag = "W/1";
                entry.Response.LastModified = DateTime.UtcNow;   
                return entry;
            }

            entry.Response = new Bundle.ResponseComponent();
            entry.Response.Status = "201";
            entry.Response.Etag = "W/1";
            entry.Response.LastModified = DateTime.UtcNow;       
            return entry;

        }

        /// <summary>
        /// Get the value to use for the message Source (default is SAM). ALlows override by STEVE endpoint.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetMessageSource()
        {
            return "SAM";
        }

        protected IncomingMessageItem ParseIncomingMessageItem(string jurisdictionId, object text)
        {
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

            return item;
        }

        protected async System.Threading.Tasks.Task SaveIncomingMessageItem(IncomingMessageItem item, IBackgroundTaskQueue queue)
        {
            await _context.IncomingMessageItems.AddAsync(item);
            await _context.SaveChangesAsync();

            if (_settings.AckAndIJEConversion)
            {
                queue.QueueConvertToIJE(item.Id);
            }
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
