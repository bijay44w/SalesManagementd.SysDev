using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 文字コード誤認による文字化け（Shift-JIS/CP932 が Windows-1252 として解釈されたケース）の自動復元ユーティリティ
    /// </summary>
    public static class StringSanitizer
    {
        private static readonly Regex MojibakePattern = new Regex(
            @"[\u0080-\u009FäÝ‹Ž˜Ÿ¬âƒ•]",
            RegexOptions.Compiled);

        /// <summary>
        /// 文字化け文字列を検知し、正しい日本語文字列に自動復元します。
        /// 正常な文字列や復元できない場合はそのまま返します。
        /// </summary>
        public static string FixMojibake(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input ?? string.Empty;
            }

            if (!MojibakePattern.IsMatch(input))
            {
                return input;
            }

            try
            {
                var cp1252 = Encoding.GetEncoding(1252);
                var cp932 = Encoding.GetEncoding(932);

                byte[] bytes = cp1252.GetBytes(input);
                string recovered = cp932.GetString(bytes);

                // 正常に復元され、未割当文字記号(?)が含まれていなければ採用
                if (!string.IsNullOrWhiteSpace(recovered) && !recovered.Contains('?'))
                {
                    return recovered;
                }
            }
            catch
            {
                // フォールバック
            }

            return input;
        }
    }
}
