using System;
using System.IO;
using Xunit;

namespace PanoramicData.LicenceMagic.Test
{
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
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				EndVersion = new Version(999, 999).ToString(),
				LicensedCompany = "ACME Inc",
				LicensedProduct = "Anvil",
			};

			// The LicenceDetails should not be valid before signing
			Assert.False(originalLicenceDetails.IsValid(out var errorMessage, GoodFileInfo.Name, _salt));
			originalLicenceDetails.Sign(GoodFileInfo.Name, _salt);
			Assert.NotNull(errorMessage);
			Assert.Equal(errorMessage, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

			// The LicenceDetails should be valid after signing
			Assert.True(originalLicenceDetails.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			Assert.Null(errorMessage);

			// ... but only for that filename
			Assert.False(originalLicenceDetails.IsValid(out errorMessage, BadFileInfo.Name, _salt));
			Assert.Equal(errorMessage, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

			// Write the license file
			new License<TestLicenceDetails>(originalLicenceDetails).WriteToFile(GoodFileInfo, _salt);

			// Read it back in - it should be valid
			var readBackLicense = new License<TestLicenceDetails>(GoodFileInfo);
			Assert.True(readBackLicense.IsValid(out errorMessage, _salt));

			// ... but not with a different filename
			BadFileInfo.Delete();
			GoodFileInfo.MoveTo(BadFileInfo.FullName);
			readBackLicense = new License<TestLicenceDetails>(BadFileInfo);
			Assert.False(readBackLicense.IsValid(out errorMessage, _salt));
			Assert.Equal(errorMessage, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

			// Clean-up by deleting the file
			BadFileInfo.Delete();
		}

		[Fact]
		public void LackOfVersionShouldFailValidation()
		{
			// Create a LicenceDetails
			var badLicenceDetailsNoEndVersion = new TestLicenceDetails {
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				LicensedCompany = "ACME Inc",
				LicensedProduct = "Anvil",
			};

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoEndVersion.Sign(GoodFileInfo.Name, _salt);
			Assert.False(badLicenceDetailsNoEndVersion.IsValid(out var errorMessage, GoodFileInfo.Name, _salt));
			Assert.NotNull(errorMessage);
			Assert.Equal(errorMessage, LicenceDetails.EndVersionErrorMessage);
			// Create a LicenceDetails

			var badLicenceDetailsNoStartVersion = new TestLicenceDetails {
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				EndVersion = new Version(1, 0).ToString(),
				LicensedCompany = "ACME Inc",
				LicensedProduct = "Anvil",
			};

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoStartVersion.Sign(GoodFileInfo.Name, _salt);
			Assert.False(badLicenceDetailsNoStartVersion.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			Assert.NotNull(errorMessage);
			Assert.Equal(errorMessage, LicenceDetails.StartVersionErrorMessage);
		}

		[Fact]
		public void LackOfLicensedCompanyShouldFailValidation()
		{
			// Create a LicenceDetails
			var badLicenceDetailsNoLicensedCompany = new TestLicenceDetails {
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				EndVersion = new Version(1, 9).ToString(),
				LicensedProduct = "Anvil",
			};

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoLicensedCompany.Sign(GoodFileInfo.Name, _salt);
			Assert.False(badLicenceDetailsNoLicensedCompany.IsValid(out var errorMessage, GoodFileInfo.Name, _salt));
			Assert.NotNull(errorMessage);
			Assert.Equal(errorMessage, LicenceDetails.NoLicensedCompanyErrorMessage);
			// Create a LicenceDetails
		}

		[Fact]
		public void LackOfLicensedProductShouldFailValidation()
		{
			// Create a LicenceDetails
			var badLicenceDetailsNoLicensedProduct = new TestLicenceDetails {
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				EndVersion = new Version(1, 9).ToString(),
				LicensedCompany = "ACME Inc",
			};

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoLicensedProduct.Sign(GoodFileInfo.Name, _salt);
			Assert.False(badLicenceDetailsNoLicensedProduct.IsValid(out var errorMessage, GoodFileInfo.Name, _salt));
			Assert.NotNull(errorMessage);
			Assert.Equal(errorMessage, LicenceDetails.NoLicensedProductErrorMessage);
			// Create a LicenceDetails
		}
	}
}