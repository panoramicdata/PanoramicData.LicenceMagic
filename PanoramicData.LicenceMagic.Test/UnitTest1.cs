using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PanoramicData.LicenceMagic.Licences;

namespace PanoramicData.LicenceMagic.Test
{
	[TestClass]
	public class LicenseTests
	{
		private static readonly FileInfo GoodFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test.lic"));
		private static readonly FileInfo BadFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "test1.lic"));
		private readonly byte[] _salt = {0,1,2,3,4,5,6,7};

		[TestMethod]
		public void SaveToFileAndReload()
		{
			// Create a LicenceDetails
			var originalLicenceDetails = new TestLicenceDetails
			{
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				EndVersion = new Version(999, 999).ToString(),
				LicensedCompany = "ACME Inc",
				LicensedProduct = "Anvil",
			};

			string errorMessage;

			// The LicenceDetails should not be valid before signing
			Assert.IsFalse(originalLicenceDetails.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			originalLicenceDetails.Sign(GoodFileInfo.Name, _salt);
			Assert.IsNotNull(errorMessage);
			Assert.AreEqual(errorMessage, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

			// The LicenceDetails should be valid after signing
			Assert.IsTrue(originalLicenceDetails.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			Assert.IsNull(errorMessage);

			// ... but only for that filename
			Assert.IsFalse(originalLicenceDetails.IsValid(out errorMessage, BadFileInfo.Name, _salt));
			Assert.AreEqual(errorMessage, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

			// Write the license file
			new License<TestLicenceDetails>(originalLicenceDetails).WriteToFile(GoodFileInfo, _salt);

			// Read it back in - it should be valid
			var readBackLicense = new License<TestLicenceDetails>(GoodFileInfo);
			Assert.IsTrue(readBackLicense.IsValid(out errorMessage, _salt));

			// ... but not with a different filename
			BadFileInfo.Delete();
			GoodFileInfo.MoveTo(BadFileInfo.FullName);
			readBackLicense = new License<TestLicenceDetails>(BadFileInfo);
			Assert.IsFalse(readBackLicense.IsValid(out errorMessage, _salt));
			Assert.AreEqual(errorMessage, LicenceDetails.SignatureIsNotValidForThisFileErrorMessage);

			// Clean-up by deleting the file
			BadFileInfo.Delete();
		}

		[TestMethod]
		public void LackOfVersionShouldFailValidation()
		{
			// Create a LicenceDetails
			var badLicenceDetailsNoEndVersion = new TestLicenceDetails
			{
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				LicensedCompany = "ACME Inc",
				LicensedProduct = "Anvil",
			};

			string errorMessage;

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoEndVersion.Sign(GoodFileInfo.Name, _salt);
			Assert.IsFalse(badLicenceDetailsNoEndVersion.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			Assert.IsNotNull(errorMessage);
			Assert.AreEqual(errorMessage, LicenceDetails.EndVersionErrorMessage);
			// Create a LicenceDetails

			var badLicenceDetailsNoStartVersion = new TestLicenceDetails
			{
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				EndVersion = new Version(1, 0).ToString(),
				LicensedCompany = "ACME Inc",
				LicensedProduct = "Anvil",
			};

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoStartVersion.Sign(GoodFileInfo.Name, _salt);
			Assert.IsFalse(badLicenceDetailsNoStartVersion.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			Assert.IsNotNull(errorMessage);
			Assert.AreEqual(errorMessage, LicenceDetails.StartVersionErrorMessage);
		}

		[TestMethod]
		public void LackOfLicensedCompanyShouldFailValidation()
		{
			// Create a LicenceDetails
			var badLicenceDetailsNoLicensedCompany = new TestLicenceDetails
			{
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				EndVersion = new Version(1, 9).ToString(),
				LicensedProduct = "Anvil",
			};

			string errorMessage;

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoLicensedCompany.Sign(GoodFileInfo.Name, _salt);
			Assert.IsFalse(badLicenceDetailsNoLicensedCompany.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			Assert.IsNotNull(errorMessage);
			Assert.AreEqual(errorMessage, LicenceDetails.NoLicensedCompanyErrorMessage);
			// Create a LicenceDetails
		}

		[TestMethod]
		public void LackOfLicensedProductShouldFailValidation()
		{
			// Create a LicenceDetails
			var badLicenceDetailsNoLicensedProduct = new TestLicenceDetails
			{
				StartDateUtc = new DateTime(2001, 01, 01),
				EndDateUtc = new DateTime(2020, 01, 01),
				StartVersion = new Version(1, 0).ToString(),
				EndVersion = new Version(1, 9).ToString(),
				LicensedCompany = "ACME Inc",
			};

			string errorMessage;

			// Signed, this is still invalid as it is missing and EndVersion
			badLicenceDetailsNoLicensedProduct.Sign(GoodFileInfo.Name, _salt);
			Assert.IsFalse(badLicenceDetailsNoLicensedProduct.IsValid(out errorMessage, GoodFileInfo.Name, _salt));
			Assert.IsNotNull(errorMessage);
			Assert.AreEqual(errorMessage, LicenceDetails.NoLicensedProductErrorMessage);
			// Create a LicenceDetails
		}
	}
}