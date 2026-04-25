namespace APBD_PJATK_Cw6_s33124.DTOs;

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
}