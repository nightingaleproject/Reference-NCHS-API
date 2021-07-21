using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NVSSMessaging.Models;
using NVSSMessaging.Services;
using VRDR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace NVSSMessaging.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BundlesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider Services;

        public BundlesController(ApplicationDbContext context, IServiceProvider services)
        {
            _context = context;
            Services = services;
        }

        // GET: Bundles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OutgoingMessageItem>>> GetOutgoingMessageItems(DateTime lastUpdated = default(DateTime))
        {
            return await _context.OutgoingMessageItems.Where(message => message.CreatedDate >= lastUpdated).ToListAsync();
        }

        // GET: Bundles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IncomingMessageItem>> GetIncomingMessageItem(long id)
        {
            var IncomingMessageItem = await _context.IncomingMessageItems.FindAsync(id);

            if (IncomingMessageItem == null)
            {
                return NotFound();
            }

            return IncomingMessageItem;
        }

        // POST: Bundles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public ActionResult PostIncomingMessageItem([FromBody] object text, [FromServices] IBackgroundTaskQueue queue)
        {
            // Check page 35 of the messaging document for full flow
            // Change over to 1 entry in the database per message
            try {
                BaseMessage message = BaseMessage.Parse(text.ToString(), true);
                IncomingMessageItem item = new IncomingMessageItem();
                item.Message = text.ToString();
                item.MessageId = message.MessageId;
                _context.IncomingMessageItems.Add(item);
                _context.SaveChanges();

                _ = queue.QueueBackgroundWorkItemAsync(token => ConvertToIJE(token, new ValueTask<long>(item.Id)));
            } catch {
                return BadRequest();
            }

            // return HTTP status code 204 (No Content)
            return NoContent();
        }

        public async ValueTask<long> ConvertToIJE(CancellationToken token, ValueTask<long> id)
        {
            // TODO: Something this throws a "Cannot access a disposed object" exception
            // Potentially need to move this into it's own Service to deal with this.
            using(var scope = Services.CreateScope()) {
                var _database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                IncomingMessageItem item = _database.IncomingMessageItems.Find(id.Result);
                BaseMessage message = BaseMessage.Parse(item.Message.ToString(), true);
                IJEItem ijeItem = new IJEItem();
                OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
                try {
                    switch(message) {
                        case DeathRecordUpdate update:
                            HandleUpdateMessage(_database, update);
                        break;
                        case DeathRecordSubmission submission:
                            HandleSubmissionMessage(_database, submission);
                        break;
                    }
                } catch {
                    ExtractionErrorMessage errorMessage = new ExtractionErrorMessage(message);
                    outgoingMessageItem.Message = errorMessage.ToJSON();
                    _database.OutgoingMessageItems.Add(outgoingMessageItem);
                }
                await _database.SaveChangesAsync();

                return id.Result;
            }
        }

        private void HandleSubmissionMessage(ApplicationDbContext _database, DeathRecordSubmission message) {
            if(!IncomingMessageLogItemExists(_database, message.MessageId)) {
                IJEItem ijeItem = new IJEItem();
                ijeItem.MessageId = message.MessageId;
                ijeItem.IJE = new IJEMortality(message.DeathRecord).ToString();
                // Log and ack message right after it is successfully extracted
                CreateAckMessage(_database, message);
                LogMessage(_database, message);
                _database.IJEItems.Add(ijeItem);
                _database.SaveChanges();
            }
        }

        private void HandleUpdateMessage(ApplicationDbContext _database, DeathRecordUpdate message) {

        }

        private void LogMessage(ApplicationDbContext _database, DeathRecordSubmission message) {
                IncomingMessageLog entry = new IncomingMessageLog();
                entry.MessageTimestamp = message.MessageTimestamp;
                entry.MessageId = message.MessageId;
                entry.CertificateNumber = message.CertificateNumber;
                entry.StateAuxiliaryIdentifier = message.StateAuxiliaryIdentifier;
                _database.IncomingMessageLogs.Add(entry);
                _database.SaveChanges();
        }

        private void CreateAckMessage(ApplicationDbContext _database, BaseMessage message) {
            OutgoingMessageItem outgoingMessageItem = new OutgoingMessageItem();
            AckMessage ackMessage = new AckMessage(message);
            outgoingMessageItem.Message = ackMessage.ToJSON();
            outgoingMessageItem.MessageId = ackMessage.MessageId;
            _database.OutgoingMessageItems.Add(outgoingMessageItem);
            _database.SaveChanges();
        }

        private bool IncomingMessageLogItemExists(ApplicationDbContext context, string messageId)
        {
            return context.IncomingMessageLogs.Any(l => l.MessageId == messageId);
        }

        private bool IncomingMessageItemExists(long id)
        {
            return _context.IncomingMessageItems.Any(e => e.Id == id);
        }
    }
}
