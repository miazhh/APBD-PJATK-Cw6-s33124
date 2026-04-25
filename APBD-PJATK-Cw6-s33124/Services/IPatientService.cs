using APBD_PJATK_Cw6_s33124.DTOs;

namespace APBD_PJATK_Cw6_s33124.Services;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetPatientsAsync();
}