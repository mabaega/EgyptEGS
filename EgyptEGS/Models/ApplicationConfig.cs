using EgyptEGS.ApiClient.Model;

namespace EgyptEGS.Models
{
    public class ApplicationConfig
    {
        public IntegrationType IntegrationType { get; set; } = IntegrationType.PreProduction;
        public string ActivityCode { get; set; } = "0910";
        public Party Issuer { get; set; }
        public string SignServiceUrl { get; set; } = "http://localhost:5211";
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public ApplicationConfig()
        {
            // Set the default values for the properties
            Issuer = new Party();
            Issuer.Type = PartyType.B;
            Issuer.Id = "382029216";
            Issuer.Name = "Issuer Company";
            Issuer.Address = new Address
            {
                BranchId = "0",
                Country = "EG",
                Governate = "Cairo",
                RegionCity = "Nasr City",
                Street = "580 Clementina Key",
                BuildingNumber = "Bldg. 0",
                PostalCode = "68030",
                Floor = "1",
                Room = "123",
                Landmark = "7660 Melody Trail",
                AdditionalInformation = "beside Townhall"
            };
        }

        /* public string ToBusinessAddress()
        {
            string address = $"{Issuer.Address.Room}, {Issuer.Address.Floor}, {Issuer.Address.BuildingNumber}, {Issuer.Address.Street}, {Issuer.Address.Landmark}, {Issuer.Address.AdditionalInformation}, {Issuer.Address.RegionCity}, {Issuer.Address.Governate}, {Issuer.Address.Country}, {Issuer.Address.PostalCode}";
            
            return $@"<table><tbody><tr><td colspan=""2"">{Issuer.Name}</td><td></td></tr><tr><td width=""70%"">Registration Number</td><td>Type</td></tr><tr><td>{Issuer.Id}</td><td>{(Issuer.Type == PartyType.B ? "Business" : "Person")}</td></tr><tr><td>Branch Address</td><td>Tax Activity Code : {ActivityCode}</td></tr><tr><td colspan=""2"">{address}</td><td></td></tr></tbody></table>";
        } */
    }

    public class CertInfoViewModel
    {
        public string BusinessDetailJson { get; set; }
        public string Api { get; set; }
        public string Token { get; set; }
        public string Referrer { get; set; }
        public ApplicationConfig AppConfig { get; set; }
        public CertInfoViewModel() { }
    }
}
