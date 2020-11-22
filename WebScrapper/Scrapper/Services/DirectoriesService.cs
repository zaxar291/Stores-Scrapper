using System;
using System.IO;
using WebScrapper.Scrapper.Abstraction;

namespace WebScrapper.Scrapper.Services
{
    public class DirectoriesService : IBaseDirectoriesProcessor
    {
        private string lastExceptMessage;
        public bool Create(string dir)
        {
            lastExceptMessage = String.Empty;
            if (ReferenceEquals(dir, String.Empty) 
                || ReferenceEquals(dir, null))
            {
                return false;
            }
            try
            {
                Directory.CreateDirectory(dir);
                return true;
            } 
            catch (Exception e) 
            {
                lastExceptMessage = $"{e.Message} -> {e.StackTrace}";
                return false;
            }
        }

        public bool Remove(string dir)
        {
            lastExceptMessage = String.Empty;
            try
            {
                Directory.Delete(dir);
                return true;
            }
            catch (Exception e)
            {
                lastExceptMessage = $"{e.Message} -> {e.StackTrace}";
                return false;
            }
        }

        public bool Exists(string dir)
        {
            return Directory.Exists(dir);
        }

        public string GetLastExcept() 
        {
            return lastExceptMessage;
        }
    }
}