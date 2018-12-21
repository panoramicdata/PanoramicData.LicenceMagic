using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PanoramicData.LicenceMagic
{
	[Serializable]
	public abstract class LicenceDetails
	{
		private readonly byte[] _initVector;

		public const string SignatureIsNotValidForThisFileErrorMessage = "Signature is not valid for this file.";
		public const string StartDateErrorMessage = "License is not yet valid.  Check the StartDate";
		public const string EndDateErrorMessage = "License has expired.  Check the EndDate";
		public const string StartVersionErrorMessage = "License is not yet valid.  Check the StartVersion";
		public const string EndVersionErrorMessage = "License has expired.  Check the EndVersion";
		public const string NoLicensedCompanyErrorMessage = "No licensed company.";
		public const string NoLicensedProductErrorMessage = "No licensed product.";

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initVector"></param>
		/// <exception cref="ArgumentException"></exception>
		protected LicenceDetails(byte[] initVector)
		{
			const int initVectorBlockSize = Crypto.InitVectorSize;
			if (initVector.Length != initVectorBlockSize) throw new ArgumentException($"Init vector must have length {initVectorBlockSize}", nameof(initVector));
			_initVector = initVector;
		}

		/// <summary>
		/// Serialization Constructor
		/// </summary>
		protected LicenceDetails()
		{
		}

		public string LicensedCompany { get; set; }
		public string LicensedProduct { get; set; }
		public DateTime StartDateUtc { get; set; }
		public DateTime EndDateUtc { get; set; }
		public string StartVersion { get; set; }
		public string EndVersion { get; set; }
		public string Signature { get; set; }

		public bool IsValid(out string errorMessage, string fileName, byte[] salt)
		{
			// Check signature unless in debug
			if (!SignatureIsValid(fileName, salt))
			{
				errorMessage = SignatureIsNotValidForThisFileErrorMessage;
				return false;
			}

			// Check licensed company
			if (LicensedCompany == null)
			{
				errorMessage = NoLicensedCompanyErrorMessage;
				return false;
			}

			// Check licensed product
			if (LicensedProduct == null)
			{
				errorMessage = NoLicensedProductErrorMessage;
				return false;
			}

			// Check start and end date
			var nowUtc = DateTime.UtcNow;
			if (nowUtc < StartDateUtc)
			{
				errorMessage = StartDateErrorMessage;
				return false;
			}
			if (nowUtc > EndDateUtc)
			{
				errorMessage = EndDateErrorMessage;
				return false;
			}

			// Check assembly version
			var version = GetType().Assembly.GetName().Version;
			if (StartVersion == null || version < new Version(StartVersion))
			{
				errorMessage = StartVersionErrorMessage;
				return false;
			}
			if (EndVersion == null || version > new Version(EndVersion))
			{
				errorMessage = EndVersionErrorMessage;
				return false;
			}

			errorMessage = null;
			return true;
		}

		private bool SignatureIsValid(string fileName, byte[] salt)
		{
			if (Signature == null) return false;
			try
			{
				return SignatureString == Crypto.Decrypt(Signature, fileName, _initVector, salt);
			}
			catch (CryptographicException)
			{
				return false;
			}
		}

		public void Sign(string fileName, byte[] salt)
		{
			Signature = Crypto.Encrypt(SignatureString, fileName, _initVector, salt);
		}

		private string SignatureString => $"{LicensedCompany}{LicensedProduct}{StartDateUtc}{EndDateUtc}{StartVersion}{EndVersion}";

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			var serializer = new XmlSerializer(GetType());
			var settings = new XmlWriterSettings {
				Encoding = new UnicodeEncoding(false, false),
				Indent = false,
				OmitXmlDeclaration = false
			};

			using (var textWriter = new StringWriter())
			{
				using (var xmlWriter = XmlWriter.Create(textWriter, settings))
				{
					serializer.Serialize(xmlWriter, this);
				}
				return textWriter.ToString();
			}
		}
	}
}