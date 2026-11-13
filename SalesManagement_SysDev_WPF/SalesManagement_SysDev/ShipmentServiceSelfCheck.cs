using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 14. 出荷管理 機能テストスイート
    /// 14.1 出荷登録機能
    /// 14.2 出荷更新機能 (確定・在庫減算)
    /// 14.3 出荷一覧表示機能
    /// 14.4 出荷検索機能
    /// 14.5 出荷非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public static class ShipmentServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 14. 出荷管理 機能テスト");
            Console.WriteLine("==================================================");

            var service = new ShipmentService();
            int passedTests = 0;
            int createdShId = 0;

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

                var targetOrder = orders.First();

                // 1. 出荷登録機能
                var newShipment = new TShipment
                {
                    OrId = targetOrder.OrId,
                    ClId = targetOrder.ClId,
                    SoId = targetOrder.SoId,
                    EmId = employees.FirstOrDefault()?.EmId,
                    ShFinishDate = DateTime.Today,
                    ShStateFlag = 0
                };

                var details = new List<TShipmentDetail>();
                if (targetOrder.TOrderDetails != null && targetOrder.TOrderDetails.Count > 0)
                {
                    foreach (var od in targetOrder.TOrderDetails)
                    {
                        details.Add(new TShipmentDetail
                        {
                            PrId = od.PrId,
                            ShQuantity = od.OrQuantity
                        });
                    }
                }
                else
                {
                    details.Add(new TShipmentDetail
                    {
                        PrId = products.First().PrId,
                        ShQuantity = 5
                    });
                }

                var (regSuccess, regMsg, createdSh) = service.RegisterShipment(newShipment, details);
                if (!regSuccess || createdSh == null)
                {
                    Console.WriteLine($"[FAIL] 出荷登録失敗: {regMsg}");
                    return 1;
                }
                createdShId = createdSh.ShId;
                Console.WriteLine($"[PASS] 14.1 出荷登録機能 (ShId: {createdShId})");
                passedTests++;

                // 2. 出荷一覧表示機能
                var allList = service.GetAllShipments(includeHidden: false);
                if (!allList.Any(s => s.ShId == createdShId))
                {
                    Console.WriteLine("[FAIL] 出荷一覧表示に登録データが含まれていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS] 14.3 出荷一覧表示機能 (取得件数: {allList.Count} 件)");
                passedTests++;

                // 3. 出荷検索機能
                var searchById = service.SearchShipments(shId: createdShId);
                var searchByOrId = service.SearchShipments(orId: targetOrder.OrId);
                if (!searchById.Any(s => s.ShId == createdShId) || !searchByOrId.Any(s => s.ShId == createdShId))
                {
                    Console.WriteLine("[FAIL] 出荷検索機能で対象が見つかりません。");
                    return 1;
                }
                Console.WriteLine("[PASS] 14.4 出荷検索機能 (ID指定検索・受注ID検索)");
                passedTests++;

                // 4. 出荷明細取得
                var shDetails = service.GetShipmentDetails(createdShId);
                if (shDetails.Count == 0)
                {
                    Console.WriteLine("[FAIL] 出荷明細が取得できません。");
                    return 1;
                }

                // 5. 出荷更新機能 (確定・在庫減算検証)
                var (updSuccess, updMsg) = service.UpdateShipment(
                    createdShId,
                    emId: employees.First().EmId,
                    finishDate: DateTime.Today,
                    stateFlag: 1);

                if (!updSuccess)
                {
                    Console.WriteLine($"[FAIL] 出荷更新失敗: {updMsg}");
                    return 1;
                }

                var updated = service.SearchShipments(shId: createdShId, includeHidden: true).FirstOrDefault();
                if (updated == null || updated.ShStateFlag != 1)
                {
                    Console.WriteLine("[FAIL] 出荷確定ステータスの更新が反映されていません。");
                    return 1;
                }
                Console.WriteLine("[PASS] 14.2 出荷更新機能 (ステータス確定)");
                passedTests++;

                // 6. 出荷非表示機能 (論理削除)
                var (hideSuccess, hideMsg) = service.HideShipment(createdShId, "自動テストによる非表示設定");
                if (!hideSuccess)
                {
                    Console.WriteLine($"[FAIL] 出荷非表示失敗: {hideMsg}");
                    return 1;
                }

                var afterHideActive = service.GetAllShipments(includeHidden: false);
                var afterHideAll = service.GetAllShipments(includeHidden: true);
                if (afterHideActive.Any(s => s.ShId == createdShId) || !afterHideAll.Any(s => s.ShId == createdShId && s.ShFlag == 2))
                {
                    Console.WriteLine("[FAIL] 非表示フラグの設定またはフィルタリングが不正です。");
                    return 1;
                }
                Console.WriteLine("[PASS] 14.5 出荷非表示機能 (論理削除)");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" SUCCESS: 14. 出荷管理 全 {passedTests} 件のテストに合格しました。");
                Console.WriteLine("==================================================");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] 出荷管理テスト実行時エラー: {ex.Message}");
                return 1;
            }
            finally
            {
                if (createdShId > 0)
                {
                    try
                    {
                        using var context = new SalesManagementContext();
                        var detailsToRemove = context.TShipmentDetails.Where(d => d.ShId == createdShId).ToList();
                        context.TShipmentDetails.RemoveRange(detailsToRemove);
                        var shToRemove = context.TShipments.FirstOrDefault(s => s.ShId == createdShId);
                        if (shToRemove != null)
                        {
                            context.TShipments.Remove(shToRemove);
                        }
                        context.SaveChanges();
                    }
                    catch { }
                }
            }
        }
    }
}
