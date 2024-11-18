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
using Microsoft.Extensions.Options;
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
        protected readonly AppSettings _settings;

        public Status(ILogger<Status> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings)
        {
            _context = context;
            _logger = logger;
            _settings = settings.Value;
        }

        // GET: Status
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // Some relative times we use repeatedly
                DateTime fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
                DateTime oneHourAgo = DateTime.UtcNow.AddMinutes(-60);

                // Get overall statistics across all jurisdictions
                var overallResults = _context.IncomingMessageItems
                    .GroupBy(message => 1) // Group by a constant to get overall results
                    .Select(group => new {
                        // Look up total number of processed and queued messages
                        ProcessedCount = group.Count(message => message.ProcessedStatus == "PROCESSED"),
                        QueuedCount = group.Count(message => message.ProcessedStatus == "QUEUED"),
                        // Look up oldest and newest queued messages
                        OldestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderBy(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        NewestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderByDescending(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        // Look up the most recently processed message
                        LatestProcessed = group.Where(message => message.ProcessedStatus == "PROCESSED")
                                               .OrderByDescending(message => message.UpdatedDate)
                                               .Select(message => message.UpdatedDate)
                                               .FirstOrDefault(),
                        // How many message have we processed in the past 5 minutes and 1 hour?
                        // Note that we make the assumption that UpdatedDate will be when state was changed to PROCESSED
                        ProcessedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                           message.UpdatedDate >= fiveMinutesAgo),
                        ProcessedCountOneHour = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                       message.UpdatedDate >= oneHourAgo),
                        // How many messages have we queued in the past 5 minutes and 1 hour?
                        QueuedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                        message.CreatedDate >= fiveMinutesAgo),
                        QueuedCountOneHour = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                    message.CreatedDate >= oneHourAgo),
                        })
                    .FirstOrDefault();

                // Now do the above grouped by source
                var sourceResults = _context.IncomingMessageItems
                    .GroupBy(message => message.Source)
                    .Select(group => new {
                        Source = group.Key,
                        ProcessedCount = group.Count(message => message.ProcessedStatus == "PROCESSED"),
                        QueuedCount = group.Count(message => message.ProcessedStatus == "QUEUED"),
                        OldestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderBy(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        NewestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderByDescending(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        LatestProcessed = group.Where(message => message.ProcessedStatus == "PROCESSED")
                                               .OrderByDescending(message => message.UpdatedDate)
                                               .Select(message => message.UpdatedDate)
                                               .FirstOrDefault(),
                        ProcessedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                           message.UpdatedDate >= fiveMinutesAgo),
                        ProcessedCountOneHour = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                       message.UpdatedDate >= oneHourAgo),
                        QueuedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                        message.CreatedDate >= fiveMinutesAgo),
                        QueuedCountOneHour = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                    message.CreatedDate >= oneHourAgo),
                        })
                    .ToList();

                // Now do the above grouped by EventType
                var eventTypeResults = _context.IncomingMessageItems
                    .GroupBy(message => message.EventType)
                    .Select(group => new {
                        EventType = group.Key,
                        ProcessedCount = group.Count(message => message.ProcessedStatus == "PROCESSED"),
                        QueuedCount = group.Count(message => message.ProcessedStatus == "QUEUED"),
                        OldestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderBy(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        NewestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderByDescending(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        LatestProcessed = group.Where(message => message.ProcessedStatus == "PROCESSED")
                                               .OrderByDescending(message => message.UpdatedDate)
                                               .Select(message => message.UpdatedDate)
                                               .FirstOrDefault(),
                        ProcessedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                           message.UpdatedDate >= fiveMinutesAgo),
                        ProcessedCountOneHour = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                       message.UpdatedDate >= oneHourAgo),
                        QueuedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                        message.CreatedDate >= fiveMinutesAgo),
                        QueuedCountOneHour = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                    message.CreatedDate >= oneHourAgo),
                        })
                    .ToList();

                // Now do the above grouped by jurisdiction
                var jurisdictionResults = _context.IncomingMessageItems
                    .GroupBy(message => message.JurisdictionId)
                    .Select(group => new {
                        JurisdictionId = group.Key,
                        ProcessedCount = group.Count(message => message.ProcessedStatus == "PROCESSED"),
                        QueuedCount = group.Count(message => message.ProcessedStatus == "QUEUED"),
                        OldestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderBy(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        NewestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                                            .OrderByDescending(message => message.CreatedDate)
                                            .Select(message => message.CreatedDate)
                                            .FirstOrDefault(),
                        LatestProcessed = group.Where(message => message.ProcessedStatus == "PROCESSED")
                                               .OrderByDescending(message => message.UpdatedDate)
                                               .Select(message => message.UpdatedDate)
                                               .FirstOrDefault(),
                        ProcessedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                           message.UpdatedDate >= fiveMinutesAgo),
                        ProcessedCountOneHour = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                                                                       message.UpdatedDate >= oneHourAgo),
                        QueuedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                        message.CreatedDate >= fiveMinutesAgo),
                        QueuedCountOneHour = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                                                                    message.CreatedDate >= oneHourAgo),
                        })
                    .ToList();

                string ApiEnvironment = _settings.Environment;

                // Create the JSON result
                var result = new
                {
                    ApiEnvironment,
                    overallResults.ProcessedCount,
                    overallResults.QueuedCount,
                    overallResults.OldestQueued,
                    overallResults.NewestQueued,
                    overallResults.LatestProcessed,
                    overallResults.ProcessedCountFiveMinutes,
                    overallResults.ProcessedCountOneHour,
                    overallResults.QueuedCountFiveMinutes,
                    overallResults.QueuedCountOneHour,
                    sourceResults,
                    eventTypeResults,
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
