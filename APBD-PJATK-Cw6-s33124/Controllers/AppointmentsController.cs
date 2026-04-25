using APBD_PJATK_Cw6_s33124.DTOs;
using Microsoft.AspNetCore.Mvc;
using APBD_PJATK_Cw6_s33124.Services;

namespace APBD_PJATK_Cw6_s33124.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] string? status, [FromQuery] string? patientLastName)
    {
        var appointments = await service.GetAppointmentsAsync(status, patientLastName);
        return Ok(appointments);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        var appointment = await service.GetAppointmentByIdAsync(id);
        if (appointment == null)
        {
            return NotFound(new ErrorResponseDto
            {
                Message = $"Wizyta o ID {id} nie istnieje."
            });
        }
        return Ok(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequestDto request)
    {
        var result = await service.CreateAppointmentAsync(request);
        if (!result.Success)
        {
            if (result.ErrorMessage!.Contains("terminie"))
            {
                return Conflict(new ErrorResponseDto
                {
                    Message = result.ErrorMessage
                });
                
            }

            return BadRequest (new ErrorResponseDto()
            {
                Message = result.ErrorMessage
            });
        }
        return CreatedAtAction(nameof(GetAppointment), new{id = result.NewId}, request);
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequestDto request)
    {
        var result = await service.UpdateAppointmentAsync(id, request);
        if (!result.Success)
        {
            if (result.ErrorMessage == "NOT_FOUND") return NotFound();
            return Conflict(new ErrorResponseDto
            {
                Message = result.ErrorMessage!
            });
        }
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var result = await service.DeleteAppointmentAsync(id);
        if (!result.Success)
        {
            if (result.ErrorMessage == "NOT_FOUND") return NotFound();
            return Conflict(new ErrorResponseDto { Message = result.ErrorMessage! });
        }
        return NoContent();
    }
}