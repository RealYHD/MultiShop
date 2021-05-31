using System.Text.Json;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MultiShop.Server.Data;
using MultiShop.Server.Models;
using MultiShop.Shared.Models;

namespace MultiShop.Server.Controllers
{

    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class ProfileController : ControllerBase
    {
        private ILogger<ProfileController> logger;
        private UserManager<ApplicationUser> userManager;
        private ApplicationDbContext dbContext;
        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, ILogger<ProfileController> logger)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.logger = logger;
        }

        [HttpGet]
        [Route("Search")]
        public async Task<IActionResult> GetSearchProfile() {
            ApplicationUser userModel = await userManager.GetUserAsync(User);
            return Ok(userModel.SearchProfile);
        }

        [HttpGet]
        [Route("Results")]
        public async Task<IActionResult> GetResultsProfile() {
            ApplicationUser userModel = await userManager.GetUserAsync(User);
            return Ok(userModel.ResultsProfile);
        }

        [HttpGet]
        [Route("Application")]
        public async Task<IActionResult> GetApplicationProfile() {
            ApplicationUser userModel = await userManager.GetUserAsync(User);
            logger.LogInformation(JsonSerializer.Serialize(userModel.ApplicationProfile));
            return Ok(userModel.ApplicationProfile);
        }

        [HttpPut]
        [Route("Search")]
        public async Task<IActionResult> PutSearchProfile(SearchProfile searchProfile) {
            ApplicationUser userModel = await userManager.GetUserAsync(User);
            if (userModel.SearchProfile.Id != searchProfile.Id || userModel.Id != searchProfile.ApplicationUserId) {
                return BadRequest();
            }
            dbContext.Entry(userModel.SearchProfile).CurrentValues.SetValues(searchProfile);
            await userManager.UpdateAsync(userModel);
            return NoContent();
        }

        [HttpPut]
        [Route("Results")]
        public async Task<IActionResult> PutResultsProfile(ResultsProfile resultsProfile) {
            ApplicationUser userModel = await userManager.GetUserAsync(User);
            if (userModel.ResultsProfile.Id != resultsProfile.Id || userModel.Id != resultsProfile.ApplicationUserId) {
                return BadRequest();
            }
            dbContext.Entry(userModel.ResultsProfile).CurrentValues.SetValues(resultsProfile);
            await userManager.UpdateAsync(userModel);
            return NoContent();
        }

        [HttpPut]
        [Route("Application")]
        public async Task<IActionResult> PutApplicationProfile(ApplicationProfile applicationProfile) {
            ApplicationUser userModel = await userManager.GetUserAsync(User);
            logger.LogInformation(JsonSerializer.Serialize(applicationProfile));
            if (userModel.ApplicationProfile.Id != applicationProfile.Id || userModel.Id != applicationProfile.ApplicationUserId) {
                return BadRequest();
            }
            dbContext.Entry(userModel.ApplicationProfile).CurrentValues.SetValues(applicationProfile);
            await userManager.UpdateAsync(userModel);
            return NoContent();
        }
    }
}