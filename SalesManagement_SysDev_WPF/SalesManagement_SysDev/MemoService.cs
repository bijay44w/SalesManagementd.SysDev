using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SalesManagement_SysDev
{
    /// <summary>
    /// 全体メモ (管理者連絡事項・社内共有)
    /// </summary>
    public class SharedMemoItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Priority { get; set; } = "通常"; // 重要, 連絡, 周知, 通常

        /// <summary>
        /// アクセス区分:
        /// "General" = 一般的な業務連絡（ログイン後そのまま閲覧可能）
        /// "AdminOnly" = 管理者向けの重要な連絡（管理者のみ閲覧可能）
        /// "Confidential" = 機密情報を含む連絡（追加のパスコード認証が必要）
        /// </summary>
        public string AccessLevel { get; set; } = "General";

        /// <summary>
        /// 機密情報閲覧用の追加パスコード（未設定時は "9999"）
        /// </summary>
        public string? Passcode { get; set; } = "9999";

        public string AuthorName { get; set; } = "管理者";
        public int AuthorEmpId { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // 表示用プロパティ
        public string DisplayDate => CreatedAt.ToString("yyyy/MM/dd HH:mm");
        public string Summary => Content.Length > 60 ? Content.Substring(0, 60) + "..." : Content;

        public string PriorityBackground => Priority switch
        {
            "重要" => "#D32F2F", // 赤
            "連絡" => "#1976D2", // 青
            "周知" => "#388E3C", // 緑
            _ => "#616161"        // グレー
        };

        public string AccessLevelName => AccessLevel switch
        {
            "AdminOnly" => "管理者限定",
            "Confidential" => "🔒 機密情報",
            _ => "一般連絡"
        };

        public string AccessLevelBackground => AccessLevel switch
        {
            "AdminOnly" => "#D97706",    // オレンジ
            "Confidential" => "#7C3AED", // 紫
            _ => "#2563EB"               // 青
        };

        public bool IsConfidential => AccessLevel == "Confidential";
        public bool IsAdminOnly => AccessLevel == "AdminOnly";
        public bool IsGeneral => AccessLevel == "General";
    }

    /// <summary>
    /// 個人メモ (社員ごとの非公開メモ)
    /// </summary>
    public class PersonalMemoItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int EmployeeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 表示用プロパティ
        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "（無題のメモ）" : Title;
        public string DisplayDate => UpdatedAt.ToString("yyyy/MM/dd HH:mm");
    }

    /// <summary>
    /// メモ管理サービス（全体メモ・個人メモの永続化と取得）
    /// </summary>
    public class MemoService
    {
        private readonly string _dataDir;
        private readonly string _sharedMemoFilePath;
        private readonly string _personalMemoFilePath;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public MemoService()
        {
            _dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            _sharedMemoFilePath = Path.Combine(_dataDir, "shared_memos.json");
            _personalMemoFilePath = Path.Combine(_dataDir, "personal_memos.json");
        }

        #region 全体メモ (Shared Memos)

        /// <summary>
        /// 全体メモ一覧を取得（作成日時降順）
        /// </summary>
        public List<SharedMemoItem> GetSharedMemos()
        {
            try
            {
                if (!File.Exists(_sharedMemoFilePath))
                {
                    var initialMemos = CreateInitialSharedMemos();
                    SaveSharedMemos(initialMemos);
                    return initialMemos;
                }

                string json = File.ReadAllText(_sharedMemoFilePath);
                var list = JsonSerializer.Deserialize<List<SharedMemoItem>>(json, _jsonOptions);
                
                // 旧形式データの場合や空の場合の補正
                if (list == null || list.Count == 0 || !list.Any(m => m.AccessLevel == "AdminOnly" || m.AccessLevel == "Confidential"))
                {
                    var initialMemos = CreateInitialSharedMemos();
                    SaveSharedMemos(initialMemos);
                    return initialMemos;
                }

                return list.OrderByDescending(m => m.Priority == "重要")
                           .ThenByDescending(m => m.CreatedAt)
                           .ToList();
            }
            catch
            {
                return CreateInitialSharedMemos();
            }
        }

        /// <summary>
        /// 全体メモの保存
        /// </summary>
        public void SaveSharedMemos(List<SharedMemoItem> memos)
        {
            try
            {
                string json = JsonSerializer.Serialize(memos, _jsonOptions);
                File.WriteAllText(_sharedMemoFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save shared memos: {ex.Message}");
            }
        }

        /// <summary>
        /// 全体メモを追加または更新
        /// </summary>
        public void UpsertSharedMemo(SharedMemoItem item)
        {
            var memos = GetSharedMemos();
            var existing = memos.FirstOrDefault(m => m.Id == item.Id);
            if (existing != null)
            {
                existing.Title = item.Title;
                existing.Content = item.Content;
                existing.Priority = item.Priority;
                existing.AccessLevel = item.AccessLevel;
                existing.Passcode = item.Passcode;
                existing.UpdatedAt = DateTime.Now;
            }
            else
            {
                memos.Insert(0, item);
            }
            SaveSharedMemos(memos);
        }

        /// <summary>
        /// 全体メモを削除
        /// </summary>
        public void DeleteSharedMemo(string id)
        {
            var memos = GetSharedMemos();
            memos.RemoveAll(m => m.Id == id);
            SaveSharedMemos(memos);
        }

        private List<SharedMemoItem> CreateInitialSharedMemos()
        {
            return new List<SharedMemoItem>
            {
                // ① 一般的な業務連絡 (ログイン後そのまま閲覧可能)
                new SharedMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "【一般連絡】月次棚卸スケジュールと通常業務の注意点",
                    Content = "社員の皆様へ\n\n・来週金曜日は各営業所にて定期在庫棚卸を実施します。\n・当日の出庫・出荷処理は午前中までに完了するよう手配をお願いします。\n・操作方法で不明な点がある場合は、ヘッダーの「使い方」マニュアルをご確認ください。",
                    Priority = "連絡",
                    AccessLevel = "General",
                    AuthorName = "本社 営業推進部",
                    AuthorEmpId = 1,
                    CreatedAt = DateTime.Now.AddHours(-1)
                },
                new SharedMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "【周知】新機能「操作ガイド」「個人メモ」利用開始のご案内",
                    Content = "本日より、メインメニューに以下の機能が追加されました。\n\n1.「システムの使い方」: 各業務フローや操作方法をいつでも閲覧できます。\n2.「個人メモ」: ログイン社員専用のTo-Doや備忘録を安全に保存できます。\n3.「社内連絡」: 全社周知や重要連絡を共有します。",
                    Priority = "周知",
                    AccessLevel = "General",
                    AuthorName = "システム管理者",
                    AuthorEmpId = 1,
                    CreatedAt = DateTime.Now.AddDays(-1)
                },

                // ② 管理者向けの重要な連絡 (管理者のみ閲覧可能)
                new SharedMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "【管理者限定】第3四半期 役職者会議および人員配置計画について",
                    Content = "管理者・役職者各位\n\n・来期の営業所別人員配置および各拠点の目標進捗確認について、来週火曜15:00より役職者会議を実施します。\n・各拠点の売上実績・在庫回転率の集計データを事前にご確認ください。\n・会議用アジェンダは追って共有します。",
                    Priority = "重要",
                    AccessLevel = "AdminOnly",
                    AuthorName = "営業本部長",
                    AuthorEmpId = 1,
                    CreatedAt = DateTime.Now.AddHours(-3)
                },

                // ③ 機密情報を含む連絡 (追加のアクセス制限・パスコード認証が必要)
                new SharedMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "【機密情報】新規大型取引先との提携契約および価格改定案（要認証）",
                    Content = "【社外秘・関係者限り】\n\n・大手量販店チェーンとの新規卸契約締結に関する合意覚書です。\n・新製品の卸価格および想定利益率が記載されています。\n・正式プレスリリースまで他部門や外部への口外を固く禁止いたします。\n\n[初期設定パスコード: 9999]",
                    Priority = "重要",
                    AccessLevel = "Confidential",
                    Passcode = "9999",
                    AuthorName = "代表取締役 / 経営企画室",
                    AuthorEmpId = 1,
                    CreatedAt = DateTime.Now.AddHours(-5)
                }
            };
        }

        #endregion

        #region 個人メモ (Personal Memos)

        /// <summary>
        /// 全社員の個人メモ一覧を取得
        /// </summary>
        private List<PersonalMemoItem> GetAllPersonalMemos()
        {
            try
            {
                if (!File.Exists(_personalMemoFilePath))
                {
                    return new List<PersonalMemoItem>();
                }

                string json = File.ReadAllText(_personalMemoFilePath);
                var list = JsonSerializer.Deserialize<List<PersonalMemoItem>>(json, _jsonOptions);
                return list ?? new List<PersonalMemoItem>();
            }
            catch
            {
                return new List<PersonalMemoItem>();
            }
        }

        /// <summary>
        /// 特定社員の個人メモ一覧を取得
        /// </summary>
        public List<PersonalMemoItem> GetPersonalMemos(int employeeId)
        {
            var all = GetAllPersonalMemos();
            var userMemos = all.Where(m => m.EmployeeId == employeeId)
                               .OrderByDescending(m => m.UpdatedAt)
                               .ToList();

            if (userMemos.Count == 0)
            {
                // 初期メモを1件作成
                var initialMemo = new PersonalMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    EmployeeId = employeeId,
                    Title = "業務メモ・ToDo備忘録",
                    Content = "・ここはログイン社員様専用の個人メモ帳です。\n・自分だけの備忘録や日々のタスク、連絡事項の下書きなどを自由に記録できます。\n・右上の「保存」ボタンで内容が保存されます。",
                    UpdatedAt = DateTime.Now
                };
                userMemos.Add(initialMemo);
                SavePersonalMemo(initialMemo);
            }

            return userMemos;
        }

        /// <summary>
        /// 個人メモを保存（新規または更新）
        /// </summary>
        public void SavePersonalMemo(PersonalMemoItem item)
        {
            var all = GetAllPersonalMemos();
            var existing = all.FirstOrDefault(m => m.Id == item.Id);
            if (existing != null)
            {
                existing.Title = item.Title;
                existing.Content = item.Content;
                existing.UpdatedAt = DateTime.Now;
            }
            else
            {
                item.UpdatedAt = DateTime.Now;
                all.Add(item);
            }

            SaveAllPersonalMemos(all);
        }

        /// <summary>
        /// 個人メモを削除
        /// </summary>
        public void DeletePersonalMemo(string id)
        {
            var all = GetAllPersonalMemos();
            all.RemoveAll(m => m.Id == id);
            SaveAllPersonalMemos(all);
        }

        private void SaveAllPersonalMemos(List<PersonalMemoItem> memos)
        {
            try
            {
                string json = JsonSerializer.Serialize(memos, _jsonOptions);
                File.WriteAllText(_personalMemoFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save personal memos: {ex.Message}");
            }
        }

        #endregion
    }
}
