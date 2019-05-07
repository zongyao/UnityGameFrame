using System.Security.Cryptography;
using System.Text;

public class MD5Util
{
	public static string GetStringMD5 (string target)
	{
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes (target ?? "");
		return GetBytesMD5 (bytes);
	}

	public static string GetBytesMD5 (byte[] bytes)
	{
		MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider ();
		bytes = md5.ComputeHash (bytes);
		StringBuilder sb = new StringBuilder ();
		for (int i = 0; i < bytes.Length; i++) {
			sb.Append (bytes [i].ToString ("X").PadLeft (2, '0'));
		}
		return sb.ToString ();
	}
}