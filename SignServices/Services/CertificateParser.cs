using System;
using System.Security.Cryptography.X509Certificates;

public class CertificateDetails
{
    public string Subject { get; set; }
    public string IssuerName { get; set; }
    public string ValidFrom { get; set; }
    public string ValidTo { get; set; }
    public string SerialNumber { get; set; }
    public string Thumbprint { get; set; }
    public string OrganizationIdentifier { get; set; }
    public string Email { get; set; }
    public string CommonName { get; set; }
    public string OrganizationalUnit { get; set; }
    public string Organization { get; set; }
    public string Locality { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
}

public class CertificateParser
{
    public static CertificateDetails ExtractCertificateInfo(X509Certificate2 cert)
    {
        var certInfo = new CertificateDetails
        {
            IssuerName = cert.Issuer.ToString(),
            Subject = cert.Subject,
            ValidFrom = cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"),
            ValidTo = cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"),
            SerialNumber = cert.SerialNumber,
            Thumbprint = cert.Thumbprint
        };

        var subjectParts = cert.Subject.Split(',');

        foreach (var part in subjectParts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("OID.2.5.4.97="))
                certInfo.OrganizationIdentifier = trimmed.Replace("OID.2.5.4.97=", "");
            else if (trimmed.StartsWith("E="))
                certInfo.Email = trimmed.Replace("E=", "");
            else if (trimmed.StartsWith("CN="))
                certInfo.CommonName = trimmed.Replace("CN=", "");
            else if (trimmed.StartsWith("OU="))
                certInfo.OrganizationalUnit = trimmed.Replace("OU=", "");
            else if (trimmed.StartsWith("O="))
                certInfo.Organization = trimmed.Replace("O=", "");
            else if (trimmed.StartsWith("L="))
                certInfo.Locality = trimmed.Replace("L=", "");
            else if (trimmed.StartsWith("S="))
                certInfo.State = trimmed.Replace("S=", "");
            else if (trimmed.StartsWith("C="))
                certInfo.Country = trimmed.Replace("C=", "");
        }

        return certInfo;
    }
}
