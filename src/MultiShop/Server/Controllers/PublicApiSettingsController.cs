using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MultiShop.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PublicApiSettingsController : ControllerBase
    {
        private IConfiguration configuration;
        public PublicApiSettingsController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetPublicConfiguration() {
            return Ok(new Dictionary<string, string> {
                {"IdentityServer:Registration", configuration["IdentityServer:Registration"]}
            });
        }
    }
}