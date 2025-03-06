using Microsoft.AspNetCore.Mvc;
using TokenServices.Models;
using TokenServices.Services;

namespace TokenServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DigitalSignatureController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DigitalSignatureController> _logger;

        public DigitalSignatureController(IWebHostEnvironment environment, ILogger<DigitalSignatureController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("GetTokenInfo")]
        public ActionResult GetTokenInfo([FromBody] TokenRequest request)
        {
            try
            {
                string driverPath = Path.Combine(_environment.ContentRootPath, "Drivers", request.LibraryPath);
                _logger.LogInformation("Full driver path: {DriverPath}", driverPath);

                if (!System.IO.File.Exists(driverPath))
                {
                    _logger.LogError("Driver file not found at: {DriverPath}", driverPath);
                    return Ok(CreateErrorResponse("Driver not found at " + driverPath));
                }

                ILogger<TokenSigner> tokenSignerLogger = LoggerFactory.Create(builder =>
                    builder.AddConsole()).CreateLogger<TokenSigner>();

                TokenSigner signer = new(driverPath, request.Pin, tokenSignerLogger);
                _logger.LogInformation("TokenSigner instance created successfully");

                TokenInfo tokenInfo = signer.GetTokenInfo();
                _logger.LogInformation("Token info retrieved: {@TokenInfo}", tokenInfo);

                if (tokenInfo == null)
                {
                    _logger.LogWarning("No token information available");
                    return Ok(CreateErrorResponse("No token information available"));
                }

                return Ok(new
                {
                    success = true,
                    tokenInfo = new
                    {
                        Label = tokenInfo.Label ?? "N/A",
                        Model = tokenInfo.Model ?? "N/A",
                        SerialNumber = tokenInfo.SerialNumber ?? "N/A"
                    },
                    certificateInfo = new
                    {
                        Subject = tokenInfo.CertificateSubject ?? "N/A",
                        Issuer = tokenInfo.CertificateIssuer ?? "N/A",
                        ValidUntil = tokenInfo.ValidUntil?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token info: {ErrorMessage}", ex.Message);
                return Ok(CreateErrorResponse(ex.Message));
            }
        }

        [HttpPost("SignDocument")]
        public ActionResult SignDocument([FromBody] SignRequest request)
        {
            try
            {
                string driverPath = Path.Combine(_environment.ContentRootPath, "Drivers", request.LibraryPath);
                _logger.LogInformation("Full driver path for signing: {DriverPath}", driverPath);

                if (!System.IO.File.Exists(driverPath))
                {
                    _logger.LogError("Driver file not found at: {DriverPath}", driverPath);
                    return Ok(new { success = false, message = $"Driver not found at {driverPath}" });
                }

                ILogger<TokenSigner> tokenSignerLogger = LoggerFactory.Create(builder =>
                    builder.AddConsole()).CreateLogger<TokenSigner>();

                TokenSigner signer = new(driverPath, request.Pin, tokenSignerLogger);
                _logger.LogInformation("TokenSigner instance created");

                TokenInfo tokenInfo = signer.GetTokenInfo();
                _logger.LogInformation("TokenInfo raw data: {@TokenInfo}", tokenInfo);
                //_logger.LogInformation("Token Label: {Label}", tokenInfo?.Label);
                //_logger.LogInformation("Token Model: {Model}", tokenInfo?.Model);
                //_logger.LogInformation("Token SerialNumber: {SerialNumber}", tokenInfo?.SerialNumber);
                //_logger.LogInformation("Certificate Subject: {Subject}", tokenInfo?.CertificateSubject);
                //_logger.LogInformation("Certificate Issuer: {Issuer}", tokenInfo?.CertificateIssuer);
                //_logger.LogInformation("Certificate ValidUntil: {ValidUntil}", tokenInfo?.ValidUntil);

                string? signature = signer.Sign(request.Data);
                _logger.LogInformation("Generated signature length: {Length}", signature?.Length);
                //_logger.LogInformation("Signature value: {Signature}", signature);

                return Ok(new
                {
                    success = true,
                    tokenInfo,  // Return the raw tokenInfo object
                    signature
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignDocument: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return Ok(new { success = false, message = ex.Message });
            }
        }
        private object CreateErrorResponse(string message)
        {
            return new
            {
                success = false,
                message,
                tokenInfo = new
                {
                    Label = "N/A",
                    Model = "N/A",
                    SerialNumber = "N/A"
                },
                certificateInfo = new
                {
                    Subject = "N/A",
                    Issuer = "N/A",
                    ValidUntil = "N/A"
                },
                signature = (string)null
            };
        }
    }
}