using System;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// Runnable self-check test suite for 3. 顧客管理 requirements.
    /// Tests all 5 functions (3.1 to 3.5) with assertions and clean-up.
    /// </summary>
    public static class ClientServiceSelfCheck
    {
        public static int RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" STARTING TEST SUITE: 3. 顧客管理 機能テスト");
            Console.WriteLine("==================================================");

            var service = new ClientService();
            int createdId = 0;
            int passedTests = 0;

            try
            {
                // Ensure there is at least one sales office
                var offices = service.GetSalesOffices();
                if (offices.Count == 0)
                {
                    Console.WriteLine("[ERROR] 営業所マスタ(M_SalesOffice)にデータが存在しません。テストを中断します。");
                    return 1;
                }
                int testSoId = offices.First().SoId;

                // ----------------------------------------------------
                // TEST 3.1 顧客登録機能: 入力された顧客情報を顧客テーブルに登録する。
                // ----------------------------------------------------
                string uniqueTestName = $"テスト顧客_{DateTime.Now:yyyyMMddHHmmss}";
                var newClient = new MClient
                {
                    ClName = uniqueTestName,
                    SoId = testSoId,
                    ClPostal = "1234567",
                    ClAddress = "大阪府大阪市北区梅田1-1-1",
                    ClPhone = "06-0000-1111",
                    ClFax = "06-0000-2222"
                };

                var regResult = service.RegisterClient(newClient);
                if (!regResult.Success || regResult.Client == null || regResult.Client.ClId <= 0)
                {
                    Console.WriteLine($"[FAIL 3.1] 顧客登録機能に失敗しました: {regResult.Message}");
                    return 1;
                }

                createdId = regResult.Client.ClId;
                if (regResult.Client.ClFlag != 0)
                {
                    Console.WriteLine($"[FAIL 3.1] 登録された顧客の初期フラグが0ではありません: {regResult.Client.ClFlag}");
                    return 1;
                }
                Console.WriteLine($"[PASS 3.1 顧客登録機能] 顧客登録成功 (ID: {createdId}, 名前: {regResult.Client.ClName}, 初期フラグ: {regResult.Client.ClFlag})");
                passedTests++;

                // ----------------------------------------------------
                // TEST 3.2 顧客更新機能: 顧客テーブルにある顧客情報を更新する。
                // ----------------------------------------------------
                string updatedName = uniqueTestName + "_更新後";
                string updatedAddress = "大阪府大阪市中央区心斎橋2-2-2";
                var updateClientObj = new MClient
                {
                    ClId = createdId,
                    ClName = updatedName,
                    SoId = testSoId,
                    ClPostal = "7654321",
                    ClAddress = updatedAddress,
                    ClPhone = "06-9999-8888",
                    ClFax = "06-9999-7777",
                    ClFlag = 0
                };

                var updateResult = service.UpdateClient(updateClientObj);
                if (!updateResult.Success)
                {
                    Console.WriteLine($"[FAIL 3.2] 顧客更新機能に失敗しました: {updateResult.Message}");
                    return 1;
                }

                var fetchedAfterUpdate = service.GetClientById(createdId);
                if (fetchedAfterUpdate == null || fetchedAfterUpdate.ClName != updatedName || fetchedAfterUpdate.ClAddress != updatedAddress)
                {
                    Console.WriteLine($"[FAIL 3.2] 顧客情報の更新検証に失敗しました。取得された値: {fetchedAfterUpdate?.ClName}");
                    return 1;
                }
                Console.WriteLine($"[PASS 3.2 顧客更新機能] 顧客情報更新成功 (更新後名前: {fetchedAfterUpdate.ClName})");
                passedTests++;

                // ----------------------------------------------------
                // TEST 3.3 顧客一覧表示機能: 登録されている顧客情報を一覧で表示する。
                // ----------------------------------------------------
                var allClients = service.GetAllClients(includeHidden: true);
                if (allClients == null || allClients.Count == 0)
                {
                    Console.WriteLine("[FAIL 3.3] 顧客一覧の取得結果が空です。");
                    return 1;
                }
                if (!allClients.Any(x => x.ClId == createdId))
                {
                    Console.WriteLine($"[FAIL 3.3] 顧客一覧に登録したテスト顧客(ID: {createdId})が含まれていません。");
                    return 1;
                }
                Console.WriteLine($"[PASS 3.3 顧客一覧表示機能] 顧客一覧取得成功 (総件数: {allClients.Count}件)");
                passedTests++;

                // ----------------------------------------------------
                // TEST 3.4 顧客検索機能: 入力された顧客情報を抽出する。
                // ----------------------------------------------------
                var searchResults = service.SearchClients(clName: updatedName);
                if (searchResults == null || searchResults.Count == 0 || !searchResults.Any(x => x.ClId == createdId))
                {
                    Console.WriteLine($"[FAIL 3.4] 顧客名「{updatedName}」の検索に失敗しました。");
                    return 1;
                }

                var searchById = service.SearchClients(clId: createdId);
                if (searchById.Count != 1 || searchById[0].ClId != createdId)
                {
                    Console.WriteLine($"[FAIL 3.4] 顧客ID「{createdId}」の検索に失敗しました。");
                    return 1;
                }
                Console.WriteLine($"[PASS 3.4 顧客検索機能] 顧客検索成功 (抽出件数: {searchResults.Count}件)");
                passedTests++;

                // ----------------------------------------------------
                // TEST 3.5 顧客非表示機能（フラグ）: 選択された項目の顧客管理フラグを0から2に更新する。
                // ----------------------------------------------------
                var hideResult = service.SetClientHiddenFlag(createdId, "テスト非表示処理");
                if (!hideResult.Success)
                {
                    Console.WriteLine($"[FAIL 3.5] 顧客非表示機能に失敗しました: {hideResult.Message}");
                    return 1;
                }

                var hiddenClient = service.GetClientById(createdId);
                if (hiddenClient == null || hiddenClient.ClFlag != 2)
                {
                    Console.WriteLine($"[FAIL 3.5] 顧客管理フラグが2に更新されていません。現在のフラグ: {hiddenClient?.ClFlag}");
                    return 1;
                }
                Console.WriteLine($"[PASS 3.5 顧客非表示機能] 顧客管理フラグを 0 -> 2 (非表示) に更新成功");
                passedTests++;

                Console.WriteLine("==================================================");
                Console.WriteLine($" RESULT: ALL {passedTests}/5 TESTS PASSED SUCCESSFULLY!");
                Console.WriteLine("==================================================");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] テスト実行中に例外が発生しました: {ex}");
                return 1;
            }
            finally
            {
                // Clean up test record from database
                if (createdId > 0)
                {
                    try
                    {
                        using var context = new SalesManagementContext();
                        var toRemove = context.MClients.FirstOrDefault(x => x.ClId == createdId);
                        if (toRemove != null)
                        {
                            context.MClients.Remove(toRemove);
                            context.SaveChanges();
                            Console.WriteLine($"[CLEANUP] テストデータ(ID: {createdId})を正常に削除しました。");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CLEANUP WARNING] テストデータのクリーンアップに失敗しました: {ex.Message}");
                    }
                }
            }
        }
    }
}
