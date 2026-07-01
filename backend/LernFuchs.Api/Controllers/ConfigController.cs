using LernFuchs.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LernFuchs.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly FeatureOptions _features;

    public ConfigController(IOptions<FeatureOptions> features) => _features = features.Value;

    /// <summary>Öffentliche Feature-Flags für das Frontend.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        userGenerationEnabled = _features.UserGenerationEnabled
    });
}
