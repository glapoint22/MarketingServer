using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class Encryption
{
    public static string Decrypt(string IV, string notification)
    {
        string decryptedString = null;

        byte[] inputBytes = Encoding.UTF8.GetBytes("BEWAREOBLIVIONIS");

        SHA1 sha1 = SHA1.Create();
        byte[] key = sha1.ComputeHash(inputBytes);

        StringBuilder hex = new StringBuilder(key.Length * 2);
        foreach (byte b in key)
            hex.AppendFormat("{0:x2}", b);

        string secondPhaseKey = hex.ToString().Substring(0, 32);

        ASCIIEncoding asciiEncoding = new ASCIIEncoding();

        byte[] keyBytes = asciiEncoding.GetBytes(secondPhaseKey);
        byte[] iv = Convert.FromBase64String(IV);

        try
        {
            using (RijndaelManaged rijndaelManaged = new RijndaelManaged
            {
                Key = keyBytes,
                IV = iv,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            using (Stream memoryStream = new MemoryStream(Convert.FromBase64String(notification)))
            using (CryptoStream cryptoStream =
               new CryptoStream(memoryStream,
                   rijndaelManaged.CreateDecryptor(keyBytes, iv),
                   CryptoStreamMode.Read))
            {
                decryptedString = new StreamReader(cryptoStream).ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            //Trace.WriteLine(new TraceData(TraceCategoryEnum.Errors, "Error decrypting message: " + ex.Message));
        }

        return decryptedString;
    }

}