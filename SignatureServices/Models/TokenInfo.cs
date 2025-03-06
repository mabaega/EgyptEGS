namespace TokenServices.Models
{
    public class TokenInfo
    {
        public string Label { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string CertificateSubject { get; set; }
        public string CertificateIssuer { get; set; }
        public DateTime? ValidUntil { get; set; }
    }
}