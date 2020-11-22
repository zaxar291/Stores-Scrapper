using WebApplication.Scrapper.Abstraction;

namespace WebApplication.Scrapper.Services {
    public class WebScrapperLinksValidator : IBaseValidationService {
        private string BaseSiteUrl { get; set; }
        private string LastExceptMessage { get; set; }
        public bool Validate(string request) {
            if (ReferenceEquals(request, null)) {
                return false;
            }
            if (request.Contains(BaseSiteUrl)) {
                return true;
            }
            if (request.IndexOf("/").Equals(0)) {
                return true;
            }
            if (request.Equals("#")) {
                return false;
            }
            return false;
        }

        public string GetExceptMessage() {
            return LastExceptMessage;
        }

        public void SetBaseSiteUrl(string s) {
            BaseSiteUrl = s;
        }
    }
}