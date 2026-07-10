using System;
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
	private const int NonceSize = 12;
	private const int TagSize = 16;
	private const int Pbkdf2Iterations = 100_000;
	public static int InitVectorSize => 16;

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

		var plaintext = Encoding.UTF8.GetBytes(plainText);
		var key = DeriveKey(passPhrase, salt);
		var nonce = RandomNumberGenerator.GetBytes(NonceSize);
		var ciphertext = new byte[plaintext.Length];
		var tag = new byte[TagSize];

		using var aes = new AesGcm(key, TagSize);
		aes.Encrypt(nonce, plaintext, ciphertext, tag, initVector);

		var payload = new byte[NonceSize + TagSize + ciphertext.Length];
		nonce.CopyTo(payload, 0);
		tag.CopyTo(payload, NonceSize);
		ciphertext.CopyTo(payload, NonceSize + TagSize);
		return Convert.ToBase64String(payload);
	}

	private static void ValidateInitVector(byte[] initVector)
	{
		if (initVector.Length != InitVectorSize)
		{
			throw new ArgumentException($"Init vector must have length {InitVectorSize}", nameof(initVector));
		}
	}

	public static string Decrypt(string cipherText, string passPhrase, byte[] initVector, byte[] salt)
	{
		ValidateInitVector(initVector);

		var payload = Convert.FromBase64String(cipherText);
		if (payload.Length < NonceSize + TagSize)
		{
			throw new CryptographicException("The encrypted payload is invalid.");
		}

		var nonce = payload.AsSpan(0, NonceSize);
		var tag = payload.AsSpan(NonceSize, TagSize);
		var ciphertext = payload.AsSpan(NonceSize + TagSize);
		var plaintext = new byte[ciphertext.Length];
		var key = DeriveKey(passPhrase, salt);

		using var aes = new AesGcm(key, TagSize);
		aes.Decrypt(nonce, ciphertext, tag, plaintext, initVector);
		return Encoding.UTF8.GetString(plaintext);
	}

	private static byte[] DeriveKey(string passPhrase, byte[] salt)
		=> Rfc2898DeriveBytes.Pbkdf2(passPhrase, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, Keysize / 8);
}
