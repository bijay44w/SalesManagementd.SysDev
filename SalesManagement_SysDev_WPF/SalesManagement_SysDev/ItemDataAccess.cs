using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 商品データアクセス (担当: 渡辺)
    /// </summary>
    public class ItemDataAccess
    {
        private static readonly List<M_Item> ItemStore = new()
        {
            new M_Item { ItemCD = "IT001", ItemName = "4K有機ELテレビ 55型", MakerCD = "M01", CategoryCD = "C0101", Price = 180000 },
            new M_Item { ItemCD = "IT002", ItemName = "大型冷蔵庫 500L", MakerCD = "M02", CategoryCD = "C0102", Price = 220000 },
            new M_Item { ItemCD = "IT003", ItemName = "軽量ノートパソコン 14型", MakerCD = "M01", CategoryCD = "C0201", Price = 150000 }
        };

        private readonly MakerDataAccess _makerAccess = new();
        private readonly CategoryDataAccess _categoryAccess = new();

        /// <summary>
        /// 一致する商品コードの有無を確認
        /// </summary>
        public bool CheckItemCDExistence(string itemCD)
        {
            if (string.IsNullOrWhiteSpace(itemCD)) return false;
            return ItemStore.Any(i => i.ItemCD == itemCD);
        }

        /// <summary>
        /// 商品データの登録
        /// </summary>
        public bool AddItemData(M_Item data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.ItemCD)) return false;
            if (CheckItemCDExistence(data.ItemCD)) return false;

            ItemStore.Add(new M_Item
            {
                ItemCD = data.ItemCD,
                ItemName = data.ItemName ?? string.Empty,
                MakerCD = data.MakerCD ?? string.Empty,
                CategoryCD = data.CategoryCD ?? string.Empty,
                Price = data.Price
            });
            return true;
        }

        /// <summary>
        /// 商品データの更新
        /// </summary>
        public bool UpdateItemData(M_Item data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.ItemCD)) return false;

            var existing = ItemStore.FirstOrDefault(i => i.ItemCD == data.ItemCD);
            if (existing == null) return false;

            existing.ItemName = data.ItemName ?? string.Empty;
            existing.MakerCD = data.MakerCD ?? string.Empty;
            existing.CategoryCD = data.CategoryCD ?? string.Empty;
            existing.Price = data.Price;
            return true;
        }

        /// <summary>
        /// 商品データの削除
        /// </summary>
        public bool DeleteItemData(string itemCD)
        {
            if (string.IsNullOrWhiteSpace(itemCD)) return false;

            var existing = ItemStore.FirstOrDefault(i => i.ItemCD == itemCD);
            if (existing == null) return false;

            ItemStore.Remove(existing);
            return true;
        }

        /// <summary>
        /// 全商品データの取得 (表示用モデル M_ItemDsp で取得)
        /// </summary>
        public List<M_ItemDsp> GetItemData()
        {
            var makers = _makerAccess.GetMakerData().ToDictionary(m => m.MakerCD, m => m.MakerName);

            return ItemStore.Select(i => new M_ItemDsp
            {
                ItemCD = i.ItemCD,
                ItemName = i.ItemName,
                MakerName = makers.TryGetValue(i.MakerCD, out var makerName) ? makerName : i.MakerCD,
                CategoryName = _categoryAccess.GetComboboxText(i.CategoryCD),
                Price = i.Price
            }).ToList();
        }

        /// <summary>
        /// 条件部分一致する商品データの取得
        /// </summary>
        public List<M_ItemDsp> GetItemData(M_ItemDsp condition)
        {
            var all = GetItemData();
            if (condition == null) return all;

            return all.Where(i =>
                (string.IsNullOrWhiteSpace(condition.ItemCD) || i.ItemCD.Contains(condition.ItemCD)) &&
                (string.IsNullOrWhiteSpace(condition.ItemName) || i.ItemName.Contains(condition.ItemName)) &&
                (string.IsNullOrWhiteSpace(condition.MakerName) || i.MakerName.Contains(condition.MakerName)) &&
                (string.IsNullOrWhiteSpace(condition.CategoryName) || i.CategoryName.Contains(condition.CategoryName)) &&
                (condition.Price <= 0 || i.Price == condition.Price)
            ).ToList();
        }
    }
}
