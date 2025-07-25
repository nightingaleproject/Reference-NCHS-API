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
                    ProcessedCount = overallResults?.ProcessedCount,
                    QueuedCount = overallResults?.QueuedCount,
                    OldestQueued = overallResults?.OldestQueued,
                    NewestQueued = overallResults?.NewestQueued,
                    LatestProcessed = overallResults?.LatestProcessed,
                    ProcessedCountFiveMinutes = overallResults?.ProcessedCountFiveMinutes,
                    ProcessedCountOneHour = overallResults?.ProcessedCountOneHour,
                    QueuedCountFiveMinutes = overallResults?.QueuedCountFiveMinutes,
                    QueuedCountOneHour = overallResults?.QueuedCountOneHour,
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
