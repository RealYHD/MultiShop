using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private UserManager<ApplicationUser> userManager;
        private ApplicationDbContext dbContext;
        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
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
            if (userModel.ResultsProfile.Id != resultsProfile.Id) {
                return BadRequest();
            }
            dbContext.Entry(userModel.ResultsProfile).CurrentValues.SetValues(resultsProfile);
            await userManager.UpdateAsync(userModel);
            return NoContent();
        }
    }
}