using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 受注一覧・検索結果の画面表示用モデル
    /// </summary>
    public class OrderDisplayModel
    {
        public int OrId { get; set; }
        public int ClId { get; set; }
        public string ClName { get; set; } = string.Empty;
        public string ClCharge { get; set; } = string.Empty;
        public int SoId { get; set; }
        public string SoName { get; set; } = string.Empty;
        public int EmId { get; set; }
        public string EmName { get; set; } = string.Empty;
        public DateTime OrDate { get; set; }
        public string OrDateFormatted => OrDate.ToString("yyyy/MM/dd");
        public decimal TotalAmount { get; set; }
        public string TotalAmountFormatted => $"{TotalAmount:N0} 円";
        public int DetailCount { get; set; }
        public int? OrStateFlag { get; set; }
        public string StateFormatted => OrStateFlag switch
        {
            1 => "確定済",
            _ => "未確定"
        };
        public int OrFlag { get; set; }
        public string FlagFormatted => OrFlag switch
        {
            0 => "表示中",
            2 => "非表示",
            _ => $"その他({OrFlag})"
        };
        public string? OrHidden { get; set; }
    }

    /// <summary>
    /// 受注明細の画面表示用モデル
    /// </summary>
    public class OrderDetailDisplayModel
    {
        public int OrDetailId { get; set; }
        public int OrId { get; set; }
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int OrQuantity { get; set; }
        public decimal OrTotalPrice { get; set; }
        public string OrTotalPriceFormatted => $"{OrTotalPrice:N0} 円";
    }

    /// <summary>
    /// 8. 受注管理サービス
    /// 8.1 受注登録機能
    /// 8.2 受注更新機能
    /// 8.3 受注一覧表示機能
    /// 8.4 受注検索機能
    /// 8.5 受注非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public class OrderService
    {
        // =====================================================================
        // 8.1 受注登録機能
        // =====================================================================
        public (bool Success, string Message, TOrder? Order) RegisterOrder(TOrder order, List<TOrderDetail>? details = null)
        {
            if (order == null)
            {
                return (false, "受注情報が指定されていません。", null);
            }

            if (order.ClId <= 0)
            {
                return (false, "顧客を選択してください。", null);
            }

            if (order.SoId <= 0)
            {
                return (false, "営業所を選択してください。", null);
            }

            if (order.EmId <= 0)
            {
                return (false, "担当社員を選択してください。", null);
            }

            if (order.OrDate == default)
            {
                order.OrDate = DateTime.Today;
            }

            try
            {
                using var context = new SalesManagementContext();

                order.OrFlag = 0;
                order.OrStateFlag = 0;
                order.OrHidden = null;
                order.ClCharge = (order.ClCharge ?? string.Empty).Trim();

                context.TOrders.Add(order);
                context.SaveChanges();

                if (details != null && details.Count > 0)
                {
                    foreach (var d in details)
                    {
                        d.OrId = order.OrId;
                        context.TOrderDetails.Add(d);
                    }
                    context.SaveChanges();
                }

                return (true, $"受注ID: {order.OrId} を正常に登録しました。", order);
            }
            catch (Exception ex)
            {
                return (false, $"受注登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        // =====================================================================
        // 8.2 受注更新機能
        // =====================================================================
        public (bool Success, string Message) UpdateOrder(int orId, int clId, string? clCharge, int soId, int emId, DateTime orDate, int? stateFlag = null)
        {
            if (orId <= 0)
            {
                return (false, "受注IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.TOrders.FirstOrDefault(o => o.OrId == orId);
                if (target == null)
                {
                    return (false, $"受注ID: {orId} が見つかりませんでした。");
                }

                if (clId > 0) target.ClId = clId;
                if (clCharge != null) target.ClCharge = clCharge.Trim();
                if (soId > 0) target.SoId = soId;
                if (emId > 0) target.EmId = emId;
                if (orDate != default) target.OrDate = orDate;
                if (stateFlag.HasValue) target.OrStateFlag = stateFlag.Value;

                context.SaveChanges();
                return (true, $"受注ID: {orId} の情報を更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"受注更新中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 8.3 受注一覧表示機能
        // =====================================================================
        public List<OrderDisplayModel> GetAllOrders(bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.TOrders
                    .AsNoTracking()
                    .Include(o => o.Cl)
                    .Include(o => o.So)
                    .Include(o => o.Em)
                    .Include(o => o.TOrderDetails)
                    .AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(o => o.OrFlag != 2);
                }

                return query
                    .OrderByDescending(o => o.OrDate)
                    .ThenByDescending(o => o.OrId)
                    .AsEnumerable()
                    .Select(o => ToDisplayModel(o))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<OrderDisplayModel>();
            }
        }

        // =====================================================================
        // 8.4 受注検索機能
        // =====================================================================
        public List<OrderDisplayModel> SearchOrders(
            int? orId = null,
            string? clName = null,
            int? soId = null,
            string? emName = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int? stateFlag = null,
            bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.TOrders
                    .AsNoTracking()
                    .Include(o => o.Cl)
                    .Include(o => o.So)
                    .Include(o => o.Em)
                    .Include(o => o.TOrderDetails)
                    .AsQueryable();

                if (orId.HasValue && orId.Value > 0)
                {
                    query = query.Where(o => o.OrId == orId.Value);
                }

                if (!string.IsNullOrWhiteSpace(clName))
                {
                    string trimmed = clName.Trim();
                    query = query.Where(o => o.Cl.ClName.Contains(trimmed));
                }

                if (soId.HasValue && soId.Value > 0)
                {
                    query = query.Where(o => o.SoId == soId.Value);
                }

                if (!string.IsNullOrWhiteSpace(emName))
                {
                    string trimmed = emName.Trim();
                    query = query.Where(o => o.Em.EmName.Contains(trimmed));
                }

                if (dateFrom.HasValue)
                {
                    query = query.Where(o => o.OrDate >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(o => o.OrDate <= dateTo.Value.Date);
                }

                if (stateFlag.HasValue)
                {
                    query = query.Where(o => o.OrStateFlag == stateFlag.Value);
                }

                if (!includeHidden)
                {
                    query = query.Where(o => o.OrFlag != 2);
                }

                return query
                    .OrderByDescending(o => o.OrDate)
                    .ThenByDescending(o => o.OrId)
                    .AsEnumerable()
                    .Select(o => ToDisplayModel(o))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<OrderDisplayModel>();
            }
        }

        // =====================================================================
        // 8.5 受注非表示機能 (フラグ 0 -> 2 更新)
        // =====================================================================
        public (bool Success, string Message) HideOrder(int orId, string reason)
        {
            if (orId <= 0)
            {
                return (false, "対象の受注IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.TOrders.FirstOrDefault(o => o.OrId == orId);
                if (target == null)
                {
                    return (false, $"受注ID: {orId} が見つかりませんでした。");
                }

                target.OrFlag = 2;
                target.OrHidden = string.IsNullOrWhiteSpace(reason) ? "管理者による非表示設定" : reason.Trim();

                context.SaveChanges();
                return (true, $"受注ID: {orId} を非表示に設定しました。");
            }
            catch (Exception ex)
            {
                return (false, $"受注非表示処理中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 受注明細取得
        // =====================================================================
        public List<OrderDetailDisplayModel> GetOrderDetails(int orId)
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.TOrderDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.OrId == orId)
                    .Select(d => new OrderDetailDisplayModel
                    {
                        OrDetailId = d.OrDetailId,
                        OrId = d.OrId,
                        PrId = d.PrId,
                        PrName = d.Pr != null ? d.Pr.PrName : $"商品ID: {d.PrId}",
                        OrQuantity = d.OrQuantity,
                        OrTotalPrice = d.OrTotalPrice
                    })
                    .ToList();
            }
            catch
            {
                return new List<OrderDetailDisplayModel>();
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

        private OrderDisplayModel ToDisplayModel(TOrder o)
        {
            decimal total = o.TOrderDetails?.Sum(d => d.OrTotalPrice) ?? 0m;
            int count = o.TOrderDetails?.Count ?? 0;

            return new OrderDisplayModel
            {
                OrId = o.OrId,
                ClId = o.ClId,
                ClName = StringSanitizer.FixMojibake(o.Cl?.ClName) ?? $"顧客 #{o.ClId}",
                ClCharge = StringSanitizer.FixMojibake(o.ClCharge),
                SoId = o.SoId,
                SoName = o.So?.SoName ?? $"営業所 #{o.SoId}",
                EmId = o.EmId,
                EmName = StringSanitizer.FixMojibake(o.Em?.EmName) ?? $"社員 #{o.EmId}",
                OrDate = o.OrDate,
                TotalAmount = total,
                DetailCount = count,
                OrStateFlag = o.OrStateFlag,
                OrFlag = o.OrFlag,
                OrHidden = o.OrHidden
            };
        }
    }
}
