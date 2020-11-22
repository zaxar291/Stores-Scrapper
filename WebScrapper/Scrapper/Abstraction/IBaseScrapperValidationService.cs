namespace WebApplication.Scrapper.Abstraction {

    public interface IBaseScrapperValidationService<VEntity>
    {
        public bool ValidateByRule(object input, VEntity rule);
        public bool ValidateAsNull(object input, VEntity rule);
        public bool ValidateAsEquals(string input, VEntity rule);
    }

}