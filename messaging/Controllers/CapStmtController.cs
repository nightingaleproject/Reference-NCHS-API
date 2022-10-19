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

namespace messaging.Controllers
{
    [Route("{jurisdictionId:length(2)}/metadata")]
    [ApiController]
    public class CapabilityStatement : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        protected readonly ILogger<CapabilityStatement> _logger;

        public CapabilityStatement(ILogger<CapabilityStatement> logger, ApplicationDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CapabilityStatement
        [HttpGet]
        public async Task<IActionResult> GetCapabilityStatement(string jurisdictionId)
        {
            if (!VRDR.MortalityData.Instance.JurisdictionCodes.ContainsKey(jurisdictionId))
            {
                // Don't log the jurisdictionId value itself, since it is (known-invalid) user input
                _logger.LogError("Rejecting request with invalid jurisdiction ID.");
                return BadRequest();
            }

            try
            {
                // read capability statement file from embedded resource
                using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("messaging.NVSSAPI.fsh_generated.resources.CapabilityStatement-NVSS-API-CS.json"))
                {
                    using (StreamReader r = new StreamReader(stream))
                    {
                        string str = r.ReadToEnd();
                        string customStmt = str.Replace("XX", jurisdictionId);
                        JObject json = JObject.Parse(customStmt);
                        return Ok(json);
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while retrieving the capability statement {ex}");
                return StatusCode(500);
            }

        }

    }
}
