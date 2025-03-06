using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TokenServices.Models;

namespace TokenServices.Controllers
{
    public class TokenConfigurationController : Controller
    {
        private readonly ILogger<TokenConfigurationController> _logger;
        private readonly string _settingsPath;

        public TokenConfigurationController(ILogger<TokenConfigurationController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _settingsPath = Path.Combine(env.ContentRootPath, "App_Data", "tokensettings.json");
        }

        public IActionResult Index()
        {
            TokenSetting settings = LoadSettingsFromFile();
            return View("~/Views/Home/Index.cshtml", settings);
        }

        [HttpGet("LoadSettings")]
        public IActionResult LoadSettings()
        {
            try
            {
                TokenSetting settings = LoadSettingsFromFile();
                // Pastikan kita selalu mengirim object, bukan null
                return Json(new
                {
                    success = true,
                    settings = settings ?? new TokenSetting() // Return empty settings object if null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading token settings");
                return Json(new
                {
                    success = false,
                    message = "Failed to load settings",
                    settings = new TokenSetting() // Always include settings object
                });
            }
        }

        private TokenSetting? LoadSettingsFromFile()
        {
            if (!System.IO.File.Exists(_settingsPath))
            {
                return null;
            }

            string settingsJson = System.IO.File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<TokenSetting>(settingsJson);
        }

        [HttpPost("SaveSettings")]
        public IActionResult SaveSettings([FromBody] TokenSetting settings)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                JsonSerializerOptions options = new() { WriteIndented = true };
                string settingsJson = JsonSerializer.Serialize(settings, options);
                System.IO.File.WriteAllText(_settingsPath, settingsJson);

                return Json(new { success = true, message = "Settings saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving token settings");
                return Json(new { success = false, message = "Failed to save settings" });
            }
        }
    }
}