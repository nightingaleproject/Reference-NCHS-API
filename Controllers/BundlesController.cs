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
        public async Task<ActionResult<IEnumerable<FHIRMessageItem>>> GetFHIRMessageItems()
        {
            return await _context.FHIRMessageItems.ToListAsync();
        }

        // GET: Bundles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FHIRMessageItem>> GetFHIRMessageItem(long id)
        {
            var fHIRMessageItem = await _context.FHIRMessageItems.FindAsync(id);

            if (fHIRMessageItem == null)
            {
                return NotFound();
            }

            return fHIRMessageItem;
        }

        // POST: Bundles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public ActionResult PostFHIRMessageItem([FromBody] object text, [FromServices] IBackgroundTaskQueue queue)
        {
            // Check page 35 of the messaging document for full flow
            // Change over to 1 entry in the database per message
            // TODO: If extraction fails create 'extraction error' for message, and send extraction error message
            BaseMessage message = BaseMessage.Parse(text.ToString(), true);
            FHIRMessageItem item = new FHIRMessageItem();
            item.Message = text.ToString();
            // TODO: Ignore message if it is one we have seen before
            _context.FHIRMessageItems.Add(item);
            _context.SaveChanges();
            ValueTask<long> valueTask = new ValueTask<long>(item.Id);

            _ = queue.QueueBackgroundWorkItemAsync(token => ConvertToIJE(token, valueTask));

            // return HTTP status code 204 (No Content)
            return NoContent();
        }

        public async ValueTask<long> ConvertToIJE(CancellationToken token, ValueTask<long> id)
        {
            using(var scope = Services.CreateScope()) {
                var _database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                FHIRMessageItem item = _database.FHIRMessageItems.Find(id.Result);
                DeathRecordSubmission message = BaseMessage.Parse<DeathRecordSubmission>(item.Message.ToString(), true);
                IJEItem ijeItem = new IJEItem();
                ijeItem.IJE = new IJEMortality(message.DeathRecord).ToString();
                _database.Update(ijeItem);
                await _database.SaveChangesAsync();
                return id.Result;
            }
        }

        private bool FHIRMessageItemExists(long id)
        {
            return _context.FHIRMessageItems.Any(e => e.Id == id);
        }
    }
}
