namespace PanoramicData.LicenceMagic;

public sealed record LicenceValidationResult(bool IsValid, string? ErrorMessage)
{
	public static LicenceValidationResult Success { get; } = new(true, null);

	public static LicenceValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
