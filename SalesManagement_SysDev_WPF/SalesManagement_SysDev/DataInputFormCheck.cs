using System;
using System.Text.RegularExpressions;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 入力フォームのバリデーションチェッククラス (担当: 鈴木)
    /// </summary>
    public static class DataInputFormCheck
    {
        /// <summary>
        /// 全角文字のチェック (すべての文字が全角であるか判定)
        /// 結果: True=異常なし(全角のみ), False=異常あり
        /// </summary>
        public static bool CheckFullWidth(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            // 半角文字 (ASCII 0x20-0x7E または 半角カナ 0xFF61-0xFF9F) が含まれていれば異常(false)
            foreach (char c in value)
            {
                if ((c >= 0x0020 && c <= 0x007E) || (c >= 0xFF61 && c <= 0xFF9F))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 半角英数字のチェック (すべての文字が半角英数字であるか判定)
        /// 結果: True=異常なし, False=異常あり
        /// </summary>
        public static bool CheckHalfAlphabetNumeric(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Regex.IsMatch(value, @"^[a-zA-Z0-9]+$");
        }

        /// <summary>
        /// 半角カタカナのチェック
        /// 結果: True=異常なし, False=異常あり
        /// </summary>
        public static bool CheckHalfWidthKatakana(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Regex.IsMatch(value, @"^[\uFF61-\uFF9F]+$");
        }

        /// <summary>
        /// 数値のチェック (整数または数値形式)
        /// 結果: True=異常なし, False=異常あり
        /// </summary>
        public static bool CheckNumeric(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Regex.IsMatch(value, @"^[0-9]+$");
        }

        /// <summary>
        /// メールアドレス形式チェック
        /// 結果: True=異常なし, False=異常あり
        /// </summary>
        public static bool CheckMailAddress(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// 半角文字のチェック (すべての文字が半角であるか判定)
        /// 結果: True=異常なし, False=異常あり
        /// </summary>
        public static bool CheckHalfChar(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            // 全ての文字が半角ASCII (0x20-0x7E) または 半角カナ (0xFF61-0xFF9F)
            foreach (char c in value)
            {
                bool isHalf = (c >= 0x0020 && c <= 0x007E) || (c >= 0xFF61 && c <= 0xFF9F);
                if (!isHalf) return false;
            }
            return true;
        }
    }
}
