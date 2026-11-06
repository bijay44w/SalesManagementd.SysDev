using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// Receiving.xaml の相互作用ロジック
    /// </summary>
    public partial class Receiving : UserControl
    {
        public Receiving()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 入庫IDの入力内容を発注ID欄に自動コピーする
        /// </summary>
        private void txt_SearchWaId_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (txt_SearchHaId.Text != txt_SearchWaId.Text)
            {
                txt_SearchHaId.Text = txt_SearchWaId.Text;
                txt_SearchHaId.CaretIndex =
                    txt_SearchHaId.Text.Length;
            }
        }

        /// <summary>
        /// 検索条件をクリアする
        /// </summary>
        private void btn_Clear_Click(
            object sender, RoutedEventArgs e)
        {
            txt_SearchWaId.Clear();
            txt_SearchHaId.Clear();
            txt_SearchProductName.Clear();
            chk_IncludeHidden.IsChecked = false;
        }

        /// <summary>
        /// 入庫情報を検索する
        /// </summary>
        private void btn_Search_Click(
            object sender, RoutedEventArgs e)
        {
            ShowNotImplemented("入庫情報の検索");
        }

        /// <summary>
        /// 新規入庫登録画面を開く
        /// </summary>
        private void btn_NewRegister_Click(
            object sender, RoutedEventArgs e)
        {
            ShowNotImplemented("新規入庫登録");
        }

        /// <summary>
        /// 選択した入庫情報を編集する
        /// </summary>
        private void btn_EditSelected_Click(
            object sender, RoutedEventArgs e)
        {
            ShowNotImplemented("入庫情報の編集");
        }

        /// <summary>
        /// 選択した入庫情報を非表示にする
        /// </summary>
        private void btn_HideSelected_Click(
            object sender, RoutedEventArgs e)
        {
            ShowNotImplemented("入庫情報の非表示設定");
        }

        /// <summary>
        /// 発注選択時のイベント
        /// </summary>
        private void cmb_HaId_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            // 発注明細の商品を読み込む処理は、
            // 既存のデータベース構成を確認してから実装する。
        }

        /// <summary>
        /// 登録・編集画面を閉じる
        /// </summary>
        private void btn_CancelEditor_Click(
            object sender, RoutedEventArgs e)
        {
            modal_Editor.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 入庫情報の登録・更新を実行する
        /// </summary>
        private void btn_ExecuteEditor_Click(
            object sender, RoutedEventArgs e)
        {
            ShowNotImplemented("入庫情報の登録・更新");
        }

        /// <summary>
        /// 登録・編集画面の背景クリックで閉じる
        /// </summary>
        private void modal_Editor_Backdrop_Click(
            object sender, MouseButtonEventArgs e)
        {
            modal_Editor.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 非表示設定画面を閉じる
        /// </summary>
        private void btn_CancelHide_Click(
            object sender, RoutedEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 入庫情報の非表示設定を実行する
        /// </summary>
        private void btn_ExecuteHide_Click(
            object sender, RoutedEventArgs e)
        {
            ShowNotImplemented("入庫情報の非表示設定");
        }

        /// <summary>
        /// 非表示設定画面の背景クリックで閉じる
        /// </summary>
        private void modal_Hide_Backdrop_Click(
            object sender, MouseButtonEventArgs e)
        {
            modal_Hide.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 未実装の処理を明示する
        /// </summary>
        private static void ShowNotImplemented(string feature)
        {
            MessageBox.Show(
                $"{feature}の処理は、まだ実装されていません。",
                "入庫管理",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void chk_IncludeHidden_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
