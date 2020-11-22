using System;
using WebApplication.Scrapper.Abstraction;

namespace WebApplication.Scrapper.Services
{
    public class WebScrapperSelectorValidator : IScrapperBaseValidator
    {
        public bool ValidateWebSelector(string selector)
        {
            if (selector.Trim().Equals(String.Empty))
            {
                return false;
            }

            return true;
        }
    }
}