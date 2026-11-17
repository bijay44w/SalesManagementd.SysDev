using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 出荷一覧・検索結果の画面表示用モデル
    /// </summary>
    public class ShipmentDisplayModel
    {
        public int ShId { get; set; }
        public int ClId { get; set; }
        public string ClName { get; set; } = string.Empty;
        public int SoId { get; set; }
        public string SoName { get; set; } = string.Empty;
        public int? EmId { get; set; }
        public string EmName { get; set; } = string.Empty;
        public int OrId { get; set; }
        public DateTime? ShFinishDate { get; set; }
        public string ShFinishDateFormatted => ShFinishDate.HasValue ? ShFinishDate.Value.ToString("yyyy/MM/dd") : "-";
        public int DetailCount { get; set; }
        public int TotalQuantity { get; set; }
        public int? ShStateFlag { get; set; }
        public string StateFormatted => ShStateFlag switch
        {
            1 => "確定済",
            _ => "未確定"
        };
        public int ShFlag { get; set; }
        public string FlagFormatted => ShFlag switch
        {
            0 => "表示中",
            2 => "非表示",
            _ => $"その他({ShFlag})"
        };
        public string? ShHidden { get; set; }
    }

    /// <summary>
    /// 出荷明細の画面表示用モデル
    /// </summary>
    public class ShipmentDetailDisplayModel
    {
        public int ShDetailId { get; set; }
        public int ShId { get; set; }
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int ShQuantity { get; set; }
    }

    /// <summary>
    /// 14. 出荷管理サービス
    /// 14.1 出荷登録機能
    /// 14.2 出荷更新機能 (出荷確定/在庫減算含む)
    /// 14.3 出荷一覧表示機能
    /// 14.4 出荷検索機能
    /// 14.5 出荷非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public class ShipmentService
    {
        // =====================================================================
        // 14.1 出荷登録機能
        // =====================================================================
        public (bool Success, string Message, TShipment? Shipment) RegisterShipment(TShipment shipment, List<TShipmentDetail>? details = null)
        {
            if (shipment == null)
            {
                return (false, "出荷情報が指定されていません。", null);
            }

            if (shipment.OrId <= 0)
            {
                return (false, "受注を選択してください。", null);
            }

            if (shipment.ClId <= 0)
            {
                return (false, "顧客を選択してください。", null);
            }

            if (shipment.SoId <= 0)
            {
                return (false, "営業所を選択してください。", null);
            }

            if (details == null || details.Count == 0)
            {
                return (false, "出荷明細を1件以上指定してください。", null);
            }

            try
            {
                using var context = new SalesManagementContext();
                using var transaction = context.Database.BeginTransaction();

                shipment.ShFlag = 0;
                shipment.ShHidden = null;
                if (!shipment.ShStateFlag.HasValue)
                {
                    shipment.ShStateFlag = 0;
                }

                shipment.Cl = null!;
                shipment.So = null!;
                shipment.Em = null;
                shipment.Or = null!;
                shipment.TShipmentDetails = new List<TShipmentDetail>();

                context.TShipments.Add(shipment);
                context.SaveChanges();

                foreach (var d in details)
                {
                    if (d.PrId <= 0 || d.ShQuantity <= 0)
                    {
                        transaction.Rollback();
                        return (false, "商品および有効な数量（1以上）を指定してください。", null);
                    }

                    d.ShId = shipment.ShId;
                    d.Pr = null!;
                    d.Sh = null!;
                    context.TShipmentDetails.Add(d);
                }
                context.SaveChanges();

                transaction.Commit();
                return (true, $"出荷ID: {shipment.ShId} を登録しました。", shipment);
            }
            catch (Exception ex)
            {
                return (false, $"出荷登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        // =====================================================================
        // 14.2 出荷更新機能
        // =====================================================================
        public (bool Success, string Message) UpdateShipment(
            int shId,
            int? emId,
            DateTime? finishDate,
            int? stateFlag,
            List<TShipmentDetail>? details = null)
        {
            if (shId <= 0)
            {
                return (false, "対象の出荷IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                using var transaction = context.Database.BeginTransaction();

                var target = context.TShipments
                    .Include(s => s.TShipmentDetails)
                    .FirstOrDefault(s => s.ShId == shId);

                if (target == null)
                {
                    return (false, $"出荷ID: {shId} が見つかりませんでした。");
                }

                if (emId.HasValue && emId.Value > 0)
                {
                    target.EmId = emId.Value;
                }

                if (finishDate.HasValue)
                {
                    target.ShFinishDate = finishDate.Value;
                }

                bool becomingConfirmed = (target.ShStateFlag != 1 && stateFlag == 1);

                if (stateFlag.HasValue)
                {
                    target.ShStateFlag = stateFlag.Value;
                }

                if (details != null && details.Count > 0)
                {
                    context.TShipmentDetails.RemoveRange(target.TShipmentDetails);
                    foreach (var d in details)
                    {
                        d.ShId = shId;
                        d.Pr = null!;
                        d.Sh = null!;
                        context.TShipmentDetails.Add(d);
                    }
                }

                // 確定状態になった場合の在庫減算処理
                if (becomingConfirmed)
                {
                    // 出庫管理（TSyukko）で既に在庫が引き落とされているか判定（二重減算防止）
                    bool hasSyukko = context.TSyukkos.Any(s => s.OrId == target.OrId && s.SyFlag == 0);
                    if (!hasSyukko)
                    {
                        var detailsToDeduct = details ?? target.TShipmentDetails.ToList();
                        foreach (var item in detailsToDeduct)
                        {
                            var stock = context.TStocks.FirstOrDefault(s => s.PrId == item.PrId && s.StFlag == 0);
                            if (stock != null)
                            {
                                stock.StQuantity = Math.Max(0, stock.StQuantity - item.ShQuantity);
                            }
                        }
                    }
                }

                context.SaveChanges();
                transaction.Commit();
                return (true, $"出荷ID: {shId} の情報を更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"出荷更新中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 14.3 出荷一覧表示機能
        // =====================================================================
        public List<ShipmentDisplayModel> GetAllShipments(bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.TShipments
                    .AsNoTracking()
                    .Include(s => s.Cl)
                    .Include(s => s.So)
                    .Include(s => s.Em)
                    .Include(s => s.TShipmentDetails)
                    .AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(s => s.ShFlag != 2);
                }

                return query
                    .OrderByDescending(s => s.ShFinishDate ?? DateTime.MinValue)
                    .ThenByDescending(s => s.ShId)
                    .AsEnumerable()
                    .Select(s => ToDisplayModel(s))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<ShipmentDisplayModel>();
            }
        }

        // =====================================================================
        // 14.4 出荷検索機能
        // =====================================================================
        public List<ShipmentDisplayModel> SearchShipments(
            int? shId = null,
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
                var query = context.TShipments
                    .AsNoTracking()
                    .Include(s => s.Cl)
                    .Include(s => s.So)
                    .Include(s => s.Em)
                    .Include(s => s.TShipmentDetails)
                    .AsQueryable();

                if (shId.HasValue && shId.Value > 0)
                {
                    query = query.Where(s => s.ShId == shId.Value);
                }

                if (orId.HasValue && orId.Value > 0)
                {
                    query = query.Where(s => s.OrId == orId.Value);
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
                    query = query.Where(s => s.Em != null && s.Em.EmName.Contains(trimmed));
                }

                if (dateFrom.HasValue)
                {
                    query = query.Where(s => s.ShFinishDate.HasValue && s.ShFinishDate.Value >= dateFrom.Value.Date);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(s => s.ShFinishDate.HasValue && s.ShFinishDate.Value <= dateTo.Value.Date);
                }

                if (stateFlag.HasValue)
                {
                    query = query.Where(s => s.ShStateFlag == stateFlag.Value);
                }

                if (!includeHidden)
                {
                    query = query.Where(s => s.ShFlag != 2);
                }

                return query
                    .OrderByDescending(s => s.ShFinishDate ?? DateTime.MinValue)
                    .ThenByDescending(s => s.ShId)
                    .AsEnumerable()
                    .Select(s => ToDisplayModel(s))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<ShipmentDisplayModel>();
            }
        }

        // =====================================================================
        // 14.5 出荷非表示機能 (フラグ 0 -> 2 更新)
        // =====================================================================
        public (bool Success, string Message) HideShipment(int shId, string reason)
        {
            if (shId <= 0)
            {
                return (false, "対象の出荷IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.TShipments.FirstOrDefault(s => s.ShId == shId);
                if (target == null)
                {
                    return (false, $"出荷ID: {shId} が見つかりませんでした。");
                }

                target.ShFlag = 2;
                target.ShHidden = string.IsNullOrWhiteSpace(reason) ? "管理者による非表示設定" : reason.Trim();

                context.SaveChanges();
                return (true, $"出荷ID: {shId} を非表示に設定しました。");
            }
            catch (Exception ex)
            {
                return (false, $"出荷非表示処理中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 出荷明細取得
        // =====================================================================
        public List<ShipmentDetailDisplayModel> GetShipmentDetails(int shId)
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.TShipmentDetails
                    .AsNoTracking()
                    .Include(d => d.Pr)
                    .Where(d => d.ShId == shId)
                    .Select(d => new ShipmentDetailDisplayModel
                    {
                        ShDetailId = d.ShDetailId,
                        ShId = d.ShId,
                        PrId = d.PrId,
                        PrName = d.Pr != null ? d.Pr.PrName : $"商品ID: {d.PrId}",
                        ShQuantity = d.ShQuantity
                    })
                    .ToList();
            }
            catch
            {
                return new List<ShipmentDetailDisplayModel>();
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

        public List<TOrder> GetOrders()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.TOrders
                    .AsNoTracking()
                    .Include(o => o.Cl)
                    .Include(o => o.So)
                    .Include(o => o.TOrderDetails)
                        .ThenInclude(d => d.Pr)
                    .Include(o => o.TSyukkos)
                    .Include(o => o.TArrivals)
                    .Include(o => o.TShipments)
                    .Where(o => o.OrFlag != 2)
                    .OrderByDescending(o => o.OrId)
                    .ToList();
            }
            catch
            {
                return new List<TOrder>();
            }
        }

        /// <summary>
        /// 出荷完全削除機能
        /// </summary>
        public (bool Success, string Message) DeleteShipment(int shId)
        {
            if (shId <= 0)
            {
                return (false, "削除対象の出荷IDが正しくありません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var shipment = context.TShipments
                    .Include(s => s.TShipmentDetails)
                    .FirstOrDefault(s => s.ShId == shId);

                if (shipment == null)
                {
                    return (false, $"出荷ID: {shId} のデータが見つかりません。");
                }

                if (shipment.ShStateFlag == 1)
                {
                    return (false,
                        "確定済みの出荷データは在庫処理に反映されているため、完全削除できません。\n非表示機能を使用してください。");
                }

                if (shipment.TShipmentDetails != null && shipment.TShipmentDetails.Any())
                {
                    context.TShipmentDetails.RemoveRange(shipment.TShipmentDetails);
                }

                context.TShipments.Remove(shipment);
                context.SaveChanges();

                return (true, $"出荷ID: {shId} の出荷データを完全に削除しました。");
            }
            catch (Exception ex)
            {
                return (false, $"出荷削除中にエラーが発生しました: {ex.Message}");
            }
        }

        private ShipmentDisplayModel ToDisplayModel(TShipment s)
        {
            int count = s.TShipmentDetails?.Count ?? 0;
            int totalQty = s.TShipmentDetails?.Sum(d => d.ShQuantity) ?? 0;

            return new ShipmentDisplayModel
            {
                ShId = s.ShId,
                ClId = s.ClId,
                ClName = StringSanitizer.FixMojibake(s.Cl?.ClName) ?? $"顧客 #{s.ClId}",
                SoId = s.SoId,
                SoName = s.So?.SoName ?? $"営業所 #{s.SoId}",
                EmId = s.EmId,
                EmName = StringSanitizer.FixMojibake(s.Em?.EmName) ?? (s.EmId.HasValue ? $"社員 #{s.EmId}" : "-"),
                OrId = s.OrId,
                ShFinishDate = s.ShFinishDate,
                DetailCount = count,
                TotalQuantity = totalQty,
                ShStateFlag = s.ShStateFlag,
                ShFlag = s.ShFlag,
                ShHidden = s.ShHidden
            };
        }
    }
}
