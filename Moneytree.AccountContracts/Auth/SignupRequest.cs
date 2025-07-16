namespace Moneytree.AccountContracts.Auth;

public record EmailRequest (
    string Email
);

public record VerifyOtpRequest (
    string ReferenceToken,
    string Otp
);

public record SignupCompleteProfileRequest (
    string FirstName,
    string LastName,
    string MiddleName,
    string PhoneNumber,
    string Refferer
);

public record LoginRequest (
    string Email,
    string? Passcode,
    string? MfaToken
);

public record ChangePasswordRequest(
    string CurrentPasscode
);

public record SetPasswordRequest(
    string NewPasscode,
    string ConfirmPasscode
);