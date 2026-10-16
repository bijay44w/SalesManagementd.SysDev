using System;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 部署マスタモデル (担当: 小林)
    /// </summary>
    public class M_Division
    {
        public string DivisionCD { get; set; } = string.Empty;
        public string DivisionName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 商品メーカマスタモデル (担当: 後藤)
    /// </summary>
    public class M_Maker
    {
        public string MakerCD { get; set; } = string.Empty;
        public string MakerName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 商品カテゴリマスタモデル (担当: 田中 / 山田)
    /// </summary>
    public class M_Category
    {
        public string CategoryCD { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? ParentCategoryCD { get; set; }
    }

    /// <summary>
    /// 仕様書の表記揺れ (M_Categoryr) 対応クラス
    /// </summary>
    public class M_Categoryr : M_Category
    {
        public M_Categoryr() { }

        public M_Categoryr(M_Category baseCategory)
        {
            if (baseCategory != null)
            {
                CategoryCD = baseCategory.CategoryCD;
                CategoryName = baseCategory.CategoryName;
                ParentCategoryCD = baseCategory.ParentCategoryCD;
            }
        }
    }

    /// <summary>
    /// 商品マスタモデル (担当: 渡辺)
    /// </summary>
    public class M_Item
    {
        public string ItemCD { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string MakerCD { get; set; } = string.Empty;
        public string CategoryCD { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    /// <summary>
    /// 表示用商品マスタモデル (担当: 渡辺)
    /// </summary>
    public class M_ItemDsp
    {
        public string ItemCD { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string MakerName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
