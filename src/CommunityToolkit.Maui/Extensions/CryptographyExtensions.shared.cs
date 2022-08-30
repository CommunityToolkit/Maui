namespace CommunityToolkit.Maui.Extensions;

using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>Cryptography extensions.</summary>
public static class CryptographyExtensions
{
	/// <summary>A hash function producing a 128-bit hash value (16 Bytes, 32 Hexadecimal characters)</summary>
	/// <param name="source">Source string.</param>
	/// <param name="separator">Separator.</param>
	/// <returns>Array of 16 bytes.</returns>
	public static string GetMd5Hash(this string source, string separator = "-")
	{
		ReadOnlySpan<char> str = source;
		using var md5 = MD5.Create();
		Span<byte> hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str.ToArray()));
		return BitConverter.ToString(hash.ToArray(), 0, hash.Length).Replace("-", separator, StringComparison.OrdinalIgnoreCase);
	}
}