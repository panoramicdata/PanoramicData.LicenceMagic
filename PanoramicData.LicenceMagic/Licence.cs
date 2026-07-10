using System;
using System.IO;
using System.Xml.Serialization;

namespace PanoramicData.LicenceMagic;

public class License<T> where T : LicenceDetails
{
	private readonly FileInfo? _fileInfo;
	private readonly T _licenceDetails;

	public License(FileInfo fileInfo)
	{
		_fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
		var serializer = new XmlSerializer(typeof(T));
		using (var streamReader = new StreamReader(_fileInfo.FullName))
		{
			_licenceDetails = (T?)serializer.Deserialize(streamReader)
				?? throw new InvalidDataException($"Could not deserialize licence file '{_fileInfo.FullName}'.");
		}
	}

	public License(T t)
	{
		_licenceDetails = t;
	}

	public void WriteToFile(FileInfo fileInfo, byte[] salt)
	{
		var validation = _licenceDetails.Validate(fileInfo.Name, salt);
		if (!validation.IsValid)
		{
			throw new InvalidOperationException($"Can't write an invalid license: {validation.ErrorMessage}");
		}

		var ser = new XmlSerializer(typeof(T));
		using (var writer = new StreamWriter(fileInfo.FullName))
		{
			ser.Serialize(writer, _licenceDetails);
		}
	}

	public LicenceValidationResult Validate(byte[] salt)
	{
		if (_fileInfo is null)
		{
			throw new InvalidOperationException("Validation without a filename is only available for licences loaded from a file.");
		}

		return _licenceDetails.Validate(_fileInfo.Name, salt);
	}
}
