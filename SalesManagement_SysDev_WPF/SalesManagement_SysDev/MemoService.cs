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
                return list?.OrderByDescending(m => m.Priority == "重要")
                            .ThenByDescending(m => m.CreatedAt)
                            .ToList() ?? new List<SharedMemoItem>();
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
                new SharedMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "【重要】システム利用時の注意点と締め処理について",
                    Content = "社員の皆様へ\n\n・日々の売上・受注データは当日中に確定処理をお願いいたします。\n・月末の棚卸期間中は在庫の移動処理にご注意ください。\n・システム操作で不明な点がある場合は、ヘッダーの「使い方」マニュアルをご確認ください。",
                    Priority = "重要",
                    AuthorName = "管理者 (システム管理課)",
                    AuthorEmpId = 1,
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new SharedMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "【周知】新機能「操作ガイド」「個人メモ」追加のお知らせ",
                    Content = "本日のシステムアップデートにより、以下の機能が利用可能になりました。\n\n1.「システムの使い方」: 各画面の操作方法や業務フローをいつでも閲覧できます。\n2.「個人メモ」: 自分専用のToDoや備忘録をログインアカウントごとに保存できます。\n3.「全体メモ」: 管理者からの重要連絡や社内周知事項を共有します。",
                    Priority = "周知",
                    AuthorName = "管理者",
                    AuthorEmpId = 1,
                    CreatedAt = DateTime.Now
                },
                new SharedMemoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "【連絡】来週の在庫棚卸スケジュールについて",
                    Content = "来週金曜日は各営業所にて定期在庫棚卸を実施します。\n棚卸完了まで商品の出庫確定は一時保留となりますので、事前準備をお願いいたします。",
                    Priority = "連絡",
                    AuthorName = "本社 営業推進部",
                    AuthorEmpId = 1,
                    CreatedAt = DateTime.Now.AddHours(-3)
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
