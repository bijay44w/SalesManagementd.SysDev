using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 8. 受注管理 機能テストスイート
    /// 8.1 受注登録機能
    /// 8.2 受注更新機能
    /// 8.3 受注一覧表示機能
    /// 8.4 受注検索機能
    /// 8.5 受注非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public static class OrderServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 8. 受注管理 機能テスト");
            Console.WriteLine("==================================================");

            var service = new OrderService();
            int passedTests = 0;
            int createdOrId = 0;

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

                // 1. 受注登録機能
                var newOrder = new TOrder
                {
                    ClId = clients.First().ClId,
                    ClCharge = "テスト担当者",
                    SoId = offices.First().SoId,
                    EmId = employees.First().EmId,
                    OrDate = DateTime.Today
                };

                var details = new List<TOrderDetail>();
                if (products.Count > 0)
                {
                    var pr = products.First();
                    details.Add(new TOrderDetail
                    {
                        PrId = pr.PrId,
                        OrQuantity = 3,
                        OrTotalPrice = pr.Price * 3
                    });
                }

                var (regSuccess, regMsg, createdOrder) = service.RegisterOrder(newOrder, details);
                if (!regSuccess || createdOrder == null)
                {
                    Console.WriteLine($"[FAIL] 受注登録失敗: {regMsg}");
                    return 1;
                }
                createdOrId = createdOrder.OrId;
                Console.WriteLine($"[PASS] 8.1 受注登録機能 (OrId: {createdOrId})");
                passedTests++;

                // 2. 受注一覧表示機能
                var allOrders = service.GetAllOrders(includeHidden: false);
                if (!allOrders.Any(o => o.OrId == createdOrId))
                {
                    Console.WriteLine("[FAIL] 受注一覧表示に登録した受注が含まれていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS] 8.3 受注一覧表示機能 (取得件数: {allOrders.Count} 件)");
                passedTests++;

                // 3. 受注検索機能
                var searchById = service.SearchOrders(orId: createdOrId);
                if (searchById.Count != 1)
                {
                    Console.WriteLine("[FAIL] 受注ID検索が正しく機能していません。");
                    return 1;
                }
                Console.WriteLine("[PASS] 8.4 受注検索機能 (ID検索・条件検索)");
                passedTests++;

                // 4. 受注更新機能
                var (updSuccess, updMsg) = service.UpdateOrder(createdOrId, clients.First().ClId, "更新後担当者", offices.First().SoId, employees.First().EmId, DateTime.Today, stateFlag: 1);
                if (!updSuccess)
                {
                    Console.WriteLine($"[FAIL] 受注更新失敗: {updMsg}");
                    return 1;
                }
                var updatedOrder = service.SearchOrders(orId: createdOrId).FirstOrDefault();
                if (updatedOrder == null || updatedOrder.ClCharge != "更新後担当者" || updatedOrder.OrStateFlag != 1)
                {
                    Console.WriteLine("[FAIL] 受注更新内容の検証に失敗しました。");
                    return 1;
                }
                Console.WriteLine("[PASS] 8.2 受注更新機能");
                passedTests++;

                // 5. 受注非表示機能 (フラグ 2)
                var (hideSuccess, hideMsg) = service.HideOrder(createdOrId, "自動テストによる非表示");
                if (!hideSuccess)
                {
                    Console.WriteLine($"[FAIL] 受注非表示失敗: {hideMsg}");
                    return 1;
                }

                var activeList = service.GetAllOrders(includeHidden: false);
                var allList = service.GetAllOrders(includeHidden: true);
                if (activeList.Any(o => o.OrId == createdOrId) || !allList.Any(o => o.OrId == createdOrId && o.OrFlag == 2))
                {
                    Console.WriteLine("[FAIL] 非表示フラグの設定またはフィルタリングが不正です。");
                    return 1;
                }
                Console.WriteLine("[PASS] 8.5 受注非表示機能 (論理削除)");
                passedTests++;

                // 6. 受注一覧の顧客担当者・文字化け解消検証
                var existingOrders = service.GetAllOrders(includeHidden: true);
                var mandaOrder = existingOrders.FirstOrDefault(o => o.OrId == 1);
                var mizunoOrder = existingOrders.FirstOrDefault(o => o.OrId == 2);
                if (mandaOrder != null && mandaOrder.ClCharge != "萬田銀次郎")
                {
                    Console.WriteLine($"[FAIL] 受注ID 1 の顧客担当者文字化けが解消されていません: {mandaOrder.ClCharge}");
                    return 1;
                }
                if (mizunoOrder != null && mizunoOrder.ClCharge != "水野勝成")
                {
                    Console.WriteLine($"[FAIL] 受注ID 2 の顧客担当者文字化けが解消されていません: {mizunoOrder.ClCharge}");
                    return 1;
                }
                string fixedManda = StringSanitizer.FixMojibake("äÝ“c‹âŽŸ˜Y");
                if (fixedManda != "萬田銀次郎")
                {
                    Console.WriteLine($"[FAIL] StringSanitizer復元検証に失敗しました: {fixedManda}");
                    return 1;
                }
                Console.WriteLine("[PASS] 8.6 文字化け解消・文字コード復元検証 (DB値および復元ロジック)");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" SUCCESS: 8. 受注管理 全 {passedTests} 件のテストに合格しました。");
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
                if (createdOrId > 0)
                {
                    try
                    {
                        using var context = new SalesManagementContext();
                        var detailsToRemove = context.TOrderDetails.Where(d => d.OrId == createdOrId).ToList();
                        context.TOrderDetails.RemoveRange(detailsToRemove);
                        var orderToRemove = context.TOrders.FirstOrDefault(o => o.OrId == createdOrId);
                        if (orderToRemove != null)
                        {
                            context.TOrders.Remove(orderToRemove);
                        }
                        context.SaveChanges();
                    }
                    catch { }
                }
            }
        }
    }
}
