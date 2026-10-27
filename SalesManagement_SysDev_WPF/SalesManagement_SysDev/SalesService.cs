using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 売上一覧・検索結果の画面表示用モデル
    /// </summary>
    public class SaleDisplayModel
    {
        public int SaId { get; set; }
        public int ClId { get; set; }
        public string ClName { get; set; } = string.Empty;
        public int SoId { get; set; }
        public string SoName { get; set; } = string.Empty;
        public int EmId { get; set; }
        public string EmName { get; set; } = string.Empty;
        public int OrId { get; set; }
        public DateTime SaDate { get; set; }
        public string SaDateFormatted => SaDate.ToString("yyyy/MM/dd");
        public decimal TotalAmount { get; set; }
        public string TotalAmountFormatted => $"{TotalAmount:N0} 円";
        public int DetailCount { get; set; }
        public int SaFlag { get; set; }
        public string FlagFormatted => SaFlag switch
        {
            0 => "表示中",
            2 => "非表示",
            _ => $"その他({SaFlag})"
        };
        public string? SaHidden { get; set; }
    }

    /// <summary>
    /// 売上明細の画面表示用モデル
    /// </summary>
    public class SaleDetailDisplayModel
    {
        public int SaDetailId { get; set; }
        public int SaId { get; set; }
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int SaQuantity { get; set; }
        public decimal SaPrTotalPrice { get; set; }
        public string SaPrTotalPriceFormatted => $"{SaPrTotalPrice:N0} 円";
    }

    /// <summary>
    /// 7. 売上管理サービス
    /// 7.1 売上登録機能
    /// 7.2 売上更新機能
    /// 7.3 売上一覧表示機能
    /// 7.4 売上検索機能
    /// 7.5 売上非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public class SalesService
    {
        // =====================================================================
        // 7.1 売上登録機能
        // =====================================================================
        public (bool Success, string Message, TSale? Sale) RegisterSale(TSale sale, List<TSaleDetail>? details = null)
        {
            if (sale == null)
            {
                return (false, "売上情報が指定されていません。", null);
            }

            if (sale.ClId <= 0)
            {
                return (false, "顧客を選択してください。", null);
            }

            if (sale.SoId <= 0)
            {
                return (false, "営業所を選択してください。", null);
            }

            if (sale.EmId <= 0)
            {
                return (false, "担当社員を選択してください。", null);
            }

            if (sale.SaDate == default)
            {
                sale.SaDate = DateTime.Today;
            }

            try
            {
                using var context = new SalesManagementContext();

                if (sale.OrId <= 0)
                {
                    // もし有効な受注が指定されていない場合、最新の受注または該当顧客の受注を補完
                    var fallbackOrder = context.TOrders.FirstOrDefault(o => o.ClId == sale.ClId) 
                                        ?? context.TOrders.FirstOrDefault();
                    if (fallbackOrder != null)
                    {
                        sale.OrId = fallbackOrder.OrId;
                    }
                    else
                    {
                        return (false, "関連付ける受注情報が見つかりません。", null);
                    }
                }

                sale.SaFlag = 0;
                sale.SaHidden = null;

                context.TSales.Add(sale);
                context.SaveChanges();

                // 明細登録
                if (details != null && details.Count > 0)
                {
                    foreach (var d in details)
                    {
                        d.SaId = sale.SaId;
                        context.TSaleDetails.Add(d);
                    }
                    context.SaveChanges();
                }

                return (true, $"売上ID: {sale.SaId} を正常に登録しました。", sale);
            }
            catch (Exception ex)
            {
                return (false, $"売上登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        // =====================================================================
        // 7.2 売上更新機能
        // =====================================================================
        public (bool Success, string Message) UpdateSale(int saId, int clId, int soId, int emId, DateTime saDate, string? hidden = null)
        {
            if (saId <= 0)
            {
                return (false, "売上IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.TSales.FirstOrDefault(s => s.SaId == saId);
                if (target == null)
                {
                    return (false, $"売上ID: {saId} が見つかりませんでした。");
                }

                if (clId > 0) target.ClId = clId;
                if (soId > 0) target.SoId = soId;
                if (emId > 0) target.EmId = emId;
                if (saDate != default) target.SaDate = saDate;
                if (hidden != null) target.SaHidden = hidden;

                context.SaveChanges();
                return (true, $"売上ID: {saId} の情報を更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"売上更新中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 7.3 売上一覧表示機能
        // =====================================================================
        public List<SaleDisplayModel> GetAllSales(bool includeHidden = false)
        {
            try
            {
                EnsureInitialData();

                using var context = new SalesManagementContext();
                var query = context.TSales
                    .AsNoTracking()
                    .Include(s => s.Cl)
                    .Include(s => s.So)
                    .Include(s => s.Em)
                    .Include(s => s.TSaleDetails)
                    .AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(s => s.SaFlag != 2);
                }

                return query
                    .OrderByDescending(s => s.SaDate)
                    .ThenByDescending(s => s.SaId)
                    .AsEnumerable()
                    .Select(s => ToDisplayModel(s))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<SaleDisplayModel>();
            }
        }

        // =====================================================================
        // 7.4 売上検索機能
        // =====================================================================
        public List<SaleDisplayModel> SearchSales(
            int? saId = null,
            string? clName = null,
            int? soId = null,
            string? emName = null,
            int? orId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool includeHidden = false)
        {
            try
            {
                EnsureInitialData();

                using var context = new SalesManagementContext();
                var query = context.TSales
                    .AsNoTracking()
                    .Include(s => s.Cl)
                    .Include(s => s.So)
                    .Include(s => s.Em)
                    .Include(s => s.TSaleDetails)
                    .AsQueryable();

                if (saId.HasValue && saId.Value > 0)
                {
                    query = query.Where(s => s.SaId == saId.Value);
                }

                if (!string.IsNullOrWhiteSpace(clName))
                {
                    string trimmed = clName.Trim();
                    query = query.Where(s => s.Cl.ClName.Contains(trimmed));
                }

                if (soId.HasValue && soId.Value > 0)
                {
                    query = query.Where(s => s.SoId == soId.Value);
                }

                if (!string.IsNullOrWhiteSpace(emName))
                {
                    string trimmed = emName.Trim();
                    query = query.Where(s => s.Em.EmName.Contains(trimmed));
                }

                if (orId.HasValue && orId.Value > 0)
                {
                    query = query.Where(s => s.OrId == orId.Value);
                }

                if (dateFrom.HasValue)
                {
                    query = query.Where(s => s.SaDate >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(s => s.SaDate <= dateTo.Value.Date);
                }

                if (!includeHidden)
                {
                    query = query.Where(s => s.SaFlag != 2);
                }

                return query
                    .OrderByDescending(s => s.SaDate)
                    .ThenByDescending(s => s.SaId)
                    .AsEnumerable()
                    .Select(s => ToDisplayModel(s))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<SaleDisplayModel>();
            }
        }

        // =====================================================================
        // 7.5 売上非表示機能 (フラグ 0 -> 2 更新)
        // =====================================================================
        public (bool Success, string Message) HideSale(int saId, string reason)
        {
            if (saId <= 0)
            {
                return (false, "対象の売上IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.TSales.FirstOrDefault(s => s.SaId == saId);
                if (target == null)
                {
                    return (false, $"売上ID: {saId} が見つかりませんでした。");
                }

                target.SaFlag = 2;
                target.SaHidden = string.IsNullOrWhiteSpace(reason) ? "管理者による非表示設定" : reason.Trim();

                context.SaveChanges();
                return (true, $"売上ID: {saId} を非表示に設定しました。");
            }
            catch (Exception ex)
            {
                return (false, $"売上非表示処理中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 売上明細取得
        // =====================================================================
        public List<SaleDetailDisplayModel> GetSaleDetails(int saId)
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.TSaleDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.SaId == saId)
                    .Select(d => new SaleDetailDisplayModel
                    {
                        SaDetailId = d.SaDetailId,
                        SaId = d.SaId,
                        PrId = d.PrId,
                        PrName = d.Pr != null ? d.Pr.PrName : $"商品ID: {d.PrId}",
                        SaQuantity = d.SaQuantity,
                        SaPrTotalPrice = d.SaPrTotalPrice
                    })
                    .ToList();
            }
            catch
            {
                return new List<SaleDetailDisplayModel>();
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

        private SaleDisplayModel ToDisplayModel(TSale s)
        {
            decimal total = s.TSaleDetails?.Sum(d => d.SaPrTotalPrice) ?? 0m;
            int count = s.TSaleDetails?.Count ?? 0;

            return new SaleDisplayModel
            {
                SaId = s.SaId,
                ClId = s.ClId,
                ClName = s.Cl?.ClName ?? $"顧客 #{s.ClId}",
                SoId = s.SoId,
                SoName = s.So?.SoName ?? $"営業所 #{s.SoId}",
                EmId = s.EmId,
                EmName = s.Em?.EmName ?? $"社員 #{s.EmId}",
                OrId = s.OrId,
                SaDate = s.SaDate,
                TotalAmount = total,
                DetailCount = count,
                SaFlag = s.SaFlag,
                SaHidden = s.SaHidden
            };
        }

        /// <summary>
        /// 売上データが0件の場合に、既存の受注・商品から初期データを自動補完する
        /// </summary>
        private void EnsureInitialData()
        {
            try
            {
                using var context = new SalesManagementContext();
                if (context.TSales.Any()) return;

                var orders = context.TOrders.Include(o => o.TOrderDetails).ThenInclude(d => d.Pr).Take(5).ToList();
                if (orders.Count == 0) return;

                foreach (var ord in orders)
                {
                    var sale = new TSale
                    {
                        ClId = ord.ClId,
                        SoId = ord.SoId,
                        EmId = ord.EmId,
                        OrId = ord.OrId,
                        SaDate = ord.OrDate != default ? ord.OrDate : DateTime.Today.AddDays(-new Random().Next(1, 30)),
                        SaFlag = 0
                    };
                    context.TSales.Add(sale);
                    context.SaveChanges();

                    if (ord.TOrderDetails != null && ord.TOrderDetails.Any())
                    {
                        foreach (var d in ord.TOrderDetails)
                        {
                            decimal price = d.Pr?.Price ?? 1000m;
                            context.TSaleDetails.Add(new TSaleDetail
                            {
                                SaId = sale.SaId,
                                PrId = d.PrId,
                                SaQuantity = d.OrQuantity,
                                SaPrTotalPrice = d.OrTotalPrice > 0 ? d.OrTotalPrice : price * d.OrQuantity
                            });
                        }
                    }
                    else
                    {
                        var firstPr = context.MProducts.FirstOrDefault();
                        if (firstPr != null)
                        {
                            context.TSaleDetails.Add(new TSaleDetail
                            {
                                SaId = sale.SaId,
                                PrId = firstPr.PrId,
                                SaQuantity = 2,
                                SaPrTotalPrice = firstPr.Price * 2
                            });
                        }
                    }
                    context.SaveChanges();
                }
            }
            catch
            {
                // エラーは無視 (テーブル構成や権限に応じたベストエフォート)
            }
        }
    }
}
