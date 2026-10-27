using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 注文一覧・検索結果の画面表示用モデル
    /// </summary>
    public class ChumonDisplayModel
    {
        public int ChId { get; set; }
        public int SoId { get; set; }
        public string SoName { get; set; } = string.Empty;
        public int? EmId { get; set; }
        public string EmName { get; set; } = string.Empty;
        public int ClId { get; set; }
        public string ClName { get; set; } = string.Empty;
        public int OrId { get; set; }
        public DateTime? ChDate { get; set; }
        public string ChDateFormatted => ChDate.HasValue ? ChDate.Value.ToString("yyyy/MM/dd") : "-";
        public int DetailCount { get; set; }
        public int TotalQuantity { get; set; }
        public int? ChStateFlag { get; set; }
        public string StateFormatted => ChStateFlag switch
        {
            1 => "確定済",
            _ => "未確定"
        };
        public int ChFlag { get; set; }
        public string FlagFormatted => ChFlag switch
        {
            0 => "表示中",
            2 => "非表示",
            _ => $"その他({ChFlag})"
        };
        public string? ChHidden { get; set; }
    }

    /// <summary>
    /// 注文明細の画面表示用モデル
    /// </summary>
    public class ChumonDetailDisplayModel
    {
        public int ChDetailId { get; set; }
        public int ChId { get; set; }
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int ChQuantity { get; set; }
    }

    /// <summary>
    /// 9. 注文管理サービス
    /// 9.1 注文登録機能
    /// 9.2 注文更新機能
    /// 9.3 注文一覧表示機能
    /// 9.4 注文検索機能
    /// 9.5 注文非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public class ChumonService
    {
        // =====================================================================
        // 9.1 注文登録機能
        // =====================================================================
        public (bool Success, string Message, TChumon? Chumon) RegisterChumon(TChumon chumon, List<TChumonDetail>? details = null)
        {
            if (chumon == null)
            {
                return (false, "注文情報が指定されていません。", null);
            }

            if (chumon.ClId <= 0)
            {
                return (false, "顧客を選択してください。", null);
            }

            if (chumon.SoId <= 0)
            {
                return (false, "営業所を選択してください。", null);
            }

            if (chumon.ChDate == null || chumon.ChDate == default(DateTime))
            {
                chumon.ChDate = DateTime.Today;
            }

            try
            {
                using var context = new SalesManagementContext();

                if (chumon.OrId <= 0)
                {
                    var fallbackOrder = context.TOrders.FirstOrDefault(o => o.ClId == chumon.ClId)
                                        ?? context.TOrders.FirstOrDefault();
                    if (fallbackOrder != null)
                    {
                        chumon.OrId = fallbackOrder.OrId;
                    }
                    else
                    {
                        return (false, "関連付ける受注情報が見つかりません。", null);
                    }
                }

                chumon.ChFlag = 0;
                chumon.ChStateFlag = 0;
                chumon.ChHidden = null;

                context.TChumons.Add(chumon);
                context.SaveChanges();

                if (details != null && details.Count > 0)
                {
                    foreach (var d in details)
                    {
                        d.ChId = chumon.ChId;
                        context.TChumonDetails.Add(d);
                    }
                    context.SaveChanges();
                }

                return (true, $"注文ID: {chumon.ChId} を正常に登録しました。", chumon);
            }
            catch (Exception ex)
            {
                return (false, $"注文登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        // =====================================================================
        // 9.2 注文更新機能
        // =====================================================================
        public (bool Success, string Message) UpdateChumon(int chId, int soId, int? emId, int clId, int orId, DateTime? chDate, int? stateFlag = null)
        {
            if (chId <= 0)
            {
                return (false, "注文IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.TChumons.FirstOrDefault(c => c.ChId == chId);
                if (target == null)
                {
                    return (false, $"注文ID: {chId} が見つかりませんでした。");
                }

                if (soId > 0) target.SoId = soId;
                if (emId.HasValue && emId.Value > 0) target.EmId = emId;
                if (clId > 0) target.ClId = clId;
                if (orId > 0) target.OrId = orId;
                if (chDate.HasValue && chDate.Value != default) target.ChDate = chDate;
                if (stateFlag.HasValue) target.ChStateFlag = stateFlag.Value;

                context.SaveChanges();
                return (true, $"注文ID: {chId} の情報を更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"注文更新中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 9.3 注文一覧表示機能
        // =====================================================================
        public List<ChumonDisplayModel> GetAllChumons(bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.TChumons
                    .AsNoTracking()
                    .Include(c => c.Cl)
                    .Include(c => c.So)
                    .Include(c => c.Em)
                    .Include(c => c.TChumonDetails)
                    .AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(c => c.ChFlag != 2);
                }

                return query
                    .OrderByDescending(c => c.ChDate)
                    .ThenByDescending(c => c.ChId)
                    .AsEnumerable()
                    .Select(c => ToDisplayModel(c))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<ChumonDisplayModel>();
            }
        }

        // =====================================================================
        // 9.4 注文検索機能
        // =====================================================================
        public List<ChumonDisplayModel> SearchChumons(
            int? chId = null,
            string? clName = null,
            int? soId = null,
            string? emName = null,
            int? orId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int? stateFlag = null,
            bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.TChumons
                    .AsNoTracking()
                    .Include(c => c.Cl)
                    .Include(c => c.So)
                    .Include(c => c.Em)
                    .Include(c => c.TChumonDetails)
                    .AsQueryable();

                if (chId.HasValue && chId.Value > 0)
                {
                    query = query.Where(c => c.ChId == chId.Value);
                }

                if (!string.IsNullOrWhiteSpace(clName))
                {
                    string trimmed = clName.Trim();
                    query = query.Where(c => c.Cl.ClName.Contains(trimmed));
                }

                if (soId.HasValue && soId.Value > 0)
                {
                    query = query.Where(c => c.SoId == soId.Value);
                }

                if (!string.IsNullOrWhiteSpace(emName))
                {
                    string trimmed = emName.Trim();
                    query = query.Where(c => c.Em != null && c.Em.EmName.Contains(trimmed));
                }

                if (orId.HasValue && orId.Value > 0)
                {
                    query = query.Where(c => c.OrId == orId.Value);
                }

                if (dateFrom.HasValue)
                {
                    query = query.Where(c => c.ChDate >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(c => c.ChDate <= dateTo.Value.Date);
                }

                if (stateFlag.HasValue)
                {
                    query = query.Where(c => c.ChStateFlag == stateFlag.Value);
                }

                if (!includeHidden)
                {
                    query = query.Where(c => c.ChFlag != 2);
                }

                return query
                    .OrderByDescending(c => c.ChDate)
                    .ThenByDescending(c => c.ChId)
                    .AsEnumerable()
                    .Select(c => ToDisplayModel(c))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<ChumonDisplayModel>();
            }
        }

        // =====================================================================
        // 9.5 注文非表示機能 (フラグ 0 -> 2 更新)
        // =====================================================================
        public (bool Success, string Message) HideChumon(int chId, string reason)
        {
            if (chId <= 0)
            {
                return (false, "対象の注文IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.TChumons.FirstOrDefault(c => c.ChId == chId);
                if (target == null)
                {
                    return (false, $"注文ID: {chId} が見つかりませんでした。");
                }

                target.ChFlag = 2;
                target.ChHidden = string.IsNullOrWhiteSpace(reason) ? "管理者による非表示設定" : reason.Trim();

                context.SaveChanges();
                return (true, $"注文ID: {chId} を非表示に設定しました。");
            }
            catch (Exception ex)
            {
                return (false, $"注文非表示処理中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 注文明細取得
        // =====================================================================
        public List<ChumonDetailDisplayModel> GetChumonDetails(int chId)
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.TChumonDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.ChId == chId)
                    .Select(d => new ChumonDetailDisplayModel
                    {
                        ChDetailId = d.ChDetailId,
                        ChId = d.ChId,
                        PrId = d.PrId,
                        PrName = d.Pr != null ? d.Pr.PrName : $"商品ID: {d.PrId}",
                        ChQuantity = d.ChQuantity
                    })
                    .ToList();
            }
            catch
            {
                return new List<ChumonDetailDisplayModel>();
            }
        }

        // =====================================================================
        // マスタ取得ヘルパー
        // =====================================================================
        public List<MSalesOffice> GetSalesOffices()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MSalesOffices
                    .AsNoTracking()
                    .Where(s => s.SoFlag != 2)
                    .OrderBy(s => s.SoId)
                    .ToList();
            }
            catch
            {
                return new List<MSalesOffice>();
            }
        }

        public List<MClient> GetClients()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MClients
                    .AsNoTracking()
                    .Where(c => c.ClFlag != 2)
                    .OrderBy(c => c.ClId)
                    .ToList();
            }
            catch
            {
                return new List<MClient>();
            }
        }

        public List<MEmployee> GetEmployees()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MEmployees
                    .AsNoTracking()
                    .Where(e => e.EmFlag != 2)
                    .OrderBy(e => e.EmId)
                    .ToList();
            }
            catch
            {
                return new List<MEmployee>();
            }
        }

        public List<TOrder> GetOrders()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.TOrders
                    .AsNoTracking()
                    .Where(o => o.OrFlag != 2)
                    .OrderByDescending(o => o.OrId)
                    .ToList();
            }
            catch
            {
                return new List<TOrder>();
            }
        }

        public List<MProduct> GetProducts()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MProducts
                    .AsNoTracking()
                    .Where(p => p.PrFlag != 2)
                    .OrderBy(p => p.PrId)
                    .ToList();
            }
            catch
            {
                return new List<MProduct>();
            }
        }

        private ChumonDisplayModel ToDisplayModel(TChumon c)
        {
            int count = c.TChumonDetails?.Count ?? 0;
            int totalQty = c.TChumonDetails?.Sum(d => d.ChQuantity) ?? 0;

            return new ChumonDisplayModel
            {
                ChId = c.ChId,
                SoId = c.SoId,
                SoName = c.So?.SoName ?? $"営業所 #{c.SoId}",
                EmId = c.EmId,
                EmName = c.Em?.EmName ?? (c.EmId.HasValue ? $"社員 #{c.EmId}" : "未設定"),
                ClId = c.ClId,
                ClName = c.Cl?.ClName ?? $"顧客 #{c.ClId}",
                OrId = c.OrId,
                ChDate = c.ChDate,
                DetailCount = count,
                TotalQuantity = totalQty,
                ChStateFlag = c.ChStateFlag,
                ChFlag = c.ChFlag,
                ChHidden = c.ChHidden
            };
        }
    }
}
