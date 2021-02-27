using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PlayerTrade
{
    public static class HashUtil
    {
        private static readonly MD5 Md5 = new MD5Cng();

        public static int GenerateStableHashCode(this string input, bool absolute = false)
        {
            byte[] hash = Md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            int i = BitConverter.ToInt32(hash, 0);
            if (absolute)
                i = Mathf.Abs(i);
            return i;
        }
    }
}
