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
    [Route("{jurisdictionId:length(2)}/CapabilityStatement")]
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
                // read capability statement file
                using (StreamReader r = new StreamReader("NVSSAPI/fsh-generated/resources/CapabilityStatement-NVSS-API-CS.json"))
                {
                    string str = r.ReadToEnd();
                    JObject json = JObject.Parse(str);
                    return Ok(json);
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
