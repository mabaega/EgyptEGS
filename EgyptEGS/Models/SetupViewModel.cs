namespace EgyptEGS.Models
{
    public class SetupViewModel
    {
        public string Referrer { get; set; }
        public string Api { get; set; }
        public string Token { get; set; }
        public string BusinessDetailJson { get; set; }
        public long EGSVersion { get; set; }
        public ApplicationConfig AppConfig { get; set; } = new ApplicationConfig();
    }
}