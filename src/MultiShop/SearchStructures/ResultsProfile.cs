using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiShop.SearchStructures
{
    public class ResultsProfile
    {
        public List<Category> Order { get; private set; } = new List<Category>(Enum.GetValues<Category>().Length);

        public ResultsProfile()
        {
            foreach (Category category in Enum.GetValues<Category>())
            {
                Order.Add(category);
            }
        }

        public Category GetCategory(int position)
        {
            return Order[position];
        }

        public enum Category
        {
            RatingPriceRatio,
            Price,
            Purchases,
            Reviews,
        }
    }
}