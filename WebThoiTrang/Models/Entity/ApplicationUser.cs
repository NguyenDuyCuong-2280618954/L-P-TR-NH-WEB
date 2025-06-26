using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
public class ApplicationUser : IdentityUser
{
    [Required]
    public string FullName { get; set; }
    public string? Address { get; set; }
    public string? Age { get; set; }
    public DateTime ConfirmationCodeSentTime { get; set; }
}