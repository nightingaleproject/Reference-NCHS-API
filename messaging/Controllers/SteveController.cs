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
    [Route("STEVE/{jurisdictionId:length(2)}/Bundles")]
    [ApiController]
    public class SteveController : BundlesController
    {

        public SteveController(ILogger<BundlesController> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings) : base(logger, context, services, settings) { }

        protected override IEnumerable<OutgoingMessageItem> ExcludeRetrieved(IEnumerable<OutgoingMessageItem> source)
        {
            return source.Where(message => message.SteveRetrievedAt == null);
        }

        protected override void MarkAsRetrieved(OutgoingMessageItem omi, DateTime retrieved)
        {
            omi.SteveRetrievedAt = retrieved;
        }

        protected override string GetMessageSource()
        {
            return "STV";
        }
    }
}
