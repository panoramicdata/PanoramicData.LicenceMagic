using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PanoramicData.LicenceMagic;

[Serializable]
public abstract class LicenceDetails
{
	private readonly byte[] _initVector = [];

	public static string SignatureIsNotValidForThisFileErrorMessage => "Signature is not valid for this file.";
	public static string StartDateErrorMessage => "License is not yet valid.  Check the StartDate";
	public static string EndDateErrorMessage => "License has expired.  Check the EndDate";
	public static string StartVersionErrorMessage => "License is not yet valid.  Check the StartVersion";
	public static string EndVersionErrorMessage => "License has expired.  Check the EndVersion";
	public static string NoLicensedCompanyErrorMessage => "No licensed company.";
	public static string NoLicensedProductErrorMessage => "No licensed product.";

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="initVector"></param>
	/// <exception cref="ArgumentException"></exception>
	protected LicenceDetails(byte[] initVector)
	{
		var initVectorBlockSize = Crypto.InitVectorSize;
		if (initVector.Length != initVectorBlockSize)
		{
			throw new ArgumentException($"Init vector must have length {initVectorBlockSize}", nameof(initVector));
		}
		_initVector = initVector;
	}

	/// <summary>
	/// Serialization Constructor
	/// </summary>
	protected LicenceDetails()
	{
	}

	public string? LicensedCompany { get; set; }
	public string? LicensedProduct { get; set; }
	public DateTime StartDateUtc { get; set; }
	public DateTime EndDateUtc { get; set; }
	public string? StartVersion { get; set; }
	public string? EndVersion { get; set; }
	public string? Signature { get; set; }

	public LicenceValidationResult Validate(string fileName, byte[] salt)
	{
		return ValidateSignature(fileName, salt)
			?? ValidateRequiredFields()
			?? ValidateDates(DateTime.UtcNow)
			?? ValidateVersions(GetType().Assembly.GetName().Version)
			?? LicenceValidationResult.Success;
	}

	private LicenceValidationResult? ValidateSignature(string fileName, byte[] salt)
		=> SignatureIsValid(fileName, salt) ? null : LicenceValidationResult.Failure(SignatureIsNotValidForThisFileErrorMessage);

	private LicenceValidationResult? ValidateRequiredFields()
	{
		if (LicensedCompany is null)
		{
			return LicenceValidationResult.Failure(NoLicensedCompanyErrorMessage);
		}

		return LicensedProduct is null ? LicenceValidationResult.Failure(NoLicensedProductErrorMessage) : null;
	}

	private LicenceValidationResult? ValidateDates(DateTime nowUtc)
	{
		if (nowUtc < StartDateUtc)
		{
			return LicenceValidationResult.Failure(StartDateErrorMessage);
		}

		return nowUtc > EndDateUtc ? LicenceValidationResult.Failure(EndDateErrorMessage) : null;
	}

	private LicenceValidationResult? ValidateVersions(Version? version)
	{
		if (StartVersion is null || version < new Version(StartVersion))
		{
			return LicenceValidationResult.Failure(StartVersionErrorMessage);
		}

		return EndVersion is null || version > new Version(EndVersion)
			? LicenceValidationResult.Failure(EndVersionErrorMessage)
			: null;
	}

	private bool SignatureIsValid(string fileName, byte[] salt)
	{
		if (Signature == null)
		{
			return false;
		}
		try
		{
			return SignatureString == Crypto.Decrypt(Signature, fileName, _initVector, salt);
		}
		catch (Exception exception) when (exception is CryptographicException or FormatException)
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
