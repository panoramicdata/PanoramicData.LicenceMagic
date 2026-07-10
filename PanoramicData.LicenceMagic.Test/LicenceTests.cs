using System;
using System.IO;
using Xunit;

namespace PanoramicData.LicenceMagic.Test;

public class LicenseTests
{
	private static readonly FileInfo GoodFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test.lic"));
	private static readonly FileInfo BadFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test1.lic"));
	private readonly byte[] _salt = { 0, 1, 2, 3, 4, 5, 6, 7 };

	[Fact]
	public void SaveToFileAndReload()
	{
		// Create a LicenceDetails
		var originalLicenceDetails = new TestLicenceDetails {
			StartDateUtc = new DateTime(2001, 01, 01),
			EndDateUtc = new DateTime(2100, 01, 01),
			StartVersion = new Version(1, 0).ToString(),
			EndVersion = new Version(999, 999).ToString(),
			LicensedCompany = "ACME Inc",
			LicensedProduct = "Anvil",
		};

		// The LicenceDetails should not be valid before signing
		var validation = originalLicenceDetails.Validate(GoodFileInfo.Name, _salt);
		Assert.False(validation.IsValid);
		originalLicenceDetails.Sign(GoodFileInfo.Name, _salt);
		Assert.NotNull(validation.ErrorMessage);
		Assert.Equal(LicenceDetails.SignatureIsNotValidForThisFileErrorMessage, validation.ErrorMessage);

		// The LicenceDetails should be valid after signing
		validation = originalLicenceDetails.Validate(GoodFileInfo.Name, _salt);
		Assert.True(validation.IsValid);
		Assert.Null(validation.ErrorMessage);

		// ... but only for that filename
		validation = originalLicenceDetails.Validate(BadFileInfo.Name, _salt);
		Assert.False(validation.IsValid);
		Assert.Equal(LicenceDetails.SignatureIsNotValidForThisFileErrorMessage, validation.ErrorMessage);

		// Write the license file
		new License<TestLicenceDetails>(originalLicenceDetails).WriteToFile(GoodFileInfo, _salt);

		// Read it back in - it should be valid
		var readBackLicense = new License<TestLicenceDetails>(GoodFileInfo);
		Assert.True(readBackLicense.Validate(_salt).IsValid);

		// ... but not with a different filename
		BadFileInfo.Delete();
		GoodFileInfo.MoveTo(BadFileInfo.FullName);
		readBackLicense = new License<TestLicenceDetails>(BadFileInfo);
		validation = readBackLicense.Validate(_salt);
		Assert.False(validation.IsValid);
		Assert.Equal(LicenceDetails.SignatureIsNotValidForThisFileErrorMessage, validation.ErrorMessage);

		// Clean-up by deleting the file
		BadFileInfo.Delete();
	}

	[Fact]
	public void TamperedSignatureShouldFailValidation()
	{
		var licenceDetails = new TestLicenceDetails {
			StartDateUtc = new DateTime(2001, 01, 01),
			EndDateUtc = new DateTime(2100, 01, 01),
			StartVersion = new Version(1, 0).ToString(),
			EndVersion = new Version(999, 999).ToString(),
			LicensedCompany = "ACME Inc",
			LicensedProduct = "Anvil",
		};

		licenceDetails.Sign(GoodFileInfo.Name, _salt);
		var signatureBytes = Convert.FromBase64String(licenceDetails.Signature!);
		signatureBytes[^1] ^= 1;
		licenceDetails.Signature = Convert.ToBase64String(signatureBytes);

		var validation = licenceDetails.Validate(GoodFileInfo.Name, _salt);

		Assert.False(validation.IsValid);
		Assert.Equal(LicenceDetails.SignatureIsNotValidForThisFileErrorMessage, validation.ErrorMessage);
	}

	[Fact]
	public void LackOfVersionShouldFailValidation()
	{
		// Create a LicenceDetails
		var badLicenceDetailsNoEndVersion = new TestLicenceDetails {
			StartDateUtc = new DateTime(2001, 01, 01),
			EndDateUtc = new DateTime(2100, 01, 01),
			StartVersion = new Version(1, 0).ToString(),
			LicensedCompany = "ACME Inc",
			LicensedProduct = "Anvil",
		};

		// Signed, this is still invalid as it is missing and EndVersion
		badLicenceDetailsNoEndVersion.Sign(GoodFileInfo.Name, _salt);
		var validation = badLicenceDetailsNoEndVersion.Validate(GoodFileInfo.Name, _salt);
		Assert.False(validation.IsValid);
		Assert.NotNull(validation.ErrorMessage);
		Assert.Equal(LicenceDetails.EndVersionErrorMessage, validation.ErrorMessage);
		// Create a LicenceDetails

		var badLicenceDetailsNoStartVersion = new TestLicenceDetails {
			StartDateUtc = new DateTime(2001, 01, 01),
			EndDateUtc = new DateTime(2100, 01, 01),
			EndVersion = new Version(1, 0).ToString(),
			LicensedCompany = "ACME Inc",
			LicensedProduct = "Anvil",
		};

		// Signed, this is still invalid as it is missing and EndVersion
		badLicenceDetailsNoStartVersion.Sign(GoodFileInfo.Name, _salt);
		validation = badLicenceDetailsNoStartVersion.Validate(GoodFileInfo.Name, _salt);
		Assert.False(validation.IsValid);
		Assert.NotNull(validation.ErrorMessage);
		Assert.Equal(LicenceDetails.StartVersionErrorMessage, validation.ErrorMessage);
	}

	[Fact]
	public void LackOfLicensedCompanyShouldFailValidation()
	{
		// Create a LicenceDetails
		var badLicenceDetailsNoLicensedCompany = new TestLicenceDetails {
			StartDateUtc = new DateTime(2001, 01, 01),
			EndDateUtc = new DateTime(2100, 01, 01),
			StartVersion = new Version(1, 0).ToString(),
			EndVersion = new Version(1, 9).ToString(),
			LicensedProduct = "Anvil",
		};

		// Signed, this is still invalid as it is missing and EndVersion
		badLicenceDetailsNoLicensedCompany.Sign(GoodFileInfo.Name, _salt);
		var validation = badLicenceDetailsNoLicensedCompany.Validate(GoodFileInfo.Name, _salt);
		Assert.False(validation.IsValid);
		Assert.NotNull(validation.ErrorMessage);
		Assert.Equal(LicenceDetails.NoLicensedCompanyErrorMessage, validation.ErrorMessage);
		// Create a LicenceDetails
	}

	[Fact]
	public void LackOfLicensedProductShouldFailValidation()
	{
		// Create a LicenceDetails
		var badLicenceDetailsNoLicensedProduct = new TestLicenceDetails {
			StartDateUtc = new DateTime(2001, 01, 01),
			EndDateUtc = new DateTime(2100, 01, 01),
			StartVersion = new Version(1, 0).ToString(),
			EndVersion = new Version(1, 9).ToString(),
			LicensedCompany = "ACME Inc",
		};

		// Signed, this is still invalid as it is missing and EndVersion
		badLicenceDetailsNoLicensedProduct.Sign(GoodFileInfo.Name, _salt);
		var validation = badLicenceDetailsNoLicensedProduct.Validate(GoodFileInfo.Name, _salt);
		Assert.False(validation.IsValid);
		Assert.NotNull(validation.ErrorMessage);
		Assert.Equal(LicenceDetails.NoLicensedProductErrorMessage, validation.ErrorMessage);
		// Create a LicenceDetails
	}
}
