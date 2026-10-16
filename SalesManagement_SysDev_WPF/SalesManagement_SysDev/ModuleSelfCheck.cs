using System;
using System.Collections.Generic;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// モジュール一覧 仕様書に定義された全クラス・全メソッドの自動テストスイート
    /// </summary>
    public static class ModuleSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: モジュール一覧 実装検証テスト");
            Console.WriteLine("==================================================");

            int totalTests = 0;
            int passedTests = 0;

            void Assert(bool condition, string testName)
            {
                totalTests++;
                if (condition)
                {
                    passedTests++;
                    Console.WriteLine($"[PASS] {testName}");
                }
                else
                {
                    Console.WriteLine($"[FAIL] {testName}");
                    throw new Exception($"Test assertion failed: {testName}");
                }
            }

            try
            {
                // 1. DataGridViewPrinter
                Console.WriteLine("\n--- 1. DataGridViewPrinter (田中) ---");
                var printer = new DataGridViewPrinter();
                printer.Preview("売上帳票プレビュー");
                Assert(printer.LastPreviewName == "売上帳票プレビュー", "DataGridViewPrinter.Preview: 印刷プレビュー作成");

                // 2. DataInputFormCheck
                Console.WriteLine("\n--- 2. DataInputFormCheck (鈴木) ---");
                Assert(DataInputFormCheck.CheckFullWidth("アイウエオ田中"), "CheckFullWidth: 全角文字列でTrue");
                Assert(!DataInputFormCheck.CheckFullWidth("アイウabc"), "CheckFullWidth: 半角混在でFalse");

                Assert(DataInputFormCheck.CheckHalfAlphabetNumeric("Abc123XYZ"), "CheckHalfAlphabetNumeric: 半角英数字でTrue");
                Assert(!DataInputFormCheck.CheckHalfAlphabetNumeric("Abc 123"), "CheckHalfAlphabetNumeric: 空白混在でFalse");
                Assert(!DataInputFormCheck.CheckHalfAlphabetNumeric("あいう123"), "CheckHalfAlphabetNumeric: 全角混在でFalse");

                Assert(DataInputFormCheck.CheckHalfWidthKatakana("ｱｲｳｴｵ"), "CheckHalfWidthKatakana: 半角カタカナでTrue");
                Assert(!DataInputFormCheck.CheckHalfWidthKatakana("アイウエオ"), "CheckHalfWidthKatakana: 全角カタカナでFalse");

                Assert(DataInputFormCheck.CheckNumeric("1234567890"), "CheckNumeric: 数字のみでTrue");
                Assert(!DataInputFormCheck.CheckNumeric("123a"), "CheckNumeric: 文字混在でFalse");

                Assert(DataInputFormCheck.CheckMailAddress("test@example.com"), "CheckMailAddress: 正しい形式でTrue");
                Assert(!DataInputFormCheck.CheckMailAddress("test.example.com"), "CheckMailAddress: 不正形式でFalse");

                Assert(DataInputFormCheck.CheckHalfChar("abc123DEF!@#"), "CheckHalfChar: 半角文字でTrue");
                Assert(!DataInputFormCheck.CheckHalfChar("abc123漢字"), "CheckHalfChar: 全角混在でFalse");

                // 3. MessageDsp
                Console.WriteLine("\n--- 3. MessageDsp (田中) ---");
                MessageDsp.IsSilentMode = true;
                var msgResult = MessageDsp.DspMsg("M001");
                Assert(msgResult == DialogResult.OK, "MessageDsp.DspMsg: メッセージ表示処理");

                // 4. PasswordHash
                Console.WriteLine("\n--- 4. PasswordHash (田中) ---");
                string hash1 = PasswordHash.CreatePasswordHash("Password123");
                string hash2 = PasswordHash.CreatePasswordHash("Password123");
                string hash3 = PasswordHash.CreatePasswordHash("AnotherPass");
                Assert(!string.IsNullOrEmpty(hash1) && hash1.Length == 64, "PasswordHash: 64文字ハッシュ生成");
                Assert(hash1 == hash2, "PasswordHash: 同一パスワードで一致");
                Assert(hash1 != hash3, "PasswordHash: 異なるパスワードで不一致");

                // 5. DivisionDataAccess
                Console.WriteLine("\n--- 5. DivisionDataAccess (小林) ---");
                var divAccess = new DivisionDataAccess();
                Assert(divAccess.CheckDivisionCDExistence("01"), "DivisionDataAccess.CheckDivisionCDExistence: 既存コード存在確認");
                Assert(!divAccess.CheckDivisionCDExistence("999"), "DivisionDataAccess.CheckDivisionCDExistence: 未存在コード非存在確認");

                Assert(divAccess.SelectDivisionExistenceCheck("営業"), "DivisionDataAccess.SelectDivisionExistenceCheck: 部分一致確認");
                Assert(!divAccess.SelectDivisionExistenceCheck("存在しない部署名"), "DivisionDataAccess.SelectDivisionExistenceCheck: 非一致確認");

                Assert(divAccess.AddDivisionData(new M_Division { DivisionCD = "99", DivisionName = "品質管理部" }), "DivisionDataAccess.AddDivisionData: 登録処理");
                Assert(divAccess.CheckDivisionCDExistence("99"), "DivisionDataAccess: 登録後存在確認");

                Assert(divAccess.UpdateDivisionData(new M_Division { DivisionCD = "99", DivisionName = "品質保証部" }), "DivisionDataAccess.UpdateDivisionData: 更新処理");
                Assert(divAccess.SelectDivisionExistenceCheck("品質保証"), "DivisionDataAccess: 更新後名称確認");

                Assert(divAccess.CheckCascadeStaff("01"), "DivisionDataAccess.CheckCascadeStaff: スタッフ利用中確認");
                Assert(!divAccess.CheckCascadeStaff("99"), "DivisionDataAccess.CheckCascadeStaff: スタッフ未利用確認");

                var allDivs = divAccess.GetDivisionData();
                Assert(allDivs.Count >= 4, "DivisionDataAccess.GetDivisionData: 全件取得");

                var filterDivs = divAccess.GetDivisionData(new M_Division { DivisionName = "品質保証" });
                Assert(filterDivs.Count == 1 && filterDivs[0].DivisionCD == "99", "DivisionDataAccess.GetDivisionData(条件): 条件抽出");

                var dspDivs = divAccess.GetDivisionDspData();
                Assert(dspDivs.Count == allDivs.Count, "DivisionDataAccess.GetDivisionDspData: 表示用データ取得");

                Assert(divAccess.DeleteDivisionData("99"), "DivisionDataAccess.DeleteDivisionData: 削除処理");
                Assert(!divAccess.CheckDivisionCDExistence("99"), "DivisionDataAccess: 削除後非存在確認");

                // 6. MakerDataAccess
                Console.WriteLine("\n--- 6. MakerDataAccess (後藤) ---");
                var makerAccess = new MakerDataAccess();
                Assert(makerAccess.CheckMakerCDExistence("M01"), "MakerDataAccess.CheckMakerCDExistence: 存在確認");
                Assert(makerAccess.SelectMakerExistenceCheck("ソニー"), "MakerDataAccess.SelectMakerExistenceCheck: 部分一致確認");

                Assert(makerAccess.AddMakerData(new M_Maker { MakerCD = "M99", MakerName = "テストメーカ" }), "MakerDataAccess.AddMakerData: 登録処理");
                Assert(makerAccess.UpdateMakerData(new M_Maker { MakerCD = "M99", MakerName = "更新テストメーカ" }), "MakerDataAccess.UpdateMakerData: 更新処理");
                Assert(makerAccess.CheckCascadeItem("M01"), "MakerDataAccess.CheckCascadeItem: 商品利用あり確認");
                Assert(!makerAccess.CheckCascadeItem("M99"), "MakerDataAccess.CheckCascadeItem: 商品利用なし確認");

                var makers = makerAccess.GetMakerData();
                Assert(makers.Count >= 4, "MakerDataAccess.GetMakerData: 全件取得");

                var filterMakers = makerAccess.GetMakerData(new M_Maker { MakerName = "更新テスト" });
                Assert(filterMakers.Count == 1, "MakerDataAccess.GetMakerData(条件): 条件抽出");

                var dspMakers = makerAccess.GetMakerDspData();
                Assert(dspMakers.Count == makers.Count, "MakerDataAccess.GetMakerDspData: 表示用取得");

                Assert(makerAccess.DeleteMakerData("M99"), "MakerDataAccess.DeleteMakerData: 削除処理");

                // 7. CategoryDataAccess
                Console.WriteLine("\n--- 7. CategoryDataAccess (田中 / 山田) ---");
                var catAccess = new CategoryDataAccess();
                Assert(catAccess.CheckCategoryCDExistence("C01"), "CategoryDataAccess.CheckCategoryCDExistence: 存在確認");
                Assert(catAccess.SelectCategoryExistenceCheck("家電"), "CategoryDataAccess.SelectCategoryExistenceCheck: 部分一致確認");

                Assert(catAccess.AddCategoryData(new M_Category { CategoryCD = "C99", CategoryName = "季節家電", ParentCategoryCD = "C01" }), "CategoryDataAccess.AddCategoryData: 登録処理");
                Assert(catAccess.UpdateCategoryData(new M_Category { CategoryCD = "C99", CategoryName = "最新季節家電", ParentCategoryCD = "C01" }), "CategoryDataAccess.UpdateCategoryData: 更新処理");

                Assert(catAccess.CheckCascadeItem("C0101"), "CategoryDataAccess.CheckCascadeItem: 商品利用中確認");
                Assert(!catAccess.CheckCascadeItem("C99"), "CategoryDataAccess.CheckCascadeItem: 商品未利用確認");

                Assert(catAccess.CheckCascadeParentCategory("C01"), "CategoryDataAccess.CheckCascadeParentCategory: 親カテゴリ利用確認");
                Assert(!catAccess.CheckCascadeParentCategory("C0101"), "CategoryDataAccess.CheckCascadeParentCategory: 子カテゴリ確認");

                var allCats = catAccess.GetCategoryData();
                Assert(allCats.Count >= 6, "CategoryDataAccess.GetCategoryData: 全件取得");

                var filterCats = catAccess.GetCategoryData(new M_Categoryr { CategoryName = "最新季節" });
                Assert(filterCats.Count == 1, "CategoryDataAccess.GetCategoryData(条件): 条件抽出");

                var parentCats = catAccess.GetParentCategoryDspData();
                Assert(parentCats.Count >= 2, "CategoryDataAccess.GetParentCategoryDspData: 親カテゴリ表示用取得");

                var childCats = catAccess.GetCategoryDspData("C01");
                Assert(childCats.Count >= 3, "CategoryDataAccess.GetCategoryDspData: 子カテゴリ表示用取得");

                string catName = catAccess.GetComboboxText("C01");
                Assert(catName == "家電", "CategoryDataAccess.GetComboboxText: カテゴリ名取得");

                Assert(catAccess.DeleteCategoryData("C99"), "CategoryDataAccess.DeleteCategoryData: 削除処理");

                // 8. ItemDataAccess
                Console.WriteLine("\n--- 8. ItemDataAccess (渡辺) ---");
                var itemAccess = new ItemDataAccess();
                Assert(itemAccess.CheckItemCDExistence("IT001"), "ItemDataAccess.CheckItemCDExistence: 存在確認");

                var newItem = new M_Item
                {
                    ItemCD = "IT999",
                    ItemName = "超高精細4Kカメラ",
                    MakerCD = "M01",
                    CategoryCD = "C01",
                    Price = 98000
                };
                Assert(itemAccess.AddItemData(newItem), "ItemDataAccess.AddItemData: 登録処理");

                newItem.Price = 88000;
                Assert(itemAccess.UpdateItemData(newItem), "ItemDataAccess.UpdateItemData: 更新処理");

                var allItems = itemAccess.GetItemData();
                Assert(allItems.Count >= 4, "ItemDataAccess.GetItemData: 全件表示用取得");

                var filterItems = itemAccess.GetItemData(new M_ItemDsp { ItemCD = "IT999" });
                Assert(filterItems.Count == 1 && filterItems[0].Price == 88000, "ItemDataAccess.GetItemData(条件): 条件抽出");

                Assert(itemAccess.DeleteItemData("IT999"), "ItemDataAccess.DeleteItemData: 削除処理");
                Assert(!itemAccess.CheckItemCDExistence("IT999"), "ItemDataAccess: 削除後非存在確認");

                Console.WriteLine("==================================================");
                Console.WriteLine($" RESULT: ALL {passedTests}/{totalTests} MODULE TESTS PASSED!");
                Console.WriteLine("==================================================");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] モジュールテスト中に例外が発生しました: {ex.Message}");
                return 1;
            }
        }
    }
}
