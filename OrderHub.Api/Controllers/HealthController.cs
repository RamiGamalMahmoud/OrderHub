using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : Controller
{
    private readonly IDatabaseService _databaseService;

    public HealthController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        //var canConnect = await _databaseService.CanConnectAsync();

        return Ok(new
        {
            status = "Api Server Is Connected."
        });
    }
}
