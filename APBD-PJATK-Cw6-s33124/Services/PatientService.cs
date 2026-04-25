using APBD_PJATK_Cw6_s33124.DTOs;
using Microsoft.Data.SqlClient;

namespace APBD_PJATK_Cw6_s33124.Services;

public class PatientService : IPatientService
{
    private readonly IConfiguration _configuration;
    
    public PatientService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IEnumerable<PatientDto>> GetPatientsAsync()
    {
        var result = new List<PatientDto>();
        var connectionString = _configuration.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);
        using var command =
            new SqlCommand(
                "SELECT IdPatient, FirstName, LastName, Email, PhoneNumber, DateOfBirth, IsActive FROM Patients",
                connection);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new PatientDto
            {
                IdPatient = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                PhoneNumber = reader.GetString(4),
                DateOfBirth =  reader.GetDateTime(5),
                IsActive = reader.GetBoolean(6)
            });
        }
        return result;
    }
}