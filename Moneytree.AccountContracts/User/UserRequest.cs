namespace Moneytree.AccountContracts.User;

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? MiddleName
);

public record PhoneNumberRequest(
    string PhoneNumber
);

public record UpdateProfileImage(
    string? MimeType,
    string Base64Image
);

// email update exist

// otp verification system