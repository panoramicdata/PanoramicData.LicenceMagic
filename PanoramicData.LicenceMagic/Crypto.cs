using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PanoramicData.LicenceMagic;

public static class Crypto
{
	/// <summary>
	/// The length of the InitVector array
	/// </summary>
	// This constant is used to determine the keysize of the encryption algorithm.
	private const int Keysize = 256;
	private const int Blocksize = 128;
	public const int InitVectorSize = Blocksize / 8;

	/// <summary>
	/// The encrypt function
	/// </summary>
	/// <param name="plainText"></param>
	/// <param name="passPhrase"></param>
	/// <param name="initVector">
	/// This constant string is used as a "salt" value for the Rfc2898DeriveBytes function calls.
	/// This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
	/// 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
	/// </param>
	/// <param name="salt">The salt</param>
	/// <returns></returns>
	public static string Encrypt(string plainText, string passPhrase, byte[] initVector, byte[] salt)
	{
		ValidateInitVector(initVector);

		var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
		var keyBytes = Rfc2898DeriveBytes.Pbkdf2(passPhrase, salt, 1000, HashAlgorithmName.SHA1, Keysize / 8);
		using (var symmetricKey = Aes.Create())
		{
			symmetricKey.Mode = CipherMode.CBC;
			using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, initVector))
			{
				using (var memoryStream = new MemoryStream())
				{
					using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
					{
						cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
						cryptoStream.FlushFinalBlock();
						var cipherTextBytes = memoryStream.ToArray();
						return Convert.ToBase64String(cipherTextBytes);
					}
				}
			}
		}
	}

	private static void ValidateInitVector(byte[] initVector)
	{
		if (initVector.Length != InitVectorSize) throw new ArgumentException($"Init vector must have length {InitVectorSize}", nameof(initVector));
	}

	public static string Decrypt(string cipherText, string passPhrase, byte[] initVector, byte[] salt)
	{
		ValidateInitVector(initVector);

		var cipherTextBytes = Convert.FromBase64String(cipherText);
		var keyBytes = Rfc2898DeriveBytes.Pbkdf2(passPhrase, salt, 1000, HashAlgorithmName.SHA1, Keysize / 8);
		using (var symmetricKey = Aes.Create())
		{
			symmetricKey.Mode = CipherMode.CBC;
			using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, initVector))
			{
				using (var memoryStream = new MemoryStream(cipherTextBytes))
				{
					using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
					{
						using (var streamReader = new StreamReader(cryptoStream, Encoding.UTF8))
						{
							return streamReader.ReadToEnd();
						}
					}
				}
			}
		}
	}
}
