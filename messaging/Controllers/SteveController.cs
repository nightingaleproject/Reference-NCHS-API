using messaging.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace messaging.Controllers
{
    [Route("STEVE/{jurisdictionId:length(2)}/Bundle")]
    [Route("STEVE/{jurisdictionId:length(2)}/Bundle/{vitalType:regex(^(VRDR|BFDR-BIRTH|BFDR-FETALDEATH)$)}/{igVersion}")]
    [Route("STEVE/{jurisdictionId:length(2)}/Bundles")] // Historical endpoint for backwards compatibility
    [ApiController]
    public class SteveController : BundlesController
    {

        public SteveController(ILogger<BundlesController> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings) : base(logger, context, services, settings) { }

        protected override string GetMessageSource()
        {
            return "STV";
        }

        protected override string GetNextUri()
        {
            return (_settings.STEVE);
        }
    }
}
