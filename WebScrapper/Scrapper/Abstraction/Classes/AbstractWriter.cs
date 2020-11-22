using System;
using System.IO;
using System.Text;
using WebApplication.Scrapper.Abstraction;

namespace WebApplication.Scrapper.Abstraction
{
    public abstract class AbstractWriter
    {
        protected virtual int Write(
            string data,
            string fileName = "", 
            string dir = "",
            string ext = ".log")
        {
            if (fileName.Equals(""))
            {
                return 2;
            }

            if (dir.Equals(String.Empty))
            {
                return 3;
            }

            if (!File.Exists($"{dir}{fileName}"))
            {
                if (!Create(fileName, dir, data))
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            try
            {
                using (StreamWriter stream = new StreamWriter($"{dir}{fileName}", true))
                {
                    stream.WriteLine(data);
                }
                return 1;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public virtual bool Create(string fileName, string dir = "", string data = "")
        {
            if (dir.Equals(String.Empty))
            {
                dir = Directory.GetCurrentDirectory();
            }

            try
            {
                using (FileStream stream = File.Create($"{dir}{fileName}"))
                {
                    byte[] bytes = new UTF8Encoding(true).GetBytes(data);
                    stream.Write(bytes, 0, bytes.Length);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual bool IsFileExists(string fileName)
        {
            if (!ReferenceEquals(fileName, null) && !fileName.Equals(String.Empty))
            {
                return File.Exists(fileName);
            }
            return false;  
        }

        public virtual string Read(string fileName)
        {
            if (IsFileExists(fileName))
            {
                try 
                {
                    return File.ReadAllText(fileName);
                }
                catch (Exception)
                {
                    return String.Empty;
                }
            }
            return String.Empty;
        }

        public virtual bool Delete(string fileName)
        {
            if (IsFileExists(fileName))
            {
                try 
                {
                    File.Delete(fileName);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }
    }
}