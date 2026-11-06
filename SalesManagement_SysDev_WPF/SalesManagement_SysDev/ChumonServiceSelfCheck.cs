using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 9. 注文管理 機能テストスイート
    /// 9.1 注文登録機能
    /// 9.2 注文更新機能
    /// 9.3 注文一覧表示機能
    /// 9.4 注文検索機能
    /// 9.5 注文非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public static class ChumonServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 9. 注文管理 機能テスト");
            Console.WriteLine("==================================================");

            var service = new ChumonService();
            int passedTests = 0;
            int createdChId = 0;

            try
            {
                var offices = service.GetSalesOffices();
                var clients = service.GetClients();
                var employees = service.GetEmployees();
                var orders = service.GetOrders();
                var products = service.GetProducts();

                if (offices.Count == 0 || clients.Count == 0 || orders.Count == 0)
                {
                    Console.WriteLine("[ERROR] 必要なマスタデータまたは受注データが存在しません。");
                    return 1;
                }

                // 1. 注文登録機能
                var newChumon = new TChumon
                {
                    OrId = orders.First().OrId,
                    ClId = clients.First().ClId,
                    SoId = offices.First().SoId,
                    EmId = employees.FirstOrDefault()?.EmId,
                    ChDate = DateTime.Today
                };

                var details = new List<TChumonDetail>();
                if (products.Count > 0)
                {
                    var pr = products.First();
                    details.Add(new TChumonDetail
                    {
                        PrId = pr.PrId,
                        ChQuantity = 5
                    });
                }

                var (regSuccess, regMsg, createdChumon) = service.RegisterChumon(newChumon, details);
                if (!regSuccess || createdChumon == null)
                {
                    Console.WriteLine($"[FAIL] 注文登録失敗: {regMsg}");
                    return 1;
                }
                createdChId = createdChumon.ChId;
                Console.WriteLine($"[PASS] 9.1 注文登録機能 (ChId: {createdChId})");
                passedTests++;

                // 2. 注文一覧表示機能
                var allChumons = service.GetAllChumons(includeHidden: false);
                if (!allChumons.Any(c => c.ChId == createdChId))
                {
                    Console.WriteLine("[FAIL] 注文一覧表示に登録した注文が含まれていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS] 9.3 注文一覧表示機能 (取得件数: {allChumons.Count} 件)");
                passedTests++;

                // 3. 注文検索機能
                var searchById = service.SearchChumons(chId: createdChId);
                if (searchById.Count != 1)
                {
                    Console.WriteLine("[FAIL] 注文ID検索が正しく機能していません。");
                    return 1;
                }
                Console.WriteLine("[PASS] 9.4 注文検索機能 (ID検索・条件検索)");
                passedTests++;

                // 4. 注文更新機能
                var (updSuccess, updMsg) = service.UpdateChumon(createdChId, offices.First().SoId, employees.FirstOrDefault()?.EmId, clients.First().ClId, orders.First().OrId, DateTime.Today, stateFlag: 1);
                if (!updSuccess)
                {
                    Console.WriteLine($"[FAIL] 注文更新失敗: {updMsg}");
                    return 1;
                }
                var updated = service.SearchChumons(chId: createdChId).FirstOrDefault();
                if (updated == null || updated.ChStateFlag != 1)
                {
                    Console.WriteLine("[FAIL] 注文更新内容の検証に失敗しました。");
                    return 1;
                }
                Console.WriteLine("[PASS] 9.2 注文更新機能");
                passedTests++;

                // 5. 注文非表示機能 (フラグ 2)
                var (hideSuccess, hideMsg) = service.HideChumon(createdChId, "自動テストによる非表示");
                if (!hideSuccess)
                {
                    Console.WriteLine($"[FAIL] 注文非表示失敗: {hideMsg}");
                    return 1;
                }

                var activeList = service.GetAllChumons(includeHidden: false);
                var allList = service.GetAllChumons(includeHidden: true);
                if (activeList.Any(c => c.ChId == createdChId) || !allList.Any(c => c.ChId == createdChId && c.ChFlag == 2))
                {
                    Console.WriteLine("[FAIL] 非表示フラグの設定またはフィルタリングが不正です。");
                    return 1;
                }
                Console.WriteLine("[PASS] 9.5 注文非表示機能 (論理削除)");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" SUCCESS: 9. 注文管理 全 {passedTests} 件のテストに合格しました。");
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
                if (createdChId > 0)
                {
                    try
                    {
                        using var context = new SalesManagementContext();
                        var detailsToRemove = context.TChumonDetails.Where(d => d.ChId == createdChId).ToList();
                        context.TChumonDetails.RemoveRange(detailsToRemove);
                        var chumonToRemove = context.TChumons.FirstOrDefault(c => c.ChId == createdChId);
                        if (chumonToRemove != null)
                        {
                            context.TChumons.Remove(chumonToRemove);
                        }
                        context.SaveChanges();
                    }
                    catch { }
                }
            }
        }
    }
}
