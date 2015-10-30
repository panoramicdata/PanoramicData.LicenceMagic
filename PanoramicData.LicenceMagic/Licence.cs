using System;
using System.IO;
using System.Xml.Serialization;

namespace PanoramicData.LicenceMagic
{
	public class License<T> where T : LicenceDetails
	{
		private readonly FileInfo _fileInfo;
		readonly T _LicenceDetails;

		public License(FileInfo fileInfo)
		{
			if(fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));
			_fileInfo = fileInfo;
			var serializer = new XmlSerializer(typeof(T));
			using (var streamReader = new StreamReader(_fileInfo.FullName))
			{
				_LicenceDetails = (T)serializer.Deserialize(streamReader);
			}
		}

		public License(T t)
		{
			_LicenceDetails = t;
		}

		public void WriteToFile(FileInfo fileInfo, byte[] salt)
		{
			string errorMessage;
			if (!_LicenceDetails.IsValid(out errorMessage, fileInfo.Name, salt))
			{
				throw new InvalidOperationException($"Can't write an invalid license: {errorMessage}");
			}

			var ser = new XmlSerializer(typeof (T));
			using (var writer = new StreamWriter(fileInfo.FullName))
			{
				ser.Serialize(writer, _LicenceDetails);
				writer.Close();
			}
		}

		public bool IsValid(out string errorMessage, byte[] salt)
		{
			return _LicenceDetails.IsValid(out errorMessage, _fileInfo.Name, salt);
		}
	}
}
