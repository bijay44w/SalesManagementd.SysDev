using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 在庫データアクセス。画面処理とデータベース処理を分離する。
    /// </summary>
    public class StockDataAccess
    {
        // 既存データでは StFlag = 0 が通常の在庫として登録されている。
        // 非表示値は現行コード内に定義が見当たらないため、ここでは 1 を使用。
        // 実データベースの運用ルールが別に定義されている場合は、この値を合わせること。
        public const int ActiveFlag = 0;
        public const int HiddenFlag = 1;

        /// <summary>在庫一覧を取得する。keyword が指定された場合は商品情報を部分一致検索する。</summary>
        public List<StockDisplayData> GetStockData(string? keyword = null, bool includeHidden = false)
        {
            using var context = new SalesManagementContext();

            var query = context.TStocks
                .AsNoTracking()
                .Include(stock => stock.Pr)
                .AsQueryable();

            if (!includeHidden)
            {
                query = query.Where(stock => stock.StFlag == ActiveFlag);
            }

            var result = query
                .OrderBy(stock => stock.PrId)
                .ThenBy(stock => stock.StId)
                .Select(stock => new StockDisplayData
                {
                    StockId = stock.StId,
                    ProductId = stock.PrId,
                    ProductName = stock.Pr.PrName,
                    ProductCode = stock.Pr.PrJcode ?? string.Empty,
                    ModelNumber = stock.Pr.PrModelNumber,
                    Quantity = stock.StQuantity,
                    IsHidden = stock.StFlag != ActiveFlag
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string search = keyword.Trim();
                result = result.Where(stock =>
                    stock.ProductId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    stock.StockId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    stock.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    stock.ProductCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    stock.ModelNumber.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return result;
        }

        /// <summary>登録画面で使用する商品選択肢を取得する。</summary>
        public List<StockProductChoice> GetProductChoices()
        {
            using var context = new SalesManagementContext();

            return context.MProducts
                .AsNoTracking()
                .Where(product => product.PrFlag == 0)
                .OrderBy(product => product.PrId)
                .Select(product => new StockProductChoice
                {
                    ProductId = product.PrId,
                    ProductName = product.PrName,
                    ProductCode = product.PrJcode ?? string.Empty,
                    ModelNumber = product.PrModelNumber
                })
                .ToList();
        }

        /// <summary>在庫を登録する。非表示済みの在庫がある場合は再表示して数量を更新する。</summary>
        public bool RegisterStock(int productId, int quantity, out string message)
        {
            if (productId <= 0)
            {
                message = "登録する商品を選択してください。";
                return false;
            }

            if (quantity < 0)
            {
                message = "在庫数は0以上で入力してください。";
                return false;
            }

            using var context = new SalesManagementContext();

            var productExists = context.MProducts.Any(product =>
                product.PrId == productId && product.PrFlag == 0);

            if (!productExists)
            {
                message = "選択した商品が見つからないか、登録対象外です。";
                return false;
            }

            var existingStock = context.TStocks
                .Where(stock => stock.PrId == productId)
                .OrderBy(stock => stock.StId)
                .FirstOrDefault();

            if (existingStock != null)
            {
                if (existingStock.StFlag == ActiveFlag)
                {
                    message = "この商品はすでに在庫登録されています。在庫更新を利用してください。";
                    return false;
                }

                existingStock.StQuantity = quantity;
                existingStock.StFlag = ActiveFlag;
                context.SaveChanges();
                message = "非表示になっていた在庫を再表示し、数量を登録しました。";
                return true;
            }

            context.TStocks.Add(new TStock
            {
                PrId = productId,
                StQuantity = quantity,
                StFlag = ActiveFlag
            });

            context.SaveChanges();
            message = "在庫を登録しました。";
            return true;
        }

        /// <summary>在庫数を更新する。</summary>
        public bool UpdateStock(int stockId, int quantity, out string message)
        {
            if (stockId <= 0)
            {
                message = "更新する在庫を一覧から選択してください。";
                return false;
            }

            if (quantity < 0)
            {
                message = "在庫数は0以上で入力してください。";
                return false;
            }

            using var context = new SalesManagementContext();
            var stock = context.TStocks.FirstOrDefault(item =>
                item.StId == stockId && item.StFlag == ActiveFlag);

            if (stock == null)
            {
                message = "対象の在庫が見つかりません。一覧を更新してからやり直してください。";
                return false;
            }

            stock.StQuantity = quantity;
            context.SaveChanges();

            message = "在庫数を更新しました。";
            return true;
        }

        /// <summary>在庫を論理的に非表示にする（物理削除はしない）。</summary>
        public bool HideStock(int stockId, out string message)
        {
            if (stockId <= 0)
            {
                message = "非表示にする在庫を一覧から選択してください。";
                return false;
            }

            using var context = new SalesManagementContext();
            var stock = context.TStocks.FirstOrDefault(item =>
                item.StId == stockId && item.StFlag == ActiveFlag);

            if (stock == null)
            {
                message = "対象の在庫が見つかりません。一覧を更新してからやり直してください。";
                return false;
            }

            stock.StFlag = HiddenFlag;
            context.SaveChanges();

            message = "在庫を非表示にしました。データ自体は削除していません。";
            return true;
        }
    }

    public class StockDisplayData
    {
        public int StockId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ModelNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsHidden { get; set; }
    }

    public class StockProductChoice
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ModelNumber { get; set; } = string.Empty;

        public string DisplayName =>
            $"{ProductId} : {ProductName}（{ProductCode} / {ModelNumber}）";
    }
}
