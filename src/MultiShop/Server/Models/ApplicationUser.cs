using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using MultiShop.Shared.Models;

namespace MultiShop.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public virtual SearchProfile SearchProfile { get; private set; } = new SearchProfile();

        [Required]
        public virtual ResultsProfile ResultsProfile { get; private set; } = new ResultsProfile();
    }
}
