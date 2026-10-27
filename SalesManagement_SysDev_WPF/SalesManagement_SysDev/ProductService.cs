using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 商品一覧・検索結果の画面表示用モデル
    /// </summary>
    public class ProductDisplayModel
    {
        public int PrId { get; set; }
        public string PrName { get; set; } = string.Empty;
        public int MaId { get; set; }
        public string MaName { get; set; } = string.Empty;
        public int ScId { get; set; }
        public string ScName { get; set; } = string.Empty;
        public int McId { get; set; }
        public string McName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PriceFormatted => $"{Price:N0} 円";
        public string? PrJcode { get; set; }
        public int PrSafetyStock { get; set; }
        public string PrModelNumber { get; set; } = string.Empty;
        public string PrColor { get; set; } = string.Empty;
        public DateTime PrReleaseDate { get; set; }
        public string ReleaseDateFormatted => PrReleaseDate.ToString("yyyy/MM/dd");
        public int PrFlag { get; set; }
        public string FlagFormatted => PrFlag switch
        {
            0 => "表示中",
            2 => "非表示",
            _ => $"その他({PrFlag})"
        };
        public string? PrHidden { get; set; }
        public int StockQuantity { get; set; }
        public string StockFormatted => $"{StockQuantity:N0} 個";
    }

    /// <summary>
    /// 商品管理サービス
    /// 1.1 商品登録機能 (商品登録 ＋ 在庫数0レコード自動生成)
    /// 4.2 商品更新機能
    /// 4.3 商品一覧表示機能
    /// 4.4 商品検索機能
    /// 4.5 商品非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public class ProductService
    {
        // =====================================================================
        // 1.1 商品登録機能
        // • 入力された商品情報を商品テーブルに登録する。
        // • 商品情報をもとに在庫テーブルに在庫数0のレコードを作成する。
        // =====================================================================
        public (bool Success, string Message, MProduct? Product) RegisterProduct(MProduct product)
        {
            if (product == null)
            {
                return (false, "商品情報が指定されていません。", null);
            }

            // バリデーション: 商品名
            if (string.IsNullOrWhiteSpace(product.PrName))
            {
                return (false, "商品名を入力してください。", null);
            }
            if (product.PrName.Length > 50)
            {
                return (false, "商品名は50文字以内で入力してください。", null);
            }

            // バリデーション: 価格
            if (product.Price < 0)
            {
                return (false, "価格は0円以上を入力してください。", null);
            }

            // バリデーション: メーカー
            if (product.MaId <= 0)
            {
                return (false, "メーカーを選択してください。", null);
            }

            // バリデーション: 小分類
            if (product.ScId <= 0)
            {
                return (false, "小分類を選択してください。", null);
            }

            // バリデーション: 安全在庫数
            if (product.PrSafetyStock < 0)
            {
                return (false, "安全在庫数は0以上を入力してください。", null);
            }

            // 型番・色の整形
            product.PrModelNumber = (product.PrModelNumber ?? string.Empty).Trim();
            if (product.PrModelNumber.Length > 20)
            {
                return (false, "型番は20文字以内で入力してください。", null);
            }
            if (string.IsNullOrEmpty(product.PrModelNumber))
            {
                product.PrModelNumber = "-";
            }

            product.PrColor = (product.PrColor ?? string.Empty).Trim();
            if (product.PrColor.Length > 20)
            {
                return (false, "色は20文字以内で入力してください。", null);
            }
            if (string.IsNullOrEmpty(product.PrColor))
            {
                product.PrColor = "-";
            }

            // JANコード
            if (!string.IsNullOrWhiteSpace(product.PrJcode))
            {
                product.PrJcode = product.PrJcode.Trim();
                if (product.PrJcode.Length > 13)
                {
                    return (false, "JANコードは13文字以内で入力してください。", null);
                }
            }
            else
            {
                product.PrJcode = null;
            }

            // 発売日 (未設定なら当日)
            if (product.PrReleaseDate == default)
            {
                product.PrReleaseDate = DateTime.Today;
            }

            // 初期フラグは 0 (有効)
            product.PrFlag = 0;

            try
            {
                using var context = new SalesManagementContext();
                using var transaction = context.Database.BeginTransaction();

                // メーカー存在確認
                bool makerExists = context.MMakers.Any(m => m.MaId == product.MaId);
                if (!makerExists)
                {
                    return (false, "指定されたメーカーが存在しません。", null);
                }

                // 小分類存在確認
                bool scExists = context.MSmallClassifications.Any(s => s.ScId == product.ScId);
                if (!scExists)
                {
                    return (false, "指定された小分類が存在しません。", null);
                }

                // ナビゲーションプロパティの切断 (EF追跡エラー防止)
                product.Ma = null!;
                product.Sc = null!;
                product.TStocks = new List<TStock>();
                product.TArrivalDetails = new List<TArrivalDetail>();
                product.TChumonDetails = new List<TChumonDetail>();
                product.THattyuDetails = new List<THattyuDetail>();
                product.TOrderDetails = new List<TOrderDetail>();
                product.TSaleDetails = new List<TSaleDetail>();
                product.TShipmentDetails = new List<TShipmentDetail>();
                product.TSyukkoDetails = new List<TSyukkoDetail>();
                product.TWarehousingDetails = new List<TWarehousingDetail>();

                // 1. 商品テーブルへの登録
                context.MProducts.Add(product);
                context.SaveChanges();

                // 2. 商品情報をもとに在庫テーブルに在庫数0のレコードを作成する
                var newStock = new TStock
                {
                    PrId = product.PrId,
                    StQuantity = 0,
                    StFlag = 0
                };
                context.TStocks.Add(newStock);
                context.SaveChanges();

                transaction.Commit();

                return (true, $"商品「{product.PrName}」(ID: {product.PrId}) を登録し、在庫テーブルに在庫数0のレコードを作成しました。", product);
            }
            catch (Exception ex)
            {
                return (false, $"商品登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        // =====================================================================
        // 4.2 商品更新機能
        // • 商品テーブルにある商品情報を更新する。
        // =====================================================================
        public (bool Success, string Message) UpdateProduct(MProduct updatedProduct)
        {
            if (updatedProduct == null || updatedProduct.PrId <= 0)
            {
                return (false, "更新対象の商品が正しく指定されていません。");
            }

            // バリデーション: 商品名
            if (string.IsNullOrWhiteSpace(updatedProduct.PrName))
            {
                return (false, "商品名を入力してください。");
            }
            if (updatedProduct.PrName.Length > 50)
            {
                return (false, "商品名は50文字以内で入力してください。");
            }

            // バリデーション: 価格
            if (updatedProduct.Price < 0)
            {
                return (false, "価格は0円以上を入力してください。");
            }

            // バリデーション: メーカー
            if (updatedProduct.MaId <= 0)
            {
                return (false, "メーカーを選択してください。");
            }

            // バリデーション: 小分類
            if (updatedProduct.ScId <= 0)
            {
                return (false, "小分類を選択してください。");
            }

            // バリデーション: 安全在庫数
            if (updatedProduct.PrSafetyStock < 0)
            {
                return (false, "安全在庫数は0以上を入力してください。");
            }

            string modelNumber = (updatedProduct.PrModelNumber ?? string.Empty).Trim();
            if (modelNumber.Length > 20)
            {
                return (false, "型番は20文字以内で入力してください。");
            }
            if (string.IsNullOrEmpty(modelNumber))
            {
                modelNumber = "-";
            }

            string color = (updatedProduct.PrColor ?? string.Empty).Trim();
            if (color.Length > 20)
            {
                return (false, "色は20文字以内で入力してください。");
            }
            if (string.IsNullOrEmpty(color))
            {
                color = "-";
            }

            string? jcode = string.IsNullOrWhiteSpace(updatedProduct.PrJcode) ? null : updatedProduct.PrJcode.Trim();
            if (jcode != null && jcode.Length > 13)
            {
                return (false, "JANコードは13文字以内で入力してください。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var existing = context.MProducts.FirstOrDefault(p => p.PrId == updatedProduct.PrId);
                if (existing == null)
                {
                    return (false, $"商品ID: {updatedProduct.PrId} の商品は見つかりませんでした。");
                }

                // メーカー存在確認
                if (!context.MMakers.Any(m => m.MaId == updatedProduct.MaId))
                {
                    return (false, "指定されたメーカーが存在しません。");
                }

                // 小分類存在確認
                if (!context.MSmallClassifications.Any(s => s.ScId == updatedProduct.ScId))
                {
                    return (false, "指定された小分類が存在しません。");
                }

                existing.PrName = updatedProduct.PrName.Trim();
                existing.MaId = updatedProduct.MaId;
                existing.ScId = updatedProduct.ScId;
                existing.Price = updatedProduct.Price;
                existing.PrSafetyStock = updatedProduct.PrSafetyStock;
                existing.PrModelNumber = modelNumber;
                existing.PrColor = color;
                existing.PrJcode = jcode;
                if (updatedProduct.PrReleaseDate != default)
                {
                    existing.PrReleaseDate = updatedProduct.PrReleaseDate;
                }
                existing.PrFlag = updatedProduct.PrFlag;
                existing.PrHidden = updatedProduct.PrHidden;

                context.SaveChanges();

                return (true, $"商品ID: {updatedProduct.PrId}「{existing.PrName}」の情報を更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"商品更新中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 4.3 商品一覧表示機能
        // • 登録されている商品情報を一覧で表示する。
        // =====================================================================
        public List<ProductDisplayModel> GetAllProducts(bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();

                var query = context.MProducts
                    .Include(p => p.Ma)
                    .Include(p => p.Sc)
                        .ThenInclude(sc => sc.Mc)
                    .Include(p => p.TStocks)
                    .AsNoTracking();

                if (!includeHidden)
                {
                    query = query.Where(p => p.PrFlag == 0);
                }

                var list = query
                    .OrderBy(p => p.PrId)
                    .ToList();

                return list.Select(ToDisplayModel).ToList();
            }
            catch (Exception)
            {
                return new List<ProductDisplayModel>();
            }
        }

        // =====================================================================
        // 4.4 商品検索機能
        // • 入力された商品情報を抽出する。
        // =====================================================================
        public List<ProductDisplayModel> SearchProducts(
            int? prId = null,
            string? prName = null,
            int? maId = null,
            int? scId = null,
            int? mcId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();

                var query = context.MProducts
                    .Include(p => p.Ma)
                    .Include(p => p.Sc)
                        .ThenInclude(sc => sc.Mc)
                    .Include(p => p.TStocks)
                    .AsNoTracking()
                    .AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(p => p.PrFlag == 0);
                }

                if (prId.HasValue && prId.Value > 0)
                {
                    query = query.Where(p => p.PrId == prId.Value);
                }

                if (!string.IsNullOrWhiteSpace(prName))
                {
                    string namePattern = prName.Trim();
                    query = query.Where(p => p.PrName.Contains(namePattern));
                }

                if (maId.HasValue && maId.Value > 0)
                {
                    query = query.Where(p => p.MaId == maId.Value);
                }

                if (scId.HasValue && scId.Value > 0)
                {
                    query = query.Where(p => p.ScId == scId.Value);
                }

                if (mcId.HasValue && mcId.Value > 0)
                {
                    query = query.Where(p => p.Sc.McId == mcId.Value);
                }

                if (minPrice.HasValue && minPrice.Value >= 0)
                {
                    query = query.Where(p => p.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue && maxPrice.Value >= 0)
                {
                    query = query.Where(p => p.Price <= maxPrice.Value);
                }

                var results = query
                    .OrderBy(p => p.PrId)
                    .ToList();

                return results.Select(ToDisplayModel).ToList();
            }
            catch (Exception)
            {
                return new List<ProductDisplayModel>();
            }
        }

        // =====================================================================
        // 4.5 商品非表示機能(フラグ)
        // • 選択された項目の商品管理フラグを０から２に更新する。
        // =====================================================================
        public (bool Success, string Message) SetProductHiddenFlag(int prId, string? hiddenReason = null)
        {
            if (prId <= 0)
            {
                return (false, "対象の商品IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var product = context.MProducts.FirstOrDefault(p => p.PrId == prId);
                if (product == null)
                {
                    return (false, $"商品ID: {prId} の商品は見つかりませんでした。");
                }

                // 管理フラグを0から2に更新
                product.PrFlag = 2;
                if (!string.IsNullOrWhiteSpace(hiddenReason))
                {
                    product.PrHidden = hiddenReason.Trim();
                }

                context.SaveChanges();

                return (true, $"商品ID: {prId}「{product.PrName}」の管理フラグを 2 (非表示) に更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"商品非表示設定中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 表示モデル変換ヘルパー
        // =====================================================================
        public ProductDisplayModel ToDisplayModel(MProduct product)
        {
            int totalStock = product.TStocks?.Sum(s => s.StQuantity) ?? 0;

            return new ProductDisplayModel
            {
                PrId = product.PrId,
                PrName = StringSanitizer.FixMojibake(product.PrName),
                MaId = product.MaId,
                MaName = product.Ma?.MaName ?? $"(メーカー #{product.MaId})",
                ScId = product.ScId,
                ScName = product.Sc?.ScName ?? $"(小分類 #{product.ScId})",
                McId = product.Sc?.McId ?? 0,
                McName = product.Sc?.Mc?.McName ?? "-",
                Price = product.Price,
                PrJcode = product.PrJcode,
                PrSafetyStock = product.PrSafetyStock,
                PrModelNumber = product.PrModelNumber ?? "-",
                PrColor = StringSanitizer.FixMojibake(product.PrColor) ?? "-",
                PrReleaseDate = product.PrReleaseDate,
                PrFlag = product.PrFlag,
                PrHidden = product.PrHidden,
                StockQuantity = totalStock
            };
        }

        // =====================================================================
        // ヘルパー: 単一商品取得
        // =====================================================================
        public MProduct? GetProductById(int prId)
        {
            if (prId <= 0) return null;
            try
            {
                using var context = new SalesManagementContext();
                return context.MProducts
                    .Include(p => p.Ma)
                    .Include(p => p.Sc)
                        .ThenInclude(sc => sc.Mc)
                    .Include(p => p.TStocks)
                    .AsNoTracking()
                    .FirstOrDefault(p => p.PrId == prId);
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // マスタ取得ヘルパー
        // =====================================================================
        public List<MMaker> GetMakers()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MMakers
                    .Where(m => m.MaFlag == 0)
                    .OrderBy(m => m.MaId)
                    .AsNoTracking()
                    .ToList();
            }
            catch
            {
                return new List<MMaker>();
            }
        }

        public List<MMajorClassification> GetMajorClassifications()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MMajorClassifications
                    .Where(m => m.McFlag == 0)
                    .OrderBy(m => m.McId)
                    .AsNoTracking()
                    .ToList();
            }
            catch
            {
                return new List<MMajorClassification>();
            }
        }

        public List<MSmallClassification> GetSmallClassifications(int? mcId = null)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.MSmallClassifications
                    .Where(s => s.ScFlag == 0)
                    .AsNoTracking();

                if (mcId.HasValue && mcId.Value > 0)
                {
                    query = query.Where(s => s.McId == mcId.Value);
                }

                return query.OrderBy(s => s.ScId).ToList();
            }
            catch
            {
                return new List<MSmallClassification>();
            }
        }

        // =====================================================================
        // テスト・クリーンアップ用: 商品および関連在庫の完全削除
        // =====================================================================
        public bool HardDeleteProductForTest(int prId)
        {
            if (prId <= 0) return false;
            try
            {
                using var context = new SalesManagementContext();
                var stocks = context.TStocks.Where(s => s.PrId == prId).ToList();
                if (stocks.Any())
                {
                    context.TStocks.RemoveRange(stocks);
                }

                var product = context.MProducts.FirstOrDefault(p => p.PrId == prId);
                if (product != null)
                {
                    context.MProducts.Remove(product);
                }
                context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
