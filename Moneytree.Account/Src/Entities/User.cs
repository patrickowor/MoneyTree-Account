namespace Moneytree.Account.Src.Entities;

public class UserModel
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? PhoneNumber { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Onboarding;
    public bool HasMFA { get; set; } = false;
    public bool PhoneNumberVerified { get; set; } = false;
    public string? Refferer { get; set; }
    public string? Email { get; set; }
    public bool EmailVerified { get; set; } = true;
    public string? Passcode { get; set; }
    public string? RefferalCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastLogInAttempt { get; set; } = DateTime.UtcNow;

    public int FailedLoginAttempt { get; set; } = 0;
    
    public string? ProfileImage { get; set; } 
}


public enum UserStatus
{
    Active,

    Onboarding,

    Blocked,

    Deleted,
}