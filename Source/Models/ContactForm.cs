namespace AspNetCore.API.Models;

public sealed class ContactForm
{
    public string Name { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string Message { get; set; } = String.Empty;
}