using System;
using System.IO;
using WebApplication.Scrapper.Abstraction;

namespace WebApplication.Scrapper.Services
{
    public class FilesWriter : AbstractWriter
    {
        public int CreateAndWrite(string data, 
            string fileName = "", 
            string dir = "", 
            string ext = "")
        {
            if (data.Equals(String.Empty))
            {
                return 0;
            }

            if (dir.Equals(String.Empty))
            {
                dir = Directory.GetCurrentDirectory();
            }

            if (ext.Equals(String.Empty))
            {
                ext = ".log";
            }
            return base.Write(data, fileName, dir, ext);
        }
    }
}