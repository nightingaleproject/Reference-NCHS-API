using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NVSSMessaging.Models;
using NVSSMessaging.Services;
using VRDR;

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
            // TODO: Return a FHIR bundle of messages instead of an array of FHIR messages
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

                queue.QueueConvertToIJE(item.Id);
            } catch {
                return BadRequest();
            }

            // return HTTP status code 204 (No Content)
            return NoContent();
        }
    }
}
