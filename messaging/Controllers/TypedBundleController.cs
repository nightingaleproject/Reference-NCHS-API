using System;
using System.Threading.Tasks;
using messaging.Models;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using messaging.Services;

namespace messaging.Controllers
{
    [Route("{jurisdictionId:length(2)}/Bundle/{vitalType:regex(^(VRDR|BFDR-BIRTH|BFDR-FETALDEATH)$)}/{igVersion}")]
    [Produces("application/json")]
    [ApiController]
    public class TypedBundleController : BundleController
    {
        public TypedBundleController(ILogger<BundleController> logger, ApplicationDbContext context, IServiceProvider services, IOptions<AppSettings> settings) : base(logger, context, services, settings) { }
    }
}
