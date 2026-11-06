using System;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 6. 社員管理 機能テストスイート
    /// 6.1 社員登録機能
    /// 6.2 社員更新機能
    /// 6.3 社員一覧表示機能
    /// 6.4 社員検索機能
    /// 6.5 社員非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public static class EmployeeServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 6. 社員管理 機能テスト");
            Console.WriteLine("==================================================");

            var service = new EmployeeService();
            int passedTests = 0;
            int createdEmId = 0;

            try
            {
                var offices = service.GetSalesOffices();
                var positions = service.GetPositions();
                if (offices.Count == 0 || positions.Count == 0)
                {
                    Console.WriteLine("[ERROR] 営業所または役職マスタが存在しません。");
                    return 1;
                }

                // 1. 社員登録機能
                string testName = $"テスト社員_{DateTime.Now:MMddHHmmss}";
                var newEmployee = new MEmployee
                {
                    EmName = testName,
                    SoId = offices.First().SoId,
                    PoId = positions.First().PoId,
                    EmHiredate = DateTime.Today,
                    EmPhone = "090-1111-2222",
                    EmPassword = "pass1234"
                };

                var (regSuccess, regMsg, createdEmp) = service.RegisterEmployee(newEmployee);
                if (!regSuccess || createdEmp == null)
                {
                    Console.WriteLine($"[FAIL] 社員登録失敗: {regMsg}");
                    return 1;
                }
                createdEmId = createdEmp.EmId;
                Console.WriteLine($"[PASS] 6.1 社員登録機能 (EmId: {createdEmId}, Name: {testName})");
                passedTests++;

                // 2. 社員一覧表示機能
                var allEmployees = service.GetAllEmployees(includeHidden: false);
                if (!allEmployees.Any(e => e.EmId == createdEmId))
                {
                    Console.WriteLine("[FAIL] 社員一覧表示に登録した社員が含まれていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS] 6.3 社員一覧表示機能 (取得件数: {allEmployees.Count} 件)");
                passedTests++;

                // 3. 社員検索機能
                var searchById = service.SearchEmployees(emId: createdEmId);
                var searchByName = service.SearchEmployees(name: testName);
                if (searchById.Count != 1 || searchByName.Count == 0)
                {
                    Console.WriteLine("[FAIL] 社員検索が正しく機能していません。");
                    return 1;
                }
                Console.WriteLine("[PASS] 6.4 社員検索機能 (ID検索・氏名部分一致検索)");
                passedTests++;

                // 4. 社員更新機能
                string updatedName = testName + "_更新後";
                createdEmp.EmName = updatedName;
                createdEmp.EmPhone = "090-9999-8888";
                var (updSuccess, updMsg) = service.UpdateEmployee(createdEmp);
                if (!updSuccess)
                {
                    Console.WriteLine($"[FAIL] 社員更新失敗: {updMsg}");
                    return 1;
                }

                var verifyUpdate = service.SearchEmployees(emId: createdEmId).FirstOrDefault();
                if (verifyUpdate == null || verifyUpdate.EmName != updatedName)
                {
                    Console.WriteLine("[FAIL] 更新内容の反映が確認できませんでした。");
                    return 1;
                }
                Console.WriteLine("[PASS] 6.2 社員更新機能");
                passedTests++;

                // 5. 社員非表示機能 (フラグ 2)
                var (hideSuccess, hideMsg) = service.HideEmployee(createdEmId, "自動テストによる非表示");
                if (!hideSuccess)
                {
                    Console.WriteLine($"[FAIL] 社員非表示失敗: {hideMsg}");
                    return 1;
                }

                var activeList = service.GetAllEmployees(includeHidden: false);
                var allList = service.GetAllEmployees(includeHidden: true);
                if (activeList.Any(e => e.EmId == createdEmId) || !allList.Any(e => e.EmId == createdEmId && e.EmFlag == 2))
                {
                    Console.WriteLine("[FAIL] 非表示フラグの設定またはフィルタリングが不正です。");
                    return 1;
                }
                Console.WriteLine("[PASS] 6.5 社員非表示機能 (論理削除)");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" SUCCESS: 6. 社員管理 全 {passedTests} 件のテストに合格しました。");
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
                if (createdEmId > 0)
                {
                    try
                    {
                        using var context = new SalesManagementContext();
                        var target = context.MEmployees.FirstOrDefault(e => e.EmId == createdEmId);
                        if (target != null)
                        {
                            context.MEmployees.Remove(target);
                            context.SaveChanges();
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
