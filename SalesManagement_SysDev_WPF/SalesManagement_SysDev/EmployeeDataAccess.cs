using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 社員データアクセス。画面処理とデータベース処理を分離する。
    /// </summary>
    public class EmployeeDataAccess
    {
        // F_login.xaml.cs では EmFlag != 0 の社員をログイン対象外としている。
        // その既存仕様に合わせ、0を有効、1を非表示として扱う。
        public const int ActiveFlag = 0;
        public const int HiddenFlag = 1;

        public List<EmployeeDisplayData> GetEmployeeData(string? keyword = null, bool includeHidden = false)
        {
            using var context = new SalesManagementContext();

            var employees = context.MEmployees
                .AsNoTracking()
                .Include(employee => employee.So)
                .Include(employee => employee.Po)
                .ToList();

            var result = employees
                .Where(employee => includeHidden || employee.EmFlag == ActiveFlag)
                .Select(employee => new EmployeeDisplayData
                {
                    EmployeeId = employee.EmId,
                    EmployeeName = employee.EmName,
                    SalesOfficeId = employee.SoId,
                    SalesOfficeName = employee.So.SoName,
                    PositionId = employee.PoId,
                    PositionName = employee.Po.PoName,
                    HireDate = employee.EmHiredate,
                    Phone = employee.EmPhone,
                    IsHidden = employee.EmFlag != ActiveFlag
                })
                .OrderBy(employee => employee.EmployeeId)
                .ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string search = keyword.Trim();
                result = result.Where(employee =>
                    employee.EmployeeId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    employee.EmployeeName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    employee.SalesOfficeName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    employee.PositionName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    employee.Phone.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return result;
        }

        public List<EmployeeOfficeChoice> GetSalesOfficeChoices()
        {
            using var context = new SalesManagementContext();

            return context.MSalesOffices
                .AsNoTracking()
                .Where(office => office.SoFlag == ActiveFlag)
                .OrderBy(office => office.SoId)
                .Select(office => new EmployeeOfficeChoice
                {
                    SalesOfficeId = office.SoId,
                    SalesOfficeName = office.SoName
                })
                .ToList();
        }

        public List<EmployeePositionChoice> GetPositionChoices()
        {
            using var context = new SalesManagementContext();

            return context.MPositions
                .AsNoTracking()
                .Where(position => position.PoFlag == ActiveFlag)
                .OrderBy(position => position.PoId)
                .Select(position => new EmployeePositionChoice
                {
                    PositionId = position.PoId,
                    PositionName = position.PoName
                })
                .ToList();
        }

        public bool AddEmployeeData(MEmployee data, out string message)
        {
            if (data == null)
            {
                message = "登録する社員情報がありません。";
                return false;
            }

            if (data.EmId <= 0)
            {
                message = "社員番号は1以上の整数で入力してください。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.EmName) || data.EmName.Trim().Length > 50)
            {
                message = "社員名を入力してください（50文字以内）。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.EmPassword) ||
                data.EmPassword.Length < 4 || data.EmPassword.Length > 10)
            {
                message = "パスワードは4～10文字で入力してください。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.EmPhone) || data.EmPhone.Length > 13)
            {
                message = "電話番号を入力してください（13文字以内）。";
                return false;
            }

            using var context = new SalesManagementContext();

            if (context.MEmployees.Any(employee => employee.EmId == data.EmId))
            {
                message = "その社員番号はすでに登録されています。";
                return false;
            }

            if (!context.MSalesOffices.Any(office =>
                    office.SoId == data.SoId && office.SoFlag == ActiveFlag) ||
                !context.MPositions.Any(position =>
                    position.PoId == data.PoId && position.PoFlag == ActiveFlag))
            {
                message = "選択した営業所または役職が無効です。";
                return false;
            }

            data.EmName = data.EmName.Trim();
            data.EmPhone = data.EmPhone.Trim();
            data.EmFlag = ActiveFlag;
            data.EmHidden = null;

            context.MEmployees.Add(data);
            context.SaveChanges();

            message = "社員を登録しました。";
            return true;
        }

        public bool UpdateEmployeeData(MEmployee data, out string message)
        {
            if (data == null || data.EmId <= 0)
            {
                message = "更新する社員を選択してください。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.EmName) || data.EmName.Trim().Length > 50)
            {
                message = "社員名を入力してください（50文字以内）。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.EmPassword) ||
                data.EmPassword.Length < 4 || data.EmPassword.Length > 10)
            {
                message = "パスワードは4～10文字で入力してください。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.EmPhone) || data.EmPhone.Length > 13)
            {
                message = "電話番号を入力してください（13文字以内）。";
                return false;
            }

            using var context = new SalesManagementContext();
            var employee = context.MEmployees.FirstOrDefault(item =>
                item.EmId == data.EmId && item.EmFlag == ActiveFlag);

            if (employee == null)
            {
                message = "対象の社員が見つかりません。一覧を更新してからやり直してください。";
                return false;
            }

            if (!context.MSalesOffices.Any(office =>
                    office.SoId == data.SoId && office.SoFlag == ActiveFlag) ||
                !context.MPositions.Any(position =>
                    position.PoId == data.PoId && position.PoFlag == ActiveFlag))
            {
                message = "選択した営業所または役職が無効です。";
                return false;
            }

            employee.EmName = data.EmName.Trim();
            employee.SoId = data.SoId;
            employee.PoId = data.PoId;
            employee.EmHiredate = data.EmHiredate.Date;
            employee.EmPassword = data.EmPassword;
            employee.EmPhone = data.EmPhone.Trim();

            context.SaveChanges();
            message = "社員情報を更新しました。";
            return true;
        }

        public bool HideEmployeeData(int employeeId, out string message)
        {
            if (employeeId <= 0)
            {
                message = "非表示にする社員を一覧から選択してください。";
                return false;
            }

            using var context = new SalesManagementContext();
            var employee = context.MEmployees.FirstOrDefault(item =>
                item.EmId == employeeId && item.EmFlag == ActiveFlag);

            if (employee == null)
            {
                message = "対象の社員が見つかりません。一覧を更新してからやり直してください。";
                return false;
            }

            // 物理削除はせず、既存ログイン処理が無効と判定するフラグ値に変更する。
            employee.EmFlag = HiddenFlag;
            context.SaveChanges();

            message = "社員を非表示にしました。社員データ自体は削除していません。";
            return true;
        }
    }

    public class EmployeeDisplayData
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int SalesOfficeId { get; set; }
        public string SalesOfficeName { get; set; } = string.Empty;
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public string Phone { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
    }

    public class EmployeeOfficeChoice
    {
        public int SalesOfficeId { get; set; }
        public string SalesOfficeName { get; set; } = string.Empty;
        public string DisplayName => $"{SalesOfficeId} : {SalesOfficeName}";
    }

    public class EmployeePositionChoice
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public string DisplayName => $"{PositionId} : {PositionName}";
    }
}
