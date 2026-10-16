using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 商品カテゴリデータアクセス (担当: 田中 / 山田)
    /// </summary>
    public class CategoryDataAccess
    {
        private static readonly List<M_Category> CategoryStore = new()
        {
            new M_Category { CategoryCD = "C01", CategoryName = "家電", ParentCategoryCD = null },
            new M_Category { CategoryCD = "C0101", CategoryName = "テレビ", ParentCategoryCD = "C01" },
            new M_Category { CategoryCD = "C0102", CategoryName = "冷蔵庫", ParentCategoryCD = "C01" },
            new M_Category { CategoryCD = "C02", CategoryName = "PC・周辺機器", ParentCategoryCD = null },
            new M_Category { CategoryCD = "C0201", CategoryName = "ノートPC", ParentCategoryCD = "C02" }
        };

        private static readonly HashSet<string> UsedInItemCategories = new() { "C0101", "C0201" };

        /// <summary>
        /// 一致する商品カテゴリコードの有無を確認
        /// </summary>
        public bool CheckCategoryCDExistence(string categoryCD)
        {
            if (string.IsNullOrWhiteSpace(categoryCD)) return false;
            return CategoryStore.Any(c => c.CategoryCD == categoryCD);
        }

        /// <summary>
        /// 部分一致する商品カテゴリコード、商品カテゴリ名の有無を確認
        /// </summary>
        public bool SelectCategoryExistenceCheck(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            return CategoryStore.Any(c =>
                (c.CategoryCD != null && c.CategoryCD.Contains(keyword)) ||
                (c.CategoryName != null && c.CategoryName.Contains(keyword)));
        }

        /// <summary>
        /// 商品カテゴリデータの登録
        /// </summary>
        public bool AddCategoryData(M_Category data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.CategoryCD)) return false;
            if (CheckCategoryCDExistence(data.CategoryCD)) return false;

            CategoryStore.Add(new M_Category
            {
                CategoryCD = data.CategoryCD,
                CategoryName = data.CategoryName ?? string.Empty,
                ParentCategoryCD = data.ParentCategoryCD
            });
            return true;
        }

        /// <summary>
        /// 商品カテゴリデータの更新
        /// </summary>
        public bool UpdateCategoryData(M_Category data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.CategoryCD)) return false;

            var existing = CategoryStore.FirstOrDefault(c => c.CategoryCD == data.CategoryCD);
            if (existing == null) return false;

            existing.CategoryName = data.CategoryName ?? string.Empty;
            existing.ParentCategoryCD = data.ParentCategoryCD;
            return true;
        }

        /// <summary>
        /// 商品カテゴリデータの削除
        /// </summary>
        public bool DeleteCategoryData(string categoryCD)
        {
            if (string.IsNullOrWhiteSpace(categoryCD)) return false;

            var existing = CategoryStore.FirstOrDefault(c => c.CategoryCD == categoryCD);
            if (existing == null) return false;

            CategoryStore.Remove(existing);
            return true;
        }

        /// <summary>
        /// 商品カテゴリCDが商品マスターでの利用可否 (True: 利用あり, False: 利用なし)
        /// </summary>
        public bool CheckCascadeItem(string categoryCD)
        {
            if (string.IsNullOrWhiteSpace(categoryCD)) return false;
            return UsedInItemCategories.Contains(categoryCD);
        }

        /// <summary>
        /// 商品カテゴリCDが親カテゴリとしての利用可否 (True: 利用あり, False: 利用なし)
        /// </summary>
        public bool CheckCascadeParentCategory(string categoryCD)
        {
            if (string.IsNullOrWhiteSpace(categoryCD)) return false;
            return CategoryStore.Any(c => c.ParentCategoryCD == categoryCD);
        }

        /// <summary>
        /// 商品カテゴリデータの取得
        /// </summary>
        public List<M_Categoryr> GetCategoryData()
        {
            return CategoryStore
                .Select(c => new M_Categoryr(c))
                .ToList();
        }

        /// <summary>
        /// 条件一致商品カテゴリデータの取得
        /// </summary>
        public List<M_Categoryr> GetCategoryData(M_Categoryr condition)
        {
            if (condition == null) return GetCategoryData();

            return CategoryStore
                .Where(c =>
                    (string.IsNullOrWhiteSpace(condition.CategoryCD) || c.CategoryCD.Contains(condition.CategoryCD)) &&
                    (string.IsNullOrWhiteSpace(condition.CategoryName) || c.CategoryName.Contains(condition.CategoryName)) &&
                    (string.IsNullOrWhiteSpace(condition.ParentCategoryCD) || c.ParentCategoryCD == condition.ParentCategoryCD))
                .Select(c => new M_Categoryr(c))
                .ToList();
        }

        /// <summary>
        /// 表示用商品親カテゴリデータの取得 (ParentCategoryCD が null または空のもの)
        /// </summary>
        public List<M_Categoryr> GetParentCategoryDspData()
        {
            return CategoryStore
                .Where(c => string.IsNullOrEmpty(c.ParentCategoryCD))
                .Select(c => new M_Categoryr(c))
                .ToList();
        }

        /// <summary>
        /// 表示用商品カテゴリデータの取得 (親カテゴリCDに属する子カテゴリ一覧)
        /// </summary>
        public List<M_Categoryr> GetCategoryDspData(string categoryCD)
        {
            if (string.IsNullOrWhiteSpace(categoryCD)) return GetCategoryData();

            return CategoryStore
                .Where(c => c.ParentCategoryCD == categoryCD || c.CategoryCD == categoryCD)
                .Select(c => new M_Categoryr(c))
                .ToList();
        }

        /// <summary>
        /// 商品カテゴリCDからカテゴリ名の取得
        /// </summary>
        public string GetComboboxText(string categoryCD)
        {
            if (string.IsNullOrWhiteSpace(categoryCD)) return string.Empty;

            var category = CategoryStore.FirstOrDefault(c => c.CategoryCD == categoryCD);
            return category?.CategoryName ?? string.Empty;
        }
    }
}
