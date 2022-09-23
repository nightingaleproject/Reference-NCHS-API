using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using messaging.Models;
using messaging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace messaging.Controllers
{
    [Route("STEVE/{jurisdictionId:length(2)}/CapabilityStatement")]
    [ApiController]
    public class SteveCapabilityStatement : CapabilityStatement
    {
        private readonly ApplicationDbContext _context;
        protected readonly ILogger<CapabilityStatement> _logger;

        public SteveCapabilityStatement(ILogger<CapabilityStatement> logger, ApplicationDbContext context) : base(logger, context)
        { }

    }
}
