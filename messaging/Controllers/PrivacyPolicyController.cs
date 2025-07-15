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
    [Route("{jurisdictionId:length(2)}/privacy-policy")]
    [ApiController]
    public class PrivacyPolicyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        protected readonly ILogger<PrivacyPolicyController> _logger;

        public PrivacyPolicyController(ILogger<PrivacyPolicyController> logger, ApplicationDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        // GET: PrivacyPolicy
        [HttpGet]
        public async Task<IActionResult> GetPrivacyPolicy(string jurisdictionId)
        {
            if (!VR.IJEData.Instance.JurisdictionCodes.ContainsKey(jurisdictionId))
            {
                // Don't log the jurisdictionId value itself, since it is (known-invalid) user input
                _logger.LogError("Rejecting request with invalid jurisdiction ID.");
                return BadRequest();
            }
            try
            {
                // read PrivacyPolicy file from embedded resource
                using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("messaging.privacy_policy.txt"))
                {
                    using (StreamReader r = new StreamReader(stream))
                    {
                        // the capability statement is approximately 1 kb
                        char[] buffer = new char[2000];
                        int size = r.ReadBlock(buffer, 0, 2000);
                        string str = new string(buffer);
                        return Ok(str);
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An exception occurred while retrieving the privacy policy {ex}");
                return StatusCode(500);
            }

        }

    }
}
