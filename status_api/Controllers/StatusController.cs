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

								var overallResults = _context.StatusResults
										.FromSqlInterpolated($"EXEC GetStatusOverallResultsWithParams @Since={_since}, @FiveMinutesAgo={fiveMinutesAgo}, @OneHourAgo={oneHourAgo}")
										.AsEnumerable<StatusResults>()
									  .FirstOrDefault();

								
								var sourceResults = _context.StatusResultsBySource
										.FromSqlInterpolated($"EXEC GetStatusBySourceResultsWithParams @Since={_since}, @FiveMinutesAgo={fiveMinutesAgo}, @OneHourAgo={oneHourAgo}")
										.ToList<StatusResultsBySource>();

								var eventTypeResults = _context.StatusResultsByEventType
										.FromSqlInterpolated($"EXEC GetStatusResultsByEventTypeWithParams @Since={_since}, @FiveMinutesAgo={fiveMinutesAgo}, @OneHourAgo={oneHourAgo}")
										.ToList<StatusResultsByEventType>();

								var jurisdictionResults = _context.StatusResultsByJurisdictionId
										.FromSqlInterpolated($"EXEC GetStatusResultsByJurisdictionIdWithParams @Since={_since}, @FiveMinutesAgo={fiveMinutesAgo}, @OneHourAgo={oneHourAgo}")
										.ToList<StatusResultsByJurisdictionId>();
								
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
