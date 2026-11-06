using System;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 1. 商品管理 機能テストスイート
    /// 1.1 商品登録機能 (在庫数0レコード生成確認を含む)
    /// 4.2 商品更新機能
    /// 4.3 商品一覧表示機能
    /// 4.4 商品検索機能
    /// 4.5 商品非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public static class ProductServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 1. 商品管理 機能テスト");
            Console.WriteLine("==================================================");

            var service = new ProductService();
            int createdPrId = 0;
            int passedTests = 0;

            try
            {
                // マスタデータ存在確認
                var makers = service.GetMakers();
                if (makers.Count == 0)
                {
                    Console.WriteLine("[ERROR] メーカーマスタ(M_Maker)にデータが存在しません。テストを中断します。");
                    return 1;
                }
                int testMaId = makers.First().MaId;

                var smallClasses = service.GetSmallClassifications();
                if (smallClasses.Count == 0)
                {
                    Console.WriteLine("[ERROR] 小分類マスタ(M_SmallClassification)にデータが存在しません。テストを中断します。");
                    return 1;
                }
                int testScId = smallClasses.First().ScId;

                // ----------------------------------------------------
                // TEST 1.1 商品登録機能
                // • 入力された商品情報を商品テーブルに登録する。
                // • 商品情報をもとに在庫テーブルに在庫数0のレコードを作成する。
                // ----------------------------------------------------
                string uniqueItemName = $"テスト商品_{DateTime.Now:yyyyMMddHHmmss}";
                var newProduct = new MProduct
                {
                    PrName = uniqueItemName,
                    MaId = testMaId,
                    ScId = testScId,
                    Price = 12800,
                    PrSafetyStock = 10,
                    PrModelNumber = "TEST-MDL-01",
                    PrColor = "シルバー",
                    PrReleaseDate = DateTime.Today,
                    PrJcode = "4901234567890"
                };

                var regResult = service.RegisterProduct(newProduct);
                if (!regResult.Success || regResult.Product == null || regResult.Product.PrId <= 0)
                {
                    Console.WriteLine($"[FAIL 1.1] 商品登録機能に失敗しました: {regResult.Message}");
                    return 1;
                }

                createdPrId = regResult.Product.PrId;
                if (regResult.Product.PrFlag != 0)
                {
                    Console.WriteLine($"[FAIL 1.1] 登録された商品の初期フラグが0ではありません: {regResult.Product.PrFlag}");
                    return 1;
                }

                // 在庫テーブルに在庫数0のレコードが作成されているか検証
                using (var context = new SalesManagementContext())
                {
                    var stockRecord = context.TStocks.FirstOrDefault(s => s.PrId == createdPrId);
                    if (stockRecord == null)
                    {
                        Console.WriteLine($"[FAIL 1.1] 在庫テーブル(T_Stock)に商品ID {createdPrId} のレコードが作成されていません。");
                        return 1;
                    }
                    if (stockRecord.StQuantity != 0)
                    {
                        Console.WriteLine($"[FAIL 1.1] 在庫レコードの初期在庫数が0ではありません: {stockRecord.StQuantity}");
                        return 1;
                    }
                    Console.WriteLine($"[PASS 1.1 商品登録機能] 商品登録成功 (ID: {createdPrId}, 名前: {regResult.Product.PrName}, 初期フラグ: {regResult.Product.PrFlag})");
                    Console.WriteLine($"[PASS 1.1 在庫作成検証] 在庫テーブルに在庫数 {stockRecord.StQuantity} のレコード(StID: {stockRecord.StId})が正常作成されました。");
                }
                passedTests++;

                // ----------------------------------------------------
                // TEST 4.2 商品更新機能
                // • 商品テーブルにある商品情報を更新する。
                // ----------------------------------------------------
                string updatedName = uniqueItemName + "_更新後";
                decimal updatedPrice = 15800;
                string updatedColor = "ブラック";
                var updateProductObj = new MProduct
                {
                    PrId = createdPrId,
                    PrName = updatedName,
                    MaId = testMaId,
                    ScId = testScId,
                    Price = updatedPrice,
                    PrSafetyStock = 15,
                    PrModelNumber = "TEST-MDL-01B",
                    PrColor = updatedColor,
                    PrReleaseDate = DateTime.Today,
                    PrFlag = 0
                };

                var updateResult = service.UpdateProduct(updateProductObj);
                if (!updateResult.Success)
                {
                    Console.WriteLine($"[FAIL 4.2] 商品更新機能に失敗しました: {updateResult.Message}");
                    return 1;
                }

                var fetchedAfterUpdate = service.GetProductById(createdPrId);
                if (fetchedAfterUpdate == null || fetchedAfterUpdate.PrName != updatedName || fetchedAfterUpdate.Price != updatedPrice || fetchedAfterUpdate.PrColor != updatedColor)
                {
                    Console.WriteLine($"[FAIL 4.2] 商品情報の更新検証に失敗しました。取得された値: Name={fetchedAfterUpdate?.PrName}, Price={fetchedAfterUpdate?.Price}");
                    return 1;
                }
                Console.WriteLine($"[PASS 4.2 商品更新機能] 商品情報更新成功 (更新後名前: {fetchedAfterUpdate.PrName}, 価格: {fetchedAfterUpdate.Price:N0}円, 色: {fetchedAfterUpdate.PrColor})");
                passedTests++;

                // ----------------------------------------------------
                // TEST 4.3 商品一覧表示機能
                // • 登録されている商品情報を一覧で表示する。
                // ----------------------------------------------------
                var allProducts = service.GetAllProducts(includeHidden: false);
                var foundInList = allProducts.FirstOrDefault(p => p.PrId == createdPrId);
                if (foundInList == null)
                {
                    Console.WriteLine($"[FAIL 4.3] 商品一覧表示機能に登録した商品(ID: {createdPrId})が含まれていません。");
                    return 1;
                }
                if (string.IsNullOrEmpty(foundInList.MaName) || string.IsNullOrEmpty(foundInList.ScName))
                {
                    Console.WriteLine($"[FAIL 4.3] 一覧表示用のメーカー名または小分類名が取得できていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS 4.3 商品一覧表示機能] 商品一覧取得成功 (表示件数: {allProducts.Count}件, 対象商品: {foundInList.PrName}, メーカー: {foundInList.MaName}, 在庫数: {foundInList.StockQuantity})");
                passedTests++;

                // ----------------------------------------------------
                // TEST 4.4 商品検索機能
                // • 入力された商品情報を抽出する。
                // ----------------------------------------------------
                var searchResults = service.SearchProducts(
                    prId: createdPrId,
                    prName: uniqueItemName,
                    includeHidden: false);

                if (searchResults.Count == 0 || searchResults.All(p => p.PrId != createdPrId))
                {
                    Console.WriteLine($"[FAIL 4.4] 商品検索機能で登録した商品(ID: {createdPrId})が抽出されませんでした。");
                    return 1;
                }
                Console.WriteLine($"[PASS 4.4 商品検索機能] 商品検索成功 (抽出件数: {searchResults.Count}件, 該当商品ID: {searchResults.First().PrId}, 名前: {searchResults.First().PrName})");
                passedTests++;

                // ----------------------------------------------------
                // TEST 4.5 商品非表示機能(フラグ)
                // • 選択された項目の商品管理フラグを０から２に更新する。
                // ----------------------------------------------------
                string hideReason = "自己診断テストによる非表示設定";
                var hideResult = service.SetProductHiddenFlag(createdPrId, hideReason);
                if (!hideResult.Success)
                {
                    Console.WriteLine($"[FAIL 4.5] 商品非表示機能に失敗しました: {hideResult.Message}");
                    return 1;
                }

                var fetchedAfterHide = service.GetProductById(createdPrId);
                if (fetchedAfterHide == null || fetchedAfterHide.PrFlag != 2)
                {
                    Console.WriteLine($"[FAIL 4.5] 商品管理フラグが 2 に更新されていません: {fetchedAfterHide?.PrFlag}");
                    return 1;
                }

                // 通常一覧から除外されていることを確認
                var listAfterHide = service.GetAllProducts(includeHidden: false);
                if (listAfterHide.Any(p => p.PrId == createdPrId))
                {
                    Console.WriteLine($"[FAIL 4.5] 非表示(フラグ=2)にした商品が通常一覧に表示されています。");
                    return 1;
                }

                // includeHidden=true では取得できることを確認
                var listWithHidden = service.GetAllProducts(includeHidden: true);
                if (!listWithHidden.Any(p => p.PrId == createdPrId))
                {
                    Console.WriteLine($"[FAIL 4.5] 非表示(フラグ=2)を含める全件一覧に商品が存在しません。");
                    return 1;
                }

                Console.WriteLine($"[PASS 4.5 商品非表示機能] 商品管理フラグを 0 -> 2 (非表示) に正常更新。通常一覧・検索からの除外を確認。");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" RESULT: ALL {passedTests}/5 TESTS PASSED SUCCESSFULLY!");
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
                // クリーンアップ: 作成したテスト商品を在庫ごと削除
                if (createdPrId > 0)
                {
                    bool deleted = service.HardDeleteProductForTest(createdPrId);
                    if (deleted)
                    {
                        Console.WriteLine($"[CLEANUP] テストデータ(ID: {createdPrId})を正常に削除しました。");
                    }
                }
            }
        }
    }
}
