namespace WebApplication.Scrapper.Delegates {
    public delegate void LinksScrapperIterationCallback(object sender, LinksScrapperIterationCallbackResult eventArgs);

    public class LinksScrapperIterationCallbackResult {

        public LinksScrapperIterationCallbackResult(string l) {
            LinksPool = l;
        }

        public string LinksPool { get; set; }
    }
}