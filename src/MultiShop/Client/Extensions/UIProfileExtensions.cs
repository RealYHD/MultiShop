using MultiShop.Shared.Models;

namespace MultiShop.Client.Extensions
{
    public static class applicationProfileExtensions
    {
        public static string GetButtonCssClass(this ApplicationProfile applicationProfile, string otherClasses = "", bool outline = false) {
            if (outline) {
                return otherClasses + (applicationProfile.DarkMode ? " btn btn-outline-light" : " btn btn-outline-dark");
            }
            return otherClasses + (applicationProfile.DarkMode ? " btn btn-light" : " btn btn-dark");
        }

        public static string GetPageCssClass(this ApplicationProfile applicationProfile, string otherClasses = "") {
            return otherClasses + (applicationProfile.DarkMode ? " text-white bg-dark" : " text-dark bg-white");
        }

        public static string GetNavCssClass(this ApplicationProfile applicationProfile, string otherClasses = "") {
            return otherClasses + (applicationProfile.DarkMode ? " navbar-dark bg-dark" : " navbar-light bg-light");
        }
    }
}