using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using messaging.Models;
using messaging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace messaging.Controllers
{
    [Route("status")]
    [ApiController]
    public class Status : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        protected readonly ILogger<Status> _logger;

        public Status(ILogger<Status> logger, ApplicationDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Status
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // Look up total number of processed and queued messages
                int processedCount = _context.IncomingMessageItems.Count(message => message.ProcessedStatus == "PROCESSED");
                int queuedCount = _context.IncomingMessageItems.Count(message => message.ProcessedStatus == "QUEUED");

                // Look up oldest and newest queued messages
                DateTime? oldestQueued = _context.IncomingMessageItems
                    .Where(message => message.ProcessedStatus == "QUEUED")
                    .OrderBy(message => message.CreatedDate)
                    .Select(message => message.CreatedDate)
                    .FirstOrDefault();
                DateTime? newestQueued = _context.IncomingMessageItems
                    .Where(message => message.ProcessedStatus == "QUEUED")
                    .OrderByDescending(message => message.CreatedDate)
                    .Select(message => message.CreatedDate)
                    .FirstOrDefault();

                // How many message have we processed in the past 5 minutes and 1 hour?
                // Note that we make the assumption that UpdatedDate will be when state was changed to PROCESSED
                int processedCountFiveMinutes = _context.IncomingMessageItems
                    .Count(message => message.ProcessedStatus == "PROCESSED" && message.UpdatedDate >= DateTime.UtcNow.AddMinutes(-5));
                int processedCountOneHour = _context.IncomingMessageItems
                    .Count(message => message.ProcessedStatus == "PROCESSED" && message.UpdatedDate >= DateTime.UtcNow.AddMinutes(-60));

                // How many messages have we queued in the past 5 minutes and 1 hour?
                // TODO: Confirm that UTC is correct
                int queuedCountFiveMinutes = _context.IncomingMessageItems
                    .Count(message => message.ProcessedStatus == "QUEUED" && message.CreatedDate >= DateTime.UtcNow.AddMinutes(-5));
                int queuedCountOneHour = _context.IncomingMessageItems
                    .Count(message => message.ProcessedStatus == "QUEUED" && message.CreatedDate >= DateTime.UtcNow.AddMinutes(-60));

                // Now do the above grouped by jurisdiction
                var jurisdictionResults = _context.IncomingMessageItems
                    .GroupBy(message => message.JurisdictionId)
                    .Select(group => new {
                            JurisdictionId = group.Key,
                            ProcessedCount = group.Count(message => message.ProcessedStatus == "PROCESSED"),
                            QueuedCount = group.Count(message => message.ProcessedStatus == "QUEUED"),
                            // TODO: These dates won't automatically be converted to null
                            OldestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                                .OrderBy(message => message.CreatedDate)
                                                .Select(message => message.CreatedDate)
                                                .FirstOrDefault(),
                            NewestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                                .OrderByDescending(message => message.CreatedDate)
                                                .Select(message => message.CreatedDate)
                                                .FirstOrDefault(),
                            ProcessedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                               message.UpdatedDate >= DateTime.UtcNow.AddMinutes(-5)),
                            ProcessedCountOneHour = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                           message.UpdatedDate >= DateTime.UtcNow.AddMinutes(-60)),
                            QueuedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                            message.CreatedDate >= DateTime.UtcNow.AddMinutes(-5)),
                            QueuedCountOneHour = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                        message.CreatedDate >= DateTime.UtcNow.AddMinutes(-60)),
                        })
                    .ToList();

                // Create the JSON result
                var result = new
                {
                    processedCount,
                    queuedCount,
                    oldestQueued,
                    newestQueued,
                    processedCountFiveMinutes,
                    processedCountOneHour,
                    queuedCountFiveMinutes,
                    queuedCountOneHour,
                    jurisdictionResults
                };

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while preparing status information: {ex}");
                return StatusCode(500);
            }

        }

    }
}
