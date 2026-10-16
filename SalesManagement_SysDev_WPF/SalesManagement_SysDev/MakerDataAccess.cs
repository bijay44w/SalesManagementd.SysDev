using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 商品メーカデータアクセス (担当: 後藤)
    /// </summary>
    public class MakerDataAccess
    {
        private static readonly List<M_Maker> MakerStore = new()
        {
            new M_Maker { MakerCD = "M01", MakerName = "ソニー" },
            new M_Maker { MakerCD = "M02", MakerName = "パナソニック" },
            new M_Maker { MakerCD = "M03", MakerName = "シャープ" }
        };

        private static readonly HashSet<string> UsedInItemCDs = new() { "M01", "M02" };

        /// <summary>
        /// 一致する商品メーカコードの有無を確認
        /// </summary>
        public bool CheckMakerCDExistence(string makerCD)
        {
            if (string.IsNullOrWhiteSpace(makerCD)) return false;
            return MakerStore.Any(m => m.MakerCD == makerCD);
        }

        /// <summary>
        /// 部分一致する商品メーカコード、商品メーカ名の有無を確認
        /// </summary>
        public bool SelectMakerExistenceCheck(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            return MakerStore.Any(m =>
                (m.MakerCD != null && m.MakerCD.Contains(keyword)) ||
                (m.MakerName != null && m.MakerName.Contains(keyword)));
        }

        /// <summary>
        /// 商品メーカデータの登録
        /// </summary>
        public bool AddMakerData(M_Maker data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.MakerCD)) return false;
            if (CheckMakerCDExistence(data.MakerCD)) return false;

            MakerStore.Add(new M_Maker
            {
                MakerCD = data.MakerCD,
                MakerName = data.MakerName ?? string.Empty
            });
            return true;
        }

        /// <summary>
        /// 商品メーカデータの更新
        /// </summary>
        public bool UpdateMakerData(M_Maker data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.MakerCD)) return false;

            var existing = MakerStore.FirstOrDefault(m => m.MakerCD == data.MakerCD);
            if (existing == null) return false;

            existing.MakerName = data.MakerName ?? string.Empty;
            return true;
        }

        /// <summary>
        /// 商品メーカデータの削除
        /// </summary>
        public bool DeleteMakerData(string makerCD)
        {
            if (string.IsNullOrWhiteSpace(makerCD)) return false;

            var existing = MakerStore.FirstOrDefault(m => m.MakerCD == makerCD);
            if (existing == null) return false;

            MakerStore.Remove(existing);
            return true;
        }

        /// <summary>
        /// 商品メーカCDが商品マスタでの利用可否 (True: 利用あり, False: 利用なし)
        /// </summary>
        public bool CheckCascadeItem(string makerCD)
        {
            if (string.IsNullOrWhiteSpace(makerCD)) return false;
            return UsedInItemCDs.Contains(makerCD);
        }

        /// <summary>
        /// 商品メーカデータの取得
        /// </summary>
        public List<M_Maker> GetMakerData()
        {
            return MakerStore
                .Select(m => new M_Maker { MakerCD = m.MakerCD, MakerName = m.MakerName })
                .ToList();
        }

        /// <summary>
        /// 条件部分一致商品メーカデータ
        /// </summary>
        public List<M_Maker> GetMakerData(M_Maker condition)
        {
            if (condition == null) return GetMakerData();

            return MakerStore
                .Where(m =>
                    (string.IsNullOrWhiteSpace(condition.MakerCD) || m.MakerCD.Contains(condition.MakerCD)) &&
                    (string.IsNullOrWhiteSpace(condition.MakerName) || m.MakerName.Contains(condition.MakerName)))
                .Select(m => new M_Maker { MakerCD = m.MakerCD, MakerName = m.MakerName })
                .ToList();
        }

        /// <summary>
        /// 表示用商品メーカデータの取得
        /// </summary>
        public List<M_Maker> GetMakerDspData()
        {
            return GetMakerData();
        }
    }
}
