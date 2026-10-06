using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    public class ClientService
    {
        /// <summary>
        /// 3.1 顧客登録機能: 入力された顧客情報を顧客テーブルに登録する。
        /// </summary>
        public (bool Success, string Message, MClient? Client) RegisterClient(MClient client)
        {
            if (client == null)
            {
                return (false, "顧客情報が指定されていません。", null);
            }

            // Validation
            if (string.IsNullOrWhiteSpace(client.ClName))
            {
                return (false, "顧客名を入力してください。", null);
            }
            if (client.ClName.Length > 50)
            {
                return (false, "顧客名は50文字以内で入力してください。", null);
            }

            if (client.SoId <= 0)
            {
                return (false, "所属営業所を選択してください。", null);
            }

            if (string.IsNullOrWhiteSpace(client.ClAddress))
            {
                return (false, "住所を入力してください。", null);
            }
            if (client.ClAddress.Length > 50)
            {
                return (false, "住所は50文字以内で入力してください。", null);
            }

            if (string.IsNullOrWhiteSpace(client.ClPhone))
            {
                return (false, "電話番号を入力してください。", null);
            }
            if (client.ClPhone.Length > 13)
            {
                return (false, "電話番号は13文字以内で入力してください。", null);
            }

            // Postal format sanitize (up to 7 chars)
            string postal = (client.ClPostal ?? "").Replace("-", "").Trim();
            if (postal.Length > 7) postal = postal.Substring(0, 7);
            client.ClPostal = postal;

            // FAX format
            string fax = (client.ClFax ?? "").Trim();
            if (fax.Length > 13) fax = fax.Substring(0, 13);
            client.ClFax = fax;

            // Default flag is 0 (有効)
            client.ClFlag = 0;

            try
            {
                using var context = new SalesManagementContext();

                // Check office exists
                bool officeExists = context.MSalesOffices.Any(x => x.SoId == client.SoId);
                if (!officeExists)
                {
                    return (false, "選択された営業所が存在しません。", null);
                }

                // Disconnect navigation to prevent EF tracking issues on insert
                client.So = null!;
                client.TArrivals = new List<TArrival>();
                client.TChumons = new List<TChumon>();
                client.TOrders = new List<TOrder>();
                client.TSales = new List<TSale>();
                client.TShipments = new List<TShipment>();
                client.TSyukkos = new List<TSyukko>();

                context.MClients.Add(client);
                context.SaveChanges();

                return (true, $"顧客「{client.ClName}」(ID: {client.ClId}) を正常に登録しました。", client);
            }
            catch (Exception ex)
            {
                return (false, $"顧客登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 3.2 顧客更新機能: 顧客テーブルにある顧客情報を更新する。
        /// </summary>
        public (bool Success, string Message) UpdateClient(MClient updatedClient)
        {
            if (updatedClient == null || updatedClient.ClId <= 0)
            {
                return (false, "更新対象の顧客IDが正しくありません。");
            }

            if (string.IsNullOrWhiteSpace(updatedClient.ClName))
            {
                return (false, "顧客名を入力してください。");
            }
            if (updatedClient.ClName.Length > 50)
            {
                return (false, "顧客名は50文字以内で入力してください。");
            }

            if (updatedClient.SoId <= 0)
            {
                return (false, "所属営業所を選択してください。");
            }

            if (string.IsNullOrWhiteSpace(updatedClient.ClAddress))
            {
                return (false, "住所を入力してください。");
            }
            if (updatedClient.ClAddress.Length > 50)
            {
                return (false, "住所は50文字以内で入力してください。");
            }

            if (string.IsNullOrWhiteSpace(updatedClient.ClPhone))
            {
                return (false, "電話番号を入力してください。");
            }
            if (updatedClient.ClPhone.Length > 13)
            {
                return (false, "電話番号は13文字以内で入力してください。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var client = context.MClients.FirstOrDefault(x => x.ClId == updatedClient.ClId);
                if (client == null)
                {
                    return (false, $"顧客ID: {updatedClient.ClId} のデータが見つかりません。");
                }

                string postal = (updatedClient.ClPostal ?? "").Replace("-", "").Trim();
                if (postal.Length > 7) postal = postal.Substring(0, 7);

                string fax = (updatedClient.ClFax ?? "").Trim();
                if (fax.Length > 13) fax = fax.Substring(0, 13);

                client.ClName = updatedClient.ClName.Trim();
                client.SoId = updatedClient.SoId;
                client.ClPostal = postal;
                client.ClAddress = updatedClient.ClAddress.Trim();
                client.ClPhone = updatedClient.ClPhone.Trim();
                client.ClFax = fax;
                client.ClFlag = updatedClient.ClFlag;
                client.ClHidden = updatedClient.ClHidden;

                context.SaveChanges();
                return (true, $"顧客ID: {client.ClId} の情報を正常に更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"顧客更新中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 3.3 顧客一覧表示機能: 登録されている顧客情報を一覧で表示する。
        /// </summary>
        public List<MClient> GetAllClients(bool includeHidden = true)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.MClients.Include(x => x.So).AsNoTracking();

                if (!includeHidden)
                {
                    query = query.Where(x => x.ClFlag != 2);
                }

                return query.OrderBy(x => x.ClId).ToList();
            }
            catch (Exception)
            {
                return new List<MClient>();
            }
        }

        /// <summary>
        /// 3.4 顧客検索機能: 入力された顧客情報を抽出する。
        /// </summary>
        public List<MClient> SearchClients(int? clId = null, string? clName = null, int? soId = null, string? phone = null, bool includeHidden = true)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.MClients.Include(x => x.So).AsNoTracking().AsQueryable();

                if (clId.HasValue && clId.Value > 0)
                {
                    query = query.Where(x => x.ClId == clId.Value);
                }

                if (!string.IsNullOrWhiteSpace(clName))
                {
                    string nameTrimmed = clName.Trim();
                    query = query.Where(x => x.ClName.Contains(nameTrimmed));
                }

                if (soId.HasValue && soId.Value > 0)
                {
                    query = query.Where(x => x.SoId == soId.Value);
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    string phoneTrimmed = phone.Trim();
                    query = query.Where(x => x.ClPhone.Contains(phoneTrimmed));
                }

                if (!includeHidden)
                {
                    query = query.Where(x => x.ClFlag != 2);
                }

                return query.OrderBy(x => x.ClId).ToList();
            }
            catch (Exception)
            {
                return new List<MClient>();
            }
        }

        /// <summary>
        /// 3.5 顧客非表示機能（フラグ）: 選択された項目の顧客管理フラグを0から2に更新する。
        /// </summary>
        public (bool Success, string Message) SetClientHiddenFlag(int clId, string? hiddenReason = null)
        {
            if (clId <= 0)
            {
                return (false, "対象の顧客IDが正しくありません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var client = context.MClients.FirstOrDefault(x => x.ClId == clId);
                if (client == null)
                {
                    return (false, $"顧客ID: {clId} のデータが見つかりません。");
                }

                // 顧客管理フラグを0から2に更新する
                client.ClFlag = 2;
                if (!string.IsNullOrWhiteSpace(hiddenReason))
                {
                    client.ClHidden = hiddenReason.Trim();
                }

                context.SaveChanges();
                return (true, $"顧客ID: {client.ClId} ({client.ClName}) の管理フラグを 2 (非表示) に更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"非表示フラグ更新中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 営業所一覧を取得 (コンボボックス用)
        /// </summary>
        public List<MSalesOffice> GetSalesOffices()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MSalesOffices.AsNoTracking().OrderBy(x => x.SoId).ToList();
            }
            catch (Exception)
            {
                return new List<MSalesOffice>();
            }
        }

        /// <summary>
        /// 顧客IDで顧客1件を取得
        /// </summary>
        public MClient? GetClientById(int clId)
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MClients.Include(x => x.So).AsNoTracking().FirstOrDefault(x => x.ClId == clId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
