using APBD_PJATK_Cw6_s33124.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_PJATK_Cw6_s33124.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController(IPatientService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPatients()
    {
        return Ok(await service.GetPatientsAsync());
    }
}