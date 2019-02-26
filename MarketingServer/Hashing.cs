using System;
using System.Security.Cryptography;

namespace MarketingServer
{
    public class Hashing
    {
        public static string GenerateSalt()
        {
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

            // Generate the salt
            byte[] data = new byte[32];
            rngCsp.GetBytes(data);
            return Convert.ToBase64String(data);
        }

        public static string GetHash(string input)
        {
            HashAlgorithm hashAlgorithm = new SHA256CryptoServiceProvider();

            byte[] byteValue = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] byteHash = hashAlgorithm.ComputeHash(byteValue);
            return Convert.ToBase64String(byteHash);
        }
    }
}