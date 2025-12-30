using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TurkceRumenceCeviri.Utilities
{
    public static class DeviceCodeGenerator
    {
        public static string CreateDeviceCode(string hwid, string salt = "AppSalt_v1")
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(hwid + salt));
            var num = BitConverter.ToUInt64(bytes, 0) ^ BitConverter.ToUInt64(bytes, 8);
            const string alphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            var code = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                code.Append(alphabet[(int)(num % (uint)alphabet.Length)]);
                num /= (uint)alphabet.Length;
            }
            var s = code.ToString();
            return string.Join("-", Enumerable.Range(0, 4).Select(i => s.Substring(i * 4, 4)));
        }
    }
}
