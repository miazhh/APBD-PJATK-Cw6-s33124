using APBD_PJATK_Cw6_s33124.DTOs;

namespace APBD_PJATK_Cw6_s33124.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAppointmentsAsync(string? status, string? patientLastName);
    Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment);
    Task<(bool Success, string? ErrorMessage, int? NewId)> CreateAppointmentAsync(CreateAppointmentRequestDto request);
    Task<(bool Success, string? ErrorMessage)> UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto request);
    Task<(bool Success, string? ErrorMessage)> DeleteAppointmentAsync(int id);
}