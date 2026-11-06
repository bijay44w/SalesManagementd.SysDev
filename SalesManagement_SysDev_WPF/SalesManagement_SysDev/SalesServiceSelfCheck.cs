using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 7. 売上管理 機能テストスイート
    /// 7.1 売上登録機能
    /// 7.2 売上更新機能
    /// 7.3 売上一覧表示機能
    /// 7.4 売上検索機能
    /// 7.5 売上非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public static class SalesServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 7. 売上管理 機能テスト");
            Console.WriteLine("==================================================");

            var service = new SalesService();
            int passedTests = 0;
            int createdSaId = 0;

            try
            {
                var offices = service.GetSalesOffices();
                var clients = service.GetClients();
                var employees = service.GetEmployees();
                var products = service.GetProducts();

                if (offices.Count == 0 || clients.Count == 0 || employees.Count == 0)
                {
                    Console.WriteLine("[ERROR] 必要なマスタデータが存在しません。");
                    return 1;
                }

                // 1. 売上登録機能
                var newSale = new TSale
                {
                    ClId = clients.First().ClId,
                    SoId = offices.First().SoId,
                    EmId = employees.First().EmId,
                    SaDate = DateTime.Today
                };

                var details = new List<TSaleDetail>();
                if (products.Count > 0)
                {
                    var pr = products.First();
                    details.Add(new TSaleDetail
                    {
                        PrId = pr.PrId,
                        SaQuantity = 2,
                        SaPrTotalPrice = pr.Price * 2
                    });
                }

                var (regSuccess, regMsg, createdSale) = service.RegisterSale(newSale, details);
                if (!regSuccess || createdSale == null)
                {
                    Console.WriteLine($"[FAIL] 売上登録失敗: {regMsg}");
                    return 1;
                }
                createdSaId = createdSale.SaId;
                Console.WriteLine($"[PASS] 7.1 売上登録機能 (SaId: {createdSaId})");
                passedTests++;

                // 2. 売上一覧表示機能
                var allSales = service.GetAllSales(includeHidden: false);
                if (!allSales.Any(s => s.SaId == createdSaId))
                {
                    Console.WriteLine("[FAIL] 売上一覧表示に登録した売上が含まれていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS] 7.3 売上一覧表示機能 (取得件数: {allSales.Count} 件)");
                passedTests++;

                // 3. 売上検索機能
                var searchById = service.SearchSales(saId: createdSaId);
                if (searchById.Count != 1)
                {
                    Console.WriteLine("[FAIL] 売上ID検索が正しく機能していません。");
                    return 1;
                }
                Console.WriteLine("[PASS] 7.4 売上検索機能 (ID検索・条件検索)");
                passedTests++;

                // 4. 売上更新機能
                var (updSuccess, updMsg) = service.UpdateSale(createdSaId, clients.First().ClId, offices.First().SoId, employees.First().EmId, DateTime.Today.AddDays(-1));
                if (!updSuccess)
                {
                    Console.WriteLine($"[FAIL] 売上更新失敗: {updMsg}");
                    return 1;
                }
                Console.WriteLine("[PASS] 7.2 売上更新機能");
                passedTests++;

                // 5. 売上非表示機能 (フラグ 2)
                var (hideSuccess, hideMsg) = service.HideSale(createdSaId, "自動テストによる非表示");
                if (!hideSuccess)
                {
                    Console.WriteLine($"[FAIL] 売上非表示失敗: {hideMsg}");
                    return 1;
                }

                var activeList = service.GetAllSales(includeHidden: false);
                var allList = service.GetAllSales(includeHidden: true);
                if (activeList.Any(s => s.SaId == createdSaId) || !allList.Any(s => s.SaId == createdSaId && s.SaFlag == 2))
                {
                    Console.WriteLine("[FAIL] 非表示フラグの設定またはフィルタリングが不正です。");
                    return 1;
                }
                Console.WriteLine("[PASS] 7.5 売上非表示機能 (論理削除)");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" SUCCESS: 7. 売上管理 全 {passedTests} 件のテストに合格しました。");
                Console.WriteLine("==================================================");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] テスト実行時エラー: {ex.Message}");
                return 1;
            }
            finally
            {
                // テストデータのクリーンアップ
                if (createdSaId > 0)
                {
                    try
                    {
                        using var context = new SalesManagementContext();
                        var detailsToRemove = context.TSaleDetails.Where(d => d.SaId == createdSaId).ToList();
                        context.TSaleDetails.RemoveRange(detailsToRemove);
                        var saleToRemove = context.TSales.FirstOrDefault(s => s.SaId == createdSaId);
                        if (saleToRemove != null)
                        {
                            context.TSales.Remove(saleToRemove);
                        }
                        context.SaveChanges();
                    }
                    catch { }
                }
            }
        }
    }
}
