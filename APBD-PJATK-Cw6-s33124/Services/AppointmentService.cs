using System.Data;
using APBD_PJATK_Cw6_s33124.DTOs;
using Microsoft.Data.SqlClient;

namespace APBD_PJATK_Cw6_s33124.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    private readonly string _connectionString = configuration.GetConnectionString("Default");

    public async Task<IEnumerable<AppointmentListDto>> GetAppointmentsAsync(string? status, string? patientLastName)
    {
        var result = new List<AppointmentListDto>();

        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand("""
                                                 SELECT 
                                                     a.IdAppointment, 
                                                     a.AppointmentDate, 
                                                     a.Status, 
                                                     a.Reason, 
                                                     p.FirstName + ' ' + p.LastName AS PatientFullName, 
                                                     p.Email AS PatientEmail
                                                 FROM dbo.Appointments a
                                                 JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                                                 WHERE (@Status IS NULL OR a.Status = @Status)
                                                   AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                                                 ORDER BY a.AppointmentDate;
                                                 """, connection);

        command.Parameters.Add("@Status", SqlDbType.NVarChar).Value = (object?)status ?? DBNull.Value;
        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar).Value = (object?)patientLastName ?? DBNull.Value;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new AppointmentListDto()
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5),
            });
        }

        return result;

    }

    public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
                                                 SELECT 
                                                     a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, a.InternalNotes, a.CreatedAt,
                                                     p.FirstName + ' ' + p.LastName AS PatientName, p.Email, p.PhoneNumber,
                                                     d.FirstName + ' ' + d.LastName AS DoctorName, d.LicenseNumber
                                                 FROM dbo.Appointments a
                                                 JOIN dbo.Patients p ON a.IdPatient = p.IdPatient
                                                 JOIN dbo.Doctors d ON a.IdDoctor = d.IdDoctor
                                                 WHERE a.IdAppointment = @Id;
                                                 """, connection);

        command.Parameters.AddWithValue("@Id", idAppointment);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new AppointmentDetailsDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),

                InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = reader.GetDateTime(5),
                PatientFullName = reader.GetString(6),
                PatientEmail = reader.GetString(7),
                PatientPhoneNumber = reader.GetString(8),
                DoctorFullName = reader.GetString(9),
                DoctorLicenseNumber = reader.GetString(10)
            };
        }

        return null;
    }

    public async Task<(bool Success, string? ErrorMessage, int? NewId)> CreateAppointmentAsync(
        CreateAppointmentRequestDto request)
    {
        if (request.AppointmentDate < DateTime.Now)
        {
            return (false, "Błędny termin wizyty.", null);
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 250)
        {
            return (false, "Opis wizyty jest wymagany, max 250 znaków.", null);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var patientCmd = new SqlCommand("SELECT IsActive FROM Patients WHERE IdPatient = @Id", connection);
        patientCmd.Parameters.AddWithValue("@Id", request.IdPatient);
        var patientActive = await patientCmd.ExecuteScalarAsync();

        if (patientActive == null)
        {
            return (false, "Pacjent nie istnieje.", null);
        }

        if (!(bool)patientActive)
        {
            return (false, "Pacjent jest nieaktywny.", null);
        }

        var doctorCmd = new SqlCommand("SELECT IsActive FROM Doctors WHERE IdDoctor = @Id", connection);
        doctorCmd.Parameters.AddWithValue("@Id", request.IdDoctor);
        var doctorActive = await doctorCmd.ExecuteScalarAsync();

        if (doctorActive == null)
        {
            return (false, "Lekarz nie istnieje.", null);
        }

        if (!(bool)doctorActive)
        {
            return (false, "Lekarz jest nieaktywny.", null);
        }

        var conflictCmd = new SqlCommand(
            "SELECT COUNT(*) FROM Appointments WHERE IdDoctor = @IdD AND AppointmentDate = @Date AND Status != 'Cancelled'",
            connection);
        conflictCmd.Parameters.AddWithValue("@IdD",request.IdDoctor);
        conflictCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);

        if ((int)await conflictCmd.ExecuteScalarAsync() > 0)
            return (false, "Lekarz ma już inną wizytę w tym terminie.", null);

        var insertCmd = new SqlCommand("""
                                       INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, CreatedAt)
                                       OUTPUT INSERTED.IdAppointment
                                       VALUES (@IdP, @IdD, @Date, 'Scheduled',@Reason, GETDATE());
                                       """, connection);

        insertCmd.Parameters.AddWithValue("@IdP", request.IdPatient);
        insertCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
        insertCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);
        insertCmd.Parameters.AddWithValue("@Reason", request.Reason);

        var newId = (int)await insertCmd.ExecuteScalarAsync();
        return (true, null, newId);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAppointmentAsync(int id,
        UpdateAppointmentRequestDto request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkCmd = new SqlCommand("SELECT Status, AppointmentDate FROM Appointments WHERE IdAppointment = @Id", connection);
        checkCmd.Parameters.AddWithValue("@Id", id);
        await using var reader = await checkCmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return (false, "NOT_FOUND");

        var currentStatus = reader.GetString(0);
        var currentDate = reader.GetDateTime(1);
        await reader.CloseAsync();

        if (currentStatus == "Completed" && currentDate != request.AppointmentDate)
            return (false, "Nie można zmienić terminu zakończonej wizyty.");

        var conflictCmd = new SqlCommand(
            "SELECT COUNT(*) FROM Appointments WHERE IdDoctor = @IdD AND AppointmentDate = @Date AND IdAppointment != @Id AND Status != 'Cancelled'",
            connection);
        conflictCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
        conflictCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);
        conflictCmd.Parameters.AddWithValue("@Id", id);

        if ((int)await conflictCmd.ExecuteScalarAsync() > 0)
            return (false, "Lekarz jest zajęt w tym terminie.");

        var updateCmd = new SqlCommand("""
                                       UPDATE Appointments 
                                       SET IdPatient = @IdP, IdDoctor = @IdD, AppointmentDate = @Date, Status = @Status, Reason = @Reason, InternalNotes = @Notes
                                       WHERE IdAppointment = @Id
                                       """, connection);

        updateCmd.Parameters.AddWithValue("@Id", id);
        updateCmd.Parameters.AddWithValue("@IdP", request.IdPatient);
        updateCmd.Parameters.AddWithValue("@IdD", request.IdDoctor);
        updateCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);
        updateCmd.Parameters.AddWithValue("@Status", request.Status);
        updateCmd.Parameters.AddWithValue("@Reason", request.Reason);
        updateCmd.Parameters.AddWithValue("@Notes", (object?)request.InternalNotes ?? DBNull.Value);

        await updateCmd.ExecuteNonQueryAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAppointmentAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkCmd = new SqlCommand("SELECT Status FROM Appointments WHERE IdAppointment = @Id", connection);
        checkCmd.Parameters.AddWithValue("@Id", id);
        var status = await checkCmd.ExecuteScalarAsync();

        if (status == null) return (false, "NOT_FOUND");
        if (status.ToString() == "Completed") return (false, "Nie można usunąć zakończonej wizyty.");

        var deleteCmd = new SqlCommand("DELETE FROM Appointments WHERE IdAppointment = @Id", connection);
        deleteCmd.Parameters.AddWithValue("@Id", id);
        await deleteCmd.ExecuteNonQueryAsync();

        return (true, null);
    }
}