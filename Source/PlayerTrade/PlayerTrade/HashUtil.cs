using System;
using System.Security.Cryptography;
using System.Text;

namespace PlayerTrade
{
    public static class HashUtil
    {
        private static readonly MD5 Md5 = new MD5Cng();

        public static int GenerateStableHashCode(this string input)
        {
            byte[] hash = Md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt32(hash, 0);
        }
    }
}
