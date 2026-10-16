using System;
using System.Security.Cryptography;
using System.Text;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// パスワードのハッシュ化クラス (担当: 田中)
    /// </summary>
    public static class PasswordHash
    {
        /// <summary>
        /// パスワードをSHA256でハッシュ化する
        /// </summary>
        public static string CreatePasswordHash(string password)
        {
            if (password == null) return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
