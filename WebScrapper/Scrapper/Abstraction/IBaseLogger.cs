using WebApplication.Scrapper.Abstraction;

namespace WebApplication.Scrapper.Abstraction
{
    public abstract class  BaseLogger : AbstractWriter
    {
        public virtual int info(string message)
        {
            return this.Write($"[Info] -> {message}");
        }

        public virtual int warn(string message)
        {
            return this.Write($"[Warn] -> {message}");
        }

        public virtual int error(string message)
        {
            return this.Write($"[Error] -> {message}");
        }
    }
}