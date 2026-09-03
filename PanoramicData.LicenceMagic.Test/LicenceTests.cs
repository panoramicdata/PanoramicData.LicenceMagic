using System;
using System.IO;
using Xunit;

namespace PanoramicData.LicenceMagic.Test;

public class LicenseTests
{
	private static readonly FileInfo GoodFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test.lic"));
	private static readonly FileInfo BadFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test1.lic"));
	private readonly byte[] _salt = { 0, 1, 2, 3, 4, 5, 6, 7 };

	/// <summary>
	/// Builds licence details that are valid in every respect, so that each test can omit exactly
	/// the one field it is about by passing null for it.
	/// </summary>
	private static TestLicenceDetails CreateLicenceDetails(
		string? startVersion = "1.0",
		string? endVersion = "999.999",
		string? licensedCompany = "ACME Inc",
		string? licensedProduct = "Anvil")
		=> new() {
			StartDateUtc = new DateTime(2001, 01, 01),
			EndDateUtc = new DateTime(2100, 01, 01),
			StartVersion = startVersion,
			EndVersion = endVersion,
			LicensedCompany = licensedCompany,
			LicensedProduct = licensedProduct,
		};

	private static void AssertValidationFailed(LicenceValidationResult validation, string expectedErrorMessage)
	{
		Assert.False(validation.IsValid);
		Assert.NotNull(validation.ErrorMessage);
		Assert.Equal(expectedErrorMessage, validation.ErrorMessage);
	}

	/// <summary>
	/// Signs the details, then asserts that validating them still fails for the given reason.
	/// </summary>
	private void AssertSignedValidationFails(TestLicenceDetails licenceDetails, string expectedErrorMessage)
	{
		licenceDetails.Sign(GoodFileInfo.Name, _salt);
		AssertValidationFailed(licenceDetails.Validate(GoodFileInfo.Name, _salt), expectedErrorMessage);
	}

	[Fact]
	public void SaveToFileAndReload()
	{
		var originalLicenceDetails = CreateLicenceDetails();

		// The LicenceDetails should not be valid before signing
		var validation = originalLicenceDetails.Validate(GoodFileInfo.Name, _salt);
		Assert.False(validation.IsValid);
		originalLicenceDetails.Sign(GoodFileInfo.Name, _salt);
		AssertValidationFailed(validation, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

		// The LicenceDetails should be valid after signing
		validation = originalLicenceDetails.Validate(GoodFileInfo.Name, _salt);
		Assert.True(validation.IsValid);
		Assert.Null(validation.ErrorMessage);

		// ... but only for that filename
		validation = originalLicenceDetails.Validate(BadFileInfo.Name, _salt);
		AssertValidationFailed(validation, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

		// Write the license file
		new License<TestLicenceDetails>(originalLicenceDetails).WriteToFile(GoodFileInfo, _salt);

		// Read it back in - it should be valid
		var readBackLicense = new License<TestLicenceDetails>(GoodFileInfo);
		Assert.True(readBackLicense.Validate(_salt).IsValid);

		// ... but not with a different filename
		BadFileInfo.Delete();
		GoodFileInfo.MoveTo(BadFileInfo.FullName);
		readBackLicense = new License<TestLicenceDetails>(BadFileInfo);
		AssertValidationFailed(readBackLicense.Validate(_salt), LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

		// Clean-up by deleting the file
		BadFileInfo.Delete();
	}

	[Fact]
	public void TamperedSignatureShouldFailValidation()
	{
		var licenceDetails = CreateLicenceDetails();

		licenceDetails.Sign(GoodFileInfo.Name, _salt);
		var signatureBytes = Convert.FromBase64String(licenceDetails.Signature!);
		signatureBytes[^1] ^= 1;
		licenceDetails.Signature = Convert.ToBase64String(signatureBytes);

		AssertValidationFailed(
			licenceDetails.Validate(GoodFileInfo.Name, _salt),
			LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);
	}

	[Fact]
	public void LackOfVersionShouldFailValidation()
	{
		// Signed, these are still invalid as each is missing a version bound
		AssertSignedValidationFails(
			CreateLicenceDetails(endVersion: null),
			LicenceDetails.EndVersionErrorMessage);

		AssertSignedValidationFails(
			CreateLicenceDetails(startVersion: null, endVersion: "1.0"),
			LicenceDetails.StartVersionErrorMessage);
	}

	[Fact]
	public void LackOfLicensedCompanyShouldFailValidation()
		=> AssertSignedValidationFails(
			CreateLicenceDetails(endVersion: "1.9", licensedCompany: null),
			LicenceDetails.NoLicensedCompanyErrorMessage);

	[Fact]
	public void LackOfLicensedProductShouldFailValidation()
		=> AssertSignedValidationFails(
			CreateLicenceDetails(endVersion: "1.9", licensedProduct: null),
			LicenceDetails.NoLicensedProductErrorMessage);
}
