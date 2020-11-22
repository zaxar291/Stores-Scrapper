using System.Collections.Generic;

namespace WebApplication.Scrapper.Delegates {
    public delegate void LinksScrapperCallback(object sender, LinksScrapperCallbackResult eventArgs);

    public class LinksScrapperCallbackResult {

        public LinksScrapperCallbackResult(List<string> l) {
            LinksPool = l;
        }

        public List<string> LinksPool { get; set; }
    }
}