using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;

namespace MultiShop.Shared.Models
{
    public class ResultsProfile
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        
        [Required]
        public IList<Category> Order { get; set; }

        public ResultsProfile()
        {
            Order = new List<Category>(Enum.GetValues<Category>().Length);
            foreach (Category category in Enum.GetValues<Category>())
            {
                Order.Add(category);
            }
        }

        public enum Category
        {
            RatingPriceRatio,
            Reviews,
            Purchases,
            Price,
        }
    }
}