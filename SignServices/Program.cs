using SignServices;
using SignServices.Models;
using SignServices.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddCors(policy =>
{
    policy.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod()
               .WithExposedHeaders("*");
    });
});

// Add TokenSettings and TokenService as singleton
TokenSettings tokenSettings = new(builder.Configuration);
builder.Services.AddSingleton(tokenSettings);
builder.Services.AddSingleton<TokenService>(sp =>
    new TokenService(
        tokenSettings.DriverPath,
        string.Empty, // Initial PIN is empty
        sp.GetRequiredService<ILogger<TokenService>>()
    ));

// Add Worker service
builder.Services.AddHostedService<Worker>();

// Configure Windows Service and Systemd support
builder.Services.AddWindowsService();
builder.Services.AddSystemd();

// Add endpoint routing
builder.Services.AddRouting();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable CORS before other middleware
app.UseRouting();
app.UseCors("AllowAll");

// Enable static files
app.UseStaticFiles();

// Configure endpoints
app.MapGet("/setting", async (string driverPath, string pin, TokenSettings settings, TokenService tokenService) =>
{
    try
    {
        if (!settings.ValidateDriverPath(driverPath))
        {
            return Results.BadRequest(new { message = "Invalid driver path. Please provide a valid .dll file." });
        }
        settings.DriverPath = driverPath;
        settings.SaveSettings(settings.GetOptions());
        TokenInfo? tokenInfo = await tokenService.InitializeToken(driverPath, pin);
        if (tokenInfo == null)
        {
            return Results.BadRequest(new { message = "Failed to initialize token. Please check your PIN and token connection." });
        }
        return Results.Ok(tokenInfo);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/sign", async (SignRequest request, TokenSettings settings, TokenService tokenService) =>
{
    if (string.IsNullOrEmpty(settings.DriverPath))
    {
        return Results.BadRequest(new { message = "Driver path not set. Please configure token settings first." });
    }

    try
    {
        TokenInfo? tokenInfo = await tokenService.InitializeToken(settings.DriverPath, request.Pin);

        if (tokenInfo == null)
        {
            return Results.BadRequest(new { message = "Failed to initialize token. Please check your PIN and token connection." });
        }

        if (!tokenInfo.IsReadyForSigning)
        {
            return Results.BadRequest(new { message = "Token is not ready for signing. Please check token status." });
        }

        SignResponse response = await tokenService.SignDocument(request.SerializedInvoice);

        if (string.IsNullOrEmpty(response.Signature))
        {
            return Results.BadRequest(new { message = "Failed to generate signature. Please try again." });
        }

        return Results.Ok(new SignResponse
        {
            Signature = response.Signature
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/sign2", async (SignRequest request, TokenSettings settings, TokenService tokenService) =>
{
    if (string.IsNullOrEmpty(settings.DriverPath))
    {
        return Results.BadRequest(new { message = "Driver path not set. Please configure token settings first." });
    }

    try
    {
        TokenInfo? tokenInfo = await tokenService.InitializeToken(settings.DriverPath, request.Pin);

        if (tokenInfo == null)
        {
            return Results.BadRequest(new { message = "Failed to initialize token. Please check your PIN and token connection." });
        }

        if (!tokenInfo.IsReadyForSigning)
        {
            return Results.BadRequest(new { message = "Token is not ready for signing. Please check token status." });
        }

        SignResponse response = await tokenService.SignDocument2(request.SerializedInvoice);

        if (string.IsNullOrEmpty(response.Signature))
        {
            return Results.BadRequest(new { message = "Failed to generate signature. Please try again." });
        }

        return Results.Ok(new SignResponse
        {
            Signature = response.Signature
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});


app.MapGet("/getcert", async (TokenSettings settings, TokenService tokenService) =>
{
    if (string.IsNullOrEmpty(settings.DriverPath))
    {
        return Results.BadRequest(new { message = "Driver path not set. Please configure token settings first." });
    }

    try
    {
        var cert = await tokenService.GetPublicCertificateOnly();
        if (cert != null)
        {
            var certInfo = CertificateParser.ExtractCertificateInfo(cert);
            return Results.Ok(certInfo);
        }
        return Results.NotFound(new { message = "No certificate found in token" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});


// Set index.html as default page
app.MapFallbackToFile("index.html");

// Create a cancellation token source for graceful shutdown
using CancellationTokenSource cts = new();
IHostApplicationLifetime lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    cts.Cancel();
    // Wait for ongoing tasks to complete
    Task.WaitAll(app.Services.GetServices<IHostedService>().Select(service => service.StopAsync(cts.Token)).ToArray());
});

// Log the URLs where the service is running
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Service is starting...");

// Add default URL if none are configured
if (app.Urls.Count == 0)
{
    app.Urls.Add("http://localhost:5211");
}

// Log all URLs where the service will be available
foreach (string url in app.Urls)
{
    logger.LogInformation("Service will be available at: {Url}", url);
}

// Start the application
try
{
    await app.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Normal shutdown, no need to log
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while running the application");
    throw;
}
finally
{
    // Ensure logger is disposed properly
    if (logger is IDisposable disposableLogger)
    {
        disposableLogger.Dispose();
    }
}
