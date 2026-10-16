using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 部署データアクセス (担当: 小林)
    /// </summary>
    public class DivisionDataAccess
    {
        // メモリ内データストア (誰でも簡単に動作確認可能)
        private static readonly List<M_Division> DivisionStore = new()
        {
            new M_Division { DivisionCD = "01", DivisionName = "営業部" },
            new M_Division { DivisionCD = "02", DivisionName = "総務部" },
            new M_Division { DivisionCD = "03", DivisionName = "開発部" }
        };

        // スタッフマスタ参照用ダミーリスト
        private static readonly HashSet<string> UsedInStaffCDs = new() { "01", "02" };

        /// <summary>
        /// 一致する部署コードの有無を確認 (True: 一致あり, False: 一致なし)
        /// </summary>
        public bool CheckDivisionCDExistence(string divisionCD)
        {
            if (string.IsNullOrWhiteSpace(divisionCD)) return false;
            return DivisionStore.Any(d => d.DivisionCD == divisionCD);
        }

        /// <summary>
        /// 部分一致する部署コード、部署名の有無を確認 (True: 一致あり, False: 一致なし)
        /// </summary>
        public bool SelectDivisionExistenceCheck(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            return DivisionStore.Any(d => 
                (d.DivisionCD != null && d.DivisionCD.Contains(keyword)) ||
                (d.DivisionName != null && d.DivisionName.Contains(keyword)));
        }

        /// <summary>
        /// 部署データの登録 (True: 異常なし, False: 異常あり)
        /// </summary>
        public bool AddDivisionData(M_Division data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.DivisionCD)) return false;
            if (CheckDivisionCDExistence(data.DivisionCD)) return false;

            DivisionStore.Add(new M_Division
            {
                DivisionCD = data.DivisionCD,
                DivisionName = data.DivisionName ?? string.Empty
            });
            return true;
        }

        /// <summary>
        /// 部署データの更新 (True: 異常なし, False: 異常あり)
        /// </summary>
        public bool UpdateDivisionData(M_Division data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.DivisionCD)) return false;

            var existing = DivisionStore.FirstOrDefault(d => d.DivisionCD == data.DivisionCD);
            if (existing == null) return false;

            existing.DivisionName = data.DivisionName ?? string.Empty;
            return true;
        }

        /// <summary>
        /// 部署データの削除 (True: 異常なし, False: 異常あり)
        /// </summary>
        public bool DeleteDivisionData(string divisionCD)
        {
            if (string.IsNullOrWhiteSpace(divisionCD)) return false;

            var existing = DivisionStore.FirstOrDefault(d => d.DivisionCD == divisionCD);
            if (existing == null) return false;

            DivisionStore.Remove(existing);
            return true;
        }

        /// <summary>
        /// 部署CDがスタッフマスターでの利用可否 (True: 利用あり, False: 利用なし)
        /// </summary>
        public bool CheckCascadeStaff(string divisionCD)
        {
            if (string.IsNullOrWhiteSpace(divisionCD)) return false;
            return UsedInStaffCDs.Contains(divisionCD);
        }

        /// <summary>
        /// 部署データの全件取得
        /// </summary>
        public List<M_Division> GetDivisionData()
        {
            return DivisionStore
                .Select(d => new M_Division { DivisionCD = d.DivisionCD, DivisionName = d.DivisionName })
                .ToList();
        }

        /// <summary>
        /// 条件部分一致部署データの取得
        /// </summary>
        public List<M_Division> GetDivisionData(M_Division condition)
        {
            if (condition == null) return GetDivisionData();

            return DivisionStore
                .Where(d => 
                    (string.IsNullOrWhiteSpace(condition.DivisionCD) || d.DivisionCD.Contains(condition.DivisionCD)) &&
                    (string.IsNullOrWhiteSpace(condition.DivisionName) || d.DivisionName.Contains(condition.DivisionName)))
                .Select(d => new M_Division { DivisionCD = d.DivisionCD, DivisionName = d.DivisionName })
                .ToList();
        }

        /// <summary>
        /// 表示用部署データの取得
        /// </summary>
        public List<M_Division> GetDivisionDspData()
        {
            return GetDivisionData();
        }
    }
}
