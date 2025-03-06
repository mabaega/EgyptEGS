using EgyptEGS.ApiClient.Model;

namespace EgyptEGS.ApiClient.Extensions
{
    public static class IntegrationTypeExtensions
    {
        private const string API_DOMAIN = "invoicing.eta.gov.eg";
        private const string ID_DOMAIN = "eta.gov.eg";

        public static string GetEnvironmentPrefix(this IntegrationType type)
        {
            return type == IntegrationType.PreProduction ? "preprod" : "prod";
        }

        public static string GetApiBaseUrl(this IntegrationType type)
        {
            return $"https://api.{type.GetEnvironmentPrefix()}.{API_DOMAIN}";
        }

        public static string GetIdSrvBaseUrl(this IntegrationType type)
        {
            return $"https://id.{type.GetEnvironmentPrefix()}.{ID_DOMAIN}";
        }
    }
}