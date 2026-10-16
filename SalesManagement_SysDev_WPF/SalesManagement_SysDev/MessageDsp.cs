using System;
using System.Collections.Generic;
using System.Windows;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// メッセージボックスの戻り値 (モジュール仕様書準拠)
    /// </summary>
    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7
    }

    /// <summary>
    /// メッセージ表示クラス (担当: 田中)
    /// </summary>
    public static class MessageDsp
    {
        /// <summary>
        /// 自動テスト用: trueの場合はダイアログを表示せず即時OKを返す
        /// </summary>
        public static bool IsSilentMode { get; set; } = false;

        private static readonly Dictionary<string, string> Messages = new()
        {
            { "M001", "登録が完了しました。" },
            { "M002", "更新が完了しました。" },
            { "M003", "削除が完了しました。" },
            { "M004", "入力内容に不備があります。" },
            { "E001", "対象のデータが見つかりません。" },
            { "E002", "既に存在するコードです。" },
            { "W001", "このデータを削除してもよろしいですか？" }
        };

        /// <summary>
        /// メッセージ番号に応じたメッセージを表示する
        /// </summary>
        public static DialogResult DspMsg(string msgNo)
        {
            string text = Messages.TryGetValue(msgNo, out var msg) 
                ? msg 
                : $"メッセージ番号 [{msgNo}] が通知されました。";

            if (IsSilentMode)
            {
                return DialogResult.OK;
            }

            MessageBoxResult result = MessageBox.Show(text, "販売管理システム", MessageBoxButton.OK, MessageBoxImage.Information);
            return result switch
            {
                MessageBoxResult.OK => DialogResult.OK,
                MessageBoxResult.Cancel => DialogResult.Cancel,
                MessageBoxResult.Yes => DialogResult.Yes,
                MessageBoxResult.No => DialogResult.No,
                _ => DialogResult.OK
            };
        }
    }
}
