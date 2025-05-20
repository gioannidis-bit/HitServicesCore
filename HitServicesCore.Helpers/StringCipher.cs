using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HitServicesCore.Helpers;

public class StringCipher
{
	private string value;

	private const int Keysize = 128;

	private const int DerivationIterations = 1000;

	public void Test(string value)
	{
		this.value = value;
	}

	public string Set(string plainText)
	{
		byte[] saltStringBytes = Generate128BitsOfRandomEntropy();
		byte[] ivStringBytes = Generate128BitsOfRandomEntropy();
		byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
		using Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(value, saltStringBytes, 1000);
		byte[] keyBytes = password.GetBytes(16);
		using RijndaelManaged symmetricKey = new RijndaelManaged();
		symmetricKey.BlockSize = 128;
		symmetricKey.Mode = CipherMode.CBC;
		symmetricKey.Padding = PaddingMode.PKCS7;
		using ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes);
		using MemoryStream memoryStream = new MemoryStream();
		using CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
		cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
		cryptoStream.FlushFinalBlock();
		byte[] cipherTextBytes = saltStringBytes;
		cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
		cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
		memoryStream.Close();
		cryptoStream.Close();
		return Convert.ToBase64String(cipherTextBytes);
	}

	public string Get(string cipherText)
	{
		if (string.IsNullOrWhiteSpace(cipherText))
		{
			return cipherText;
		}
		try
		{
			byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
			byte[] saltStringBytes = cipherTextBytesWithSaltAndIv.Take(16).ToArray();
			byte[] ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(16).Take(16).ToArray();
			byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(32).Take(cipherTextBytesWithSaltAndIv.Length - 32).ToArray();
			using Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(value, saltStringBytes, 1000);
			byte[] keyBytes = password.GetBytes(16);
			using RijndaelManaged symmetricKey = new RijndaelManaged();
			symmetricKey.BlockSize = 128;
			symmetricKey.Mode = CipherMode.CBC;
			symmetricKey.Padding = PaddingMode.PKCS7;
			using ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
			using MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
			using CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
			byte[] plainTextBytes = new byte[cipherTextBytes.Length];
			int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
			memoryStream.Close();
			cryptoStream.Close();
			return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
		}
		catch
		{
			return "";
		}
	}

	private byte[] Generate256BitsOfRandomEntropy()
	{
		byte[] randomBytes = new byte[32];
		using RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
		rngCsp.GetBytes(randomBytes);
		return randomBytes;
	}

	private byte[] Generate128BitsOfRandomEntropy()
	{
		byte[] randomBytes = new byte[16];
		using RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
		rngCsp.GetBytes(randomBytes);
		return randomBytes;
	}

	public string ComputeSha256Hash(string rawData)
	{
		using SHA256 sha256Hash = SHA256.Create();
		byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
		StringBuilder builder = new StringBuilder();
		for (int i = 0; i < bytes.Length; i++)
		{
			builder.Append(bytes[i].ToString("x2"));
		}
		return builder.ToString();
	}
}
