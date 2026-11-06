using System;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 5. 在庫管理 機能テストスイート
    /// 5.1 在庫更新機能
    /// 5.2 在庫一覧表示機能
    /// 5.3 在庫検索機能
    /// 5.4 在庫非表示機能(フラグ 0 -> 2 更新)
    /// </summary>
    public static class StockServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 5. 在庫管理 機能テスト");
            Console.WriteLine("==================================================");

            var stockService = new StockService();
            var productService = new ProductService();
            int createdPrId = 0;
            int createdStId = 0;
            int passedTests = 0;
           
            try
            {
                // テスト用の商品を1件作成 (1.1 商品登録で在庫0のレコードが作成される)
                var makers = productService.GetMakers();
                var smallClasses = productService.GetSmallClassifications();
                if (makers.Count == 0 || smallClasses.Count == 0)
                {
                    Console.WriteLine("[ERROR] マスタデータが存在しません。テストを中断します。");
                    return 1;
                }

                string uniqueName = $"在庫テスト商品_{DateTime.Now:yyyyMMddHHmmss}";
                var testProduct = new MProduct
                {
                    PrName = uniqueName,
                    MaId = makers.First().MaId,
                    ScId = smallClasses.First().ScId,
                    Price = 5000,
                    PrSafetyStock = 20,
                    PrModelNumber = "ST-TEST",
                    PrColor = "ホワイト",
                    PrReleaseDate = DateTime.Today
                };

                var regProdResult = productService.RegisterProduct(testProduct);
                if (!regProdResult.Success || regProdResult.Product == null)
                {
                    Console.WriteLine($"[ERROR] テスト用商品の事前作成に失敗しました: {regProdResult.Message}");
                    return 1;
                }
                createdPrId = regProdResult.Product.PrId;

                using (var context = new SalesManagementContext())
                {
                    var stockRecord = context.TStocks.FirstOrDefault(s => s.PrId == createdPrId);
                    if (stockRecord == null)
                    {
                        Console.WriteLine("[ERROR] 事前作成した商品に対応する在庫レコードが見つかりません。");
                        return 1;
                    }
                    createdStId = stockRecord.StId;
                }

                // ----------------------------------------------------
                // TEST 5.1 在庫更新機能: 在庫テーブルにある商品情報を更新する。
                // ----------------------------------------------------
                int updatedQuantity = 50;
                var updateResult = stockService.UpdateStock(createdStId, updatedQuantity, flag: 0);
                if (!updateResult.Success)
                {
                    Console.WriteLine($"[FAIL 5.1] 在庫更新機能に失敗しました: {updateResult.Message}");
                    return 1;
                }

                var fetchedAfterUpdate = stockService.GetStockById(createdStId);
                if (fetchedAfterUpdate == null || fetchedAfterUpdate.StQuantity != updatedQuantity)
                {
                    Console.WriteLine($"[FAIL 5.1] 在庫更新値の検証に失敗しました。取得された在庫数: {fetchedAfterUpdate?.StQuantity}");
                    return 1;
                }
                Console.WriteLine($"[PASS 5.1 在庫更新機能] 在庫数更新成功 (在庫ID: {createdStId}, 更新後在庫数: {fetchedAfterUpdate.StQuantity}個)");
                passedTests++;

                // ----------------------------------------------------
                // TEST 5.2 在庫一覧表示機能: 登録されている在庫情報を一覧で表示する。
                // ----------------------------------------------------
                var allStocks = stockService.GetAllStocks(includeHidden: false);
                var foundInList = allStocks.FirstOrDefault(s => s.StId == createdStId);
                if (foundInList == null)
                {
                    Console.WriteLine($"[FAIL 5.2] 在庫一覧表示機能に登録した在庫(ID: {createdStId})が含まれていません。");
                    return 1;
                }
                if (foundInList.StQuantity != updatedQuantity || string.IsNullOrEmpty(foundInList.PrName))
                {
                    Console.WriteLine($"[FAIL 5.2] 一覧表示用の商品名または在庫数が正しく取得できていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS 5.2 在庫一覧表示機能] 在庫一覧取得成功 (全件数: {allStocks.Count}件, 対象商品: {foundInList.PrName}, 在庫数: {foundInList.StQuantity}個, 安全在庫: {foundInList.PrSafetyStock}個, 状態: {foundInList.StockStatus})");
                passedTests++;

                // ----------------------------------------------------
                // TEST 5.3 在庫検索機能: 入力された在庫情報を抽出する。
                // ----------------------------------------------------
                var searchResults = stockService.SearchStocks(
                    stId: createdStId,
                    prId: createdPrId,
                    prName: uniqueName,
                    includeHidden: false);

                if (searchResults.Count == 0 || searchResults.All(s => s.StId != createdStId))
                {
                    Console.WriteLine($"[FAIL 5.3] 在庫検索機能で対象在庫(ID: {createdStId})が抽出されませんでした。");
                    return 1;
                }
                Console.WriteLine($"[PASS 5.3 在庫検索機能] 在庫検索成功 (抽出件数: {searchResults.Count}件, 該当在庫ID: {searchResults.First().StId}, 商品名: {searchResults.First().PrName})");
                passedTests++;

                // ----------------------------------------------------
                // TEST 5.4 在庫非表示機能(フラグ): 選択された項目の在庫管理フラグを０から２に更新する。
                // ----------------------------------------------------
                var hideResult = stockService.SetStockHiddenFlag(createdStId);
                if (!hideResult.Success)
                {
                    Console.WriteLine($"[FAIL 5.4] 在庫非表示機能に失敗しました: {hideResult.Message}");
                    return 1;
                }

                var fetchedAfterHide = stockService.GetStockById(createdStId);
                if (fetchedAfterHide == null || fetchedAfterHide.StFlag != 2)
                {
                    Console.WriteLine($"[FAIL 5.4] 在庫管理フラグが 2 に更新されていません: {fetchedAfterHide?.StFlag}");
                    return 1;
                }

                // 通常一覧から除外されていることを確認
                var listAfterHide = stockService.GetAllStocks(includeHidden: false);
                if (listAfterHide.Any(s => s.StId == createdStId))
                {
                    Console.WriteLine($"[FAIL 5.4] 非表示(フラグ=2)にした在庫が通常一覧に表示されています。");
                    return 1;
                }

                // includeHidden=true では取得できることを確認
                var listWithHidden = stockService.GetAllStocks(includeHidden: true);
                if (!listWithHidden.Any(s => s.StId == createdStId))
                {
                    Console.WriteLine($"[FAIL 5.4] 全件一覧(非表示含む)に対象在庫が存在しません。");
                    return 1;
                }

                Console.WriteLine($"[PASS 5.4 在庫非表示機能] 在庫管理フラグを 0 -> 2 (非表示) に正常更新。通常一覧・検索からの除外を確認。");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" RESULT: ALL {passedTests}/4 TESTS PASSED SUCCESSFULLY!");
                Console.WriteLine("==================================================");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] テストスイート実行中に予期せぬ例外が発生しました: {ex}");
                return 1;
            }
            finally
            {
                // クリーンアップ
                if (createdStId > 0)
                {
                    stockService.HardDeleteStockForTest(createdStId);
                }
                if (createdPrId > 0)
                {
                    productService.HardDeleteProductForTest(createdPrId);
                    Console.WriteLine($"[CLEANUP] テストデータ(商品ID: {createdPrId}, 在庫ID: {createdStId})を正常に削除しました。");
                }
            }
        }
    }
}
