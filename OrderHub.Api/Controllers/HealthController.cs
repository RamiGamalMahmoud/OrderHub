using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace OrderHub.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            status = "Api Server Is Connected."
        });
    }
}
