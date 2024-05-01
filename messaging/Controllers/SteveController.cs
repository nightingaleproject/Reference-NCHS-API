using Hl7.Fhir.Model;
using messaging.Models;
using messaging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace messaging.Controllers
{
    [Route("STEVE/{jurisdictionId:length(2)}/Bundle")]
    [Route("STEVE/{jurisdictionId:length(2)}/Bundle/{vitalType:length(4)}/{igVersion}")]
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
