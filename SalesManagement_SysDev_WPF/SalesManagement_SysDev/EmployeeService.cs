using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 社員一覧・検索結果の画面表示用モデル
    /// </summary>
    public class EmployeeDisplayModel
    {
        public int EmId { get; set; }
        public string EmName { get; set; } = string.Empty;
        public int SoId { get; set; }
        public string SoName { get; set; } = string.Empty;
        public int PoId { get; set; }
        public string PoName { get; set; } = string.Empty;
        public DateTime EmHiredate { get; set; }
        public string HiredateFormatted => EmHiredate.ToString("yyyy/MM/dd");
        public string EmPhone { get; set; } = string.Empty;
        public string EmPassword { get; set; } = string.Empty;
        public int EmFlag { get; set; }
        public string FlagFormatted => EmFlag switch
        {
            0 => "表示中",
            2 => "非表示",
            _ => $"その他({EmFlag})"
        };
        public string? EmHidden { get; set; }
    }

    /// <summary>
    /// 6. 社員管理サービス
    /// 6.1 社員登録機能
    /// 6.2 社員更新機能
    /// 6.3 社員一覧表示機能
    /// 6.4 社員検索機能
    /// 6.5 社員非表示機能 (フラグ 0 -> 2 更新)
    /// </summary>
    public class EmployeeService
    {
        // =====================================================================
        // 6.1 社員登録機能
        // =====================================================================
        public (bool Success, string Message, MEmployee? Employee) RegisterEmployee(MEmployee employee)
        {
            if (employee == null)
            {
                return (false, "社員情報が指定されていません。", null);
            }

            if (string.IsNullOrWhiteSpace(employee.EmName))
            {
                return (false, "社員名を入力してください。", null);
            }
            if (employee.EmName.Length > 50)
            {
                return (false, "社員名は50文字以内で入力してください。", null);
            }

            if (employee.SoId <= 0)
            {
                return (false, "所属営業所を選択してください。", null);
            }

            if (employee.PoId <= 0)
            {
                return (false, "役職を選択してください。", null);
            }

            if (string.IsNullOrWhiteSpace(employee.EmPhone))
            {
                return (false, "電話番号を入力してください。", null);
            }
            if (employee.EmPhone.Length > 15)
            {
                return (false, "電話番号は15文字以内で入力してください。", null);
            }

            if (string.IsNullOrWhiteSpace(employee.EmPassword))
            {
                employee.EmPassword = "1234";
            }
            else if (employee.EmPassword.Length < 4 || employee.EmPassword.Length > 10)
            {
                return (false, "パスワードは4文字以上10文字以内で入力してください。", null);
            }

            if (employee.EmHiredate == default)
            {
                employee.EmHiredate = DateTime.Today;
            }

            try
            {
                using var context = new SalesManagementContext();

                // ID自動採番 (0 または 未指定時: 1, 2, 3... の連番)
                if (employee.EmId <= 0)
                {
                    int maxId = context.MEmployees.Select(e => (int?)e.EmId).Max() ?? 0;
                    employee.EmId = maxId + 1;
                }
                else
                {
                    if (context.MEmployees.Any(e => e.EmId == employee.EmId))
                    {
                        return (false, $"社員ID: {employee.EmId} は既に登録されています。", null);
                    }
                }

                employee.EmFlag = 0;
                employee.EmHidden = null;

                context.MEmployees.Add(employee);
                context.SaveChanges();

                return (true, $"社員ID: {employee.EmId}（{employee.EmName} 様）を登録しました。", employee);
            }
            catch (Exception ex)
            {
                return (false, $"社員登録中にエラーが発生しました: {ex.Message}", null);
            }
        }

        // =====================================================================
        // 6.2 社員更新機能
        // =====================================================================
        public (bool Success, string Message) UpdateEmployee(MEmployee employee, bool updatePassword = false)
        {
            if (employee == null || employee.EmId <= 0)
            {
                return (false, "更新対象の社員IDが正しく指定されていません。");
            }

            if (string.IsNullOrWhiteSpace(employee.EmName))
            {
                return (false, "社員名を入力してください。");
            }
            if (employee.EmName.Length > 50)
            {
                return (false, "社員名は50文字以内で入力してください。");
            }

            if (employee.SoId <= 0)
            {
                return (false, "所属営業所を選択してください。");
            }

            if (employee.PoId <= 0)
            {
                return (false, "役職を選択してください。");
            }

            if (string.IsNullOrWhiteSpace(employee.EmPhone))
            {
                return (false, "電話番号を入力してください。");
            }

            if (updatePassword && !string.IsNullOrWhiteSpace(employee.EmPassword))
            {
                if (employee.EmPassword.Length < 4 || employee.EmPassword.Length > 10)
                {
                    return (false, "パスワードは4文字以上10文字以内で入力してください。");
                }
            }

            try
            {
                using var context = new SalesManagementContext();
                var target = context.MEmployees.FirstOrDefault(e => e.EmId == employee.EmId);
                if (target == null)
                {
                    return (false, $"社員ID: {employee.EmId} の社員情報が見つかりませんでした。");
                }

                target.EmName = employee.EmName.Trim();
                target.SoId = employee.SoId;
                target.PoId = employee.PoId;
                target.EmPhone = employee.EmPhone.Trim();
                if (employee.EmHiredate != default)
                {
                    target.EmHiredate = employee.EmHiredate;
                }

                if (updatePassword && !string.IsNullOrWhiteSpace(employee.EmPassword))
                {
                    target.EmPassword = employee.EmPassword;
                }

                context.SaveChanges();
                return (true, $"社員ID: {employee.EmId}（{target.EmName} 様）の情報を更新しました。");
            }
            catch (Exception ex)
            {
                return (false, $"社員情報更新中にエラーが発生しました: {ex.Message}");
            }
        }

        // =====================================================================
        // 6.3 社員一覧取得機能
        // =====================================================================
        public List<EmployeeDisplayModel> GetAllEmployees(bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.MEmployees
                    .AsNoTracking()
                    .Include(e => e.So)
                    .Include(e => e.Po)
                    .AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(e => e.EmFlag != 2);
                }

                return query
                    .OrderBy(e => e.EmId)
                    .AsEnumerable()
                    .Select(e => ToDisplayModel(e))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeDisplayModel>();
            }
        }

        // =====================================================================
        // 6.4 社員検索機能
        // =====================================================================
        public List<EmployeeDisplayModel> SearchEmployees(
            int? emId = null,
            string? name = null,
            int? soId = null,
            int? poId = null,
            string? phone = null,
            bool includeHidden = false)
        {
            try
            {
                using var context = new SalesManagementContext();
                var query = context.MEmployees
                    .AsNoTracking()
                    .Include(e => e.So)
                    .Include(e => e.Po)
                    .AsQueryable();

                if (emId.HasValue && emId.Value > 0)
                {
                    query = query.Where(e => e.EmId == emId.Value);
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    string trimmedName = name.Trim();
                    query = query.Where(e => e.EmName.Contains(trimmedName));
                }

                if (soId.HasValue && soId.Value > 0)
                {
                    query = query.Where(e => e.SoId == soId.Value);
                }

                if (poId.HasValue && poId.Value > 0)
                {
                    query = query.Where(e => e.PoId == poId.Value);
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    string trimmedPhone = phone.Trim();
                    query = query.Where(e => e.EmPhone.Contains(trimmedPhone));
                }

                if (!includeHidden)
                {
                    query = query.Where(e => e.EmFlag != 2);
                }

                return query
                    .OrderBy(e => e.EmId)
                    .AsEnumerable()
                    .Select(e => ToDisplayModel(e))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeDisplayModel>();
            }
        }

        // =====================================================================
        // 6.5 社員非表示機能 (フラグ 0 -> 2 更新)
        // =====================================================================
        public (bool Success, string Message) HideEmployee(int emId, string reason)
        {
            if (emId <= 0)
            {
                return (false, "対象の社員IDが正しく指定されていません。");
            }

            try
            {
                using var context = new SalesManagementContext();
                var emp = context.MEmployees.FirstOrDefault(e => e.EmId == emId);
                if (emp == null)
                {
                    return (false, $"社員ID: {emId} の社員情報が見つかりませんでした。");
                }

                emp.EmFlag = 2;
                emp.EmHidden = string.IsNullOrWhiteSpace(reason) ? "管理者による非表示設定" : reason.Trim();

                context.SaveChanges();
                return (true, $"社員ID: {emId}（{emp.EmName} 様）を非表示に設定しました。");
            }
            catch (Exception ex)
            {
                return (false, $"社員非表示処理中にエラーが発生しました: {ex.Message}");
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

        public List<MPosition> GetPositions()
        {
            try
            {
                using var context = new SalesManagementContext();
                return context.MPositions
                    .AsNoTracking()
                    .Where(p => p.PoFlag != 2)
                    .OrderBy(p => p.PoId)
                    .ToList();
            }
            catch
            {
                return new List<MPosition>();
            }
        }

        public EmployeeDisplayModel ToDisplayModel(MEmployee e)
        {
            return new EmployeeDisplayModel
            {
                EmId = e.EmId,
                EmName = e.EmName ?? string.Empty,
                SoId = e.SoId,
                SoName = e.So?.SoName ?? $"営業所 #{e.SoId}",
                PoId = e.PoId,
                PoName = e.Po?.PoName ?? $"役職 #{e.PoId}",
                EmHiredate = e.EmHiredate,
                EmPhone = e.EmPhone ?? string.Empty,
                EmPassword = e.EmPassword ?? string.Empty,
                EmFlag = e.EmFlag,
                EmHidden = e.EmHidden
            };
        }
    }
}
