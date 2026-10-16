using System;
using System.Diagnostics;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 印刷プレビュー作成クラス (担当: 田中)
    /// </summary>
    public class DataGridViewPrinter
    {
        public string LastPreviewName { get; private set; } = string.Empty;

        /// <summary>
        /// 印刷プレビューの作成
        /// </summary>
        /// <param name="printName">印刷名 (ドキュメントタイトル)</param>
        public void Preview(string printName)
        {
            LastPreviewName = printName ?? "帳票印刷";
            Debug.WriteLine($"[DataGridViewPrinter] 印刷プレビューを作成しました: {LastPreviewName}");
        }
    }
}
