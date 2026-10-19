using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 在庫一覧・検索結果の画面表示用モデル
    /// </summary>
    public class StockDisplayModel
    {
        public int StId { get; set; }
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int MaId { get; set; }
        public string MaName { get; set; } = string.Empty;
        public int ScId { get; set; }
        public string ScName { get; set; } = string.Empty;
        public string PrModelNumber { get; set; } = string.Empty;
        public string PrColor { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PriceFormatted => $"{Price:N0} 円";
        public int PrSafetyStock { get; set; }
        public int StQuantity { get; set; }
        public string QuantityFormatted => $"{StQuantity:N0} 個";

        // 在庫状態 (適正 / 不足 / 在庫切れ)
        public string StockStatus => StQuantity switch
        {
            0 => "在庫切れ",
            var q when q < PrSafetyStock => "不足（要発注）",
            _ => "適正"
        };

        public int StFlag { get; set; }
        public string FlagFormatted => StFlag switch
        {
            0 => "表示中",
            2 => "非表示",
            _ => $"その他({StFlag})"
        };
    }

    /// <summary>
    /// 5. 在庫管理 サービス
    /// 5.1 在庫更新機能
    /// 5.2 在庫一覧表示機能
    /// 5.3 在庫検索機能
    /// 5.4 在庫非表示機能(フラグ)
    /// </summary>
    public class StockService
    {
        // =====================================================================
        // 5.1 在庫更新機能
        // • 在庫テーブルにある商品情報を更新する。
        // =====================================================================
        public (bool Success, string Message) UpdateStock(int stId, int newQuantity, int flag = 0)
        {
            if (stId <= 0)
            {
                return (false, "対象の在庫IDが正しく指定されていません。");
            }

            if (newQuantity < 0)
            {
                return (false, "在庫数は0以上を入力してください。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var stock = context.TStocks.Include(s => s.Pr).FirstOrDefault(s => s.StId == stId);
                if (stock == null)
                {
                    return (false, $"在庫ID: {stId} のレコードは見つかりませんでした。");
                }

                stock.StQuantity = newQuantity;
                stock.StFlag = flag;

                context.SaveChanges();

                string prName = stock.Pr?.PrName ?? $"商品ID: {stock.PrId}";
                return (true, $"在庫ID: {stId}（{prName}）の在庫数を {newQuantity:N0} 個に更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"在庫更新中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 5.2 在庫一覧表示機能
        // • 登録されている在庫情報を一覧で表示する。
        // =====================================================================
        public List<StockDisplayModel> GetAllStocks(bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();

                var query = context.TStocks
                    .Include(s => s.Pr)
                        .ThenInclude(p => p.Ma)
                    .Include(s => s.Pr)
                        .ThenInclude(p => p.Sc)
                    .AsNoTracking();

                if (!includeHidden)
                {
                    query = query.Where(s => s.StFlag == 0);
                }

                var list = query
                    .OrderBy(s => s.StId)
                    .ToList();

                return list.Select(ToDisplayModel).ToList();
            }
            catch (Exception)
            {
                return new List<StockDisplayModel>();
            }
        }

        // =====================================================================
        // 5.3 在庫検索機能
        // • 入力された在庫情報を抽出する。
        // =====================================================================
        public List<StockDisplayModel> SearchStocks(
            int? stId = null,
            int? prId = null,
            string? prName = null,
            int? maId = null,
            int? scId = null,
            int? minQty = null,
            int? maxQty = null,
            bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();

                var query = context.TStocks
                    .Include(s => s.Pr)
                        .ThenInclude(p => p.Ma)
                    .Include(s => s.Pr)
                        .ThenInclude(p => p.Sc)
                    .AsNoTracking()
                    .AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(s => s.StFlag == 0);
                }

                if (stId.HasValue && stId.Value > 0)
                {
                    query = query.Where(s => s.StId == stId.Value);
                }

                if (prId.HasValue && prId.Value > 0)
                {
                    query = query.Where(s => s.PrId == prId.Value);
                }

                if (!string.IsNullOrWhiteSpace(prName))
                {
                    string pattern = prName.Trim();
                    query = query.Where(s => s.Pr.PrName.Contains(pattern));
                }

                if (maId.HasValue && maId.Value > 0)
                {
                    query = query.Where(s => s.Pr.MaId == maId.Value);
                }

                if (scId.HasValue && scId.Value > 0)
                {
                    query = query.Where(s => s.Pr.ScId == scId.Value);
                }

                if (minQty.HasValue && minQty.Value >= 0)
                {
                    query = query.Where(s => s.StQuantity >= minQty.Value);
                }

                if (maxQty.HasValue && maxQty.Value >= 0)
                {
                    query = query.Where(s => s.StQuantity <= maxQty.Value);
                }

                var list = query
                    .OrderBy(s => s.StId)
                    .ToList();

                return list.Select(ToDisplayModel).ToList();
            }
            catch (Exception)
            {
                return new List<StockDisplayModel>();
            }
        }

        // =====================================================================
        // 5.4 在庫非表示機能(フラグ)
        // • 選択された項目の在庫管理フラグを０から２に更新する。
        // =====================================================================
        public (bool Success, string Message) SetStockHiddenFlag(int stId)
        {
            if (stId <= 0)
            {
                return (false, "対象の在庫IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var stock = context.TStocks.Include(s => s.Pr).FirstOrDefault(s => s.StId == stId);
                if (stock == null)
                {
                    return (false, $"在庫ID: {stId} のレコードは見つかりませんでした。");
                }

                // 管理フラグを0から2に更新
                stock.StFlag = 2;
                context.SaveChanges();

                string prName = stock.Pr?.PrName ?? $"商品ID: {stock.PrId}";
                return (true, $"在庫ID: {stId}（{prName}）の管理フラグを 2 (非表示) に更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"在庫非表示設定中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 在庫登録（特定商品の在庫レコード作成・補充）
        // =====================================================================
        public (bool Success, string Message, TStock? Stock) RegisterStock(int prId, int quantity)
        {
            if (prId <= 0)
            {
                return (false, "対象商品を選択してください。", null);
            }

            if (quantity < 0)
            {
                return (false, "在庫数は0以上を入力してください。", null);
            }

            try
            {
                using var context = new SalesManagementContext();
                var product = context.MProducts.FirstOrDefault(p => p.PrId == prId);
                if (product == null)
                {
                    return (false, "指定された商品が存在しません。", null);
                }

                // 既に在庫レコードが存在する場合は加算または新規作成
                var existingStock = context.TStocks.FirstOrDefault(s => s.PrId == prId && s.StFlag == 0);
                if (existingStock != null)
                {
                    existingStock.StQuantity += quantity;
                    context.SaveChanges();
                    return (true, $"商品「{product.PrName}」の既存在庫に {quantity} 個追加し、現在在庫は {existingStock.StQuantity} 個になりました。", existingStock);
                }

                var newStock = new TStock
                {
                    PrId = prId,
                    StQuantity = quantity,
                    StFlag = 0
                };
                context.TStocks.Add(newStock);
                context.SaveChanges();

                return (true, $"商品「{product.PrName}」の在庫レコード(ID: {newStock.StId})を在庫数 {quantity} 個で作成しました。", newStock);
            }
            catch (Exception ex)
            {
                return (false, $"在庫登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        // =====================================================================
        // ヘルパー: 単一在庫取得
        // =====================================================================
        public TStock? GetStockById(int stId)
        {
            if (stId <= 0) return null;
            try
            {
                using var context = new SalesManagementContext();
                return context.TStocks
                    .Include(s => s.Pr)
                        .ThenInclude(p => p.Ma)
                    .Include(s => s.Pr)
                        .ThenInclude(p => p.Sc)
                    .AsNoTracking()
                    .FirstOrDefault(s => s.StId == stId);
            }
            catch
            {
                return null;
            }
        }

        public ProductDisplayModel? GetProductInfo(int prId)
        {
            if (prId <= 0) return null;
            try
            {
                using var context = new SalesManagementContext();
                var p = context.MProducts
                    .Include(x => x.Ma)
                    .Include(x => x.Sc)
                    .AsNoTracking()
                    .FirstOrDefault(x => x.PrId == prId);
                if (p == null) return null;

                return new ProductDisplayModel
                {
                    PrId = p.PrId,
                    PrName = p.PrName,
                    MaName = p.Ma?.MaName ?? "-",
                    ScName = p.Sc?.ScName ?? "-",
                    Price = p.Price,
                    PrModelNumber = p.PrModelNumber ?? "-",
                    PrColor = p.PrColor ?? "-",
                    PrSafetyStock = p.PrSafetyStock
                };
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // 表示モデル変換
        // =====================================================================
        public StockDisplayModel ToDisplayModel(TStock stock)
        {
            var p = stock.Pr;
            return new StockDisplayModel
            {
                StId = stock.StId,
                PrId = stock.PrId,
                PrName = p?.PrName ?? $"(商品 #{stock.PrId})",
                MaId = p?.MaId ?? 0,
                MaName = p?.Ma?.MaName ?? "-",
                ScId = p?.ScId ?? 0,
                ScName = p?.Sc?.ScName ?? "-",
                PrModelNumber = p?.PrModelNumber ?? "-",
                PrColor = p?.PrColor ?? "-",
                Price = p?.Price ?? 0,
                PrSafetyStock = p?.PrSafetyStock ?? 0,
                StQuantity = stock.StQuantity,
                StFlag = stock.StFlag
            };
        }

        // =====================================================================
        // テスト用クリーンアップ
        // =====================================================================
        public bool HardDeleteStockForTest(int stId)
        {
            if (stId <= 0) return false;
            try
            {
                using var context = new SalesManagementContext();
                var stock = context.TStocks.FirstOrDefault(s => s.StId == stId);
                if (stock != null)
                {
                    context.TStocks.Remove(stock);
                    context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
