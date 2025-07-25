using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using messaging.Models;

namespace status_api.Controllers
{
    [Route("api/v1/status")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        protected readonly ILogger<StatusController> _logger;
        protected readonly AppSettings _settings;

        public StatusController(ILogger<StatusController> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings)
        {
            _context = context;
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus(DateTime _since = default(DateTime))
        {
            try
            {
                // Some relative times we use repeatedly
                DateTime fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
                DateTime oneHourAgo = DateTime.UtcNow.AddMinutes(-60);

								var overallResults = _context.StatusOverallResults
										.FromSqlInterpolated($"EXEC GetOverallStatusWithParams @Since={_since}, @FiveMinutesAgo={fiveMinutesAgo}, @OneHourAgo={oneHourAgo}")
										.AsEnumerable<StatusOverallResults>()
										.FirstOrDefault();
								
								Console.WriteLine("==========================");
								Console.WriteLine(overallResults);
								Console.WriteLine("==========================");
								
                // // Get overall statistics across all jurisdictions
                // var overallResults = _context.IncomingMessageItems
                //     .Where(message => message.CreatedDate >= _since) // Only include messages as specified using _since
                //     .GroupBy(message => 1) // Group by a constant to get overall results
                //     .Select(group => new {
                //         // Look up total number of processed and queued messages
                //         ProcessedCount = group.Count(message => message.ProcessedStatus == "PROCESSED"),
                //         QueuedCount = group.Count(message => message.ProcessedStatus == "QUEUED"),
                //         // Look up oldest and newest queued messages
                //         OldestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                //                             .OrderBy(message => message.CreatedDate)
                //                             .Select(message => message.CreatedDate)
                //                             .FirstOrDefault(),
                //         NewestQueued = group.Where(message => message.ProcessedStatus == "QUEUED")
                //                             .OrderByDescending(message => message.CreatedDate)
                //                             .Select(message => message.CreatedDate)
                //                             .FirstOrDefault(),
                //         // Look up the most recently processed message
                //         LatestProcessed = group.Where(message => message.ProcessedStatus == "PROCESSED")
                //                                .OrderByDescending(message => message.UpdatedDate)
                //                                .Select(message => message.UpdatedDate)
                //                                .FirstOrDefault(),
                //         // How many message have we processed in the past 5 minutes and 1 hour?
                //         // Note that we make the assumption that UpdatedDate will be when state was changed to PROCESSED
                //         ProcessedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                //                                                            message.UpdatedDate >= fiveMinutesAgo),
                //         ProcessedCountOneHour = group.Count(message => message.ProcessedStatus == "PROCESSED" &&
                //                                                        message.UpdatedDate >= oneHourAgo),
                //         // How many messages have we queued in the past 5 minutes and 1 hour?
                //         QueuedCountFiveMinutes = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                //                                                         message.CreatedDate >= fiveMinutesAgo),
                //         QueuedCountOneHour = group.Count(message => message.ProcessedStatus == "QUEUED" &&
                //                                                     message.CreatedDate >= oneHourAgo),
                //         })
                //     .FirstOrDefault();

                // Now do the above grouped by source
                var sourceResults = _context.IncomingMessageItems
                    .Where(message => message.CreatedDate >= _since) // Only include messages as specified using _since
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
                    .Where(message => message.CreatedDate >= _since) // Only include messages as specified using _since
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
                    .Where(message => message.CreatedDate >= _since) // Only include messages as specified using _since
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

                string ApiEnvironment = _settings.Environment ?? "environment not set in appsettings";

                var result = new
                {
                    ApiEnvironment,
                    ProcessedCount = overallResults?.ProcessedCount, //?? 0,
                    QueuedCount = overallResults?.QueuedCount, //?? 0,
                    OldestQueued = overallResults?.OldestQueued, //?? DateTime.MinValue,
                    NewestQueued = overallResults?.NewestQueued, //?? DateTime.MinValue,
                    LatestProcessed = overallResults?.LatestProcessed, //?? DateTime.MinValue,
                    ProcessedCountFiveMinutes = overallResults?.ProcessedCountFiveMinutes, //?? 0,
                    ProcessedCountOneHour = overallResults?.ProcessedCountOneHour, //?? 0,
                    QueuedCountFiveMinutes = overallResults?.QueuedCountFiveMinutes, //?? 0,
                    QueuedCountOneHour = overallResults?.QueuedCountOneHour, //?? 0,
                    sourceResults,
                    eventTypeResults,
                    jurisdictionResults
                };

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while preparing status information. Param since: {since}", _since);
                return StatusCode(500);
            }

        }

    }
}
