// 引用 Dapper 和 SQL Server 客戶端
using Dapper;
using Microsoft.Data.SqlClient;
using System.IO;

namespace HWDPortal.Services
{
    // 用於表示公告資料的公開類別，以便在專案中其他地方使用
    public class BulletinItem
    {
        public required string Title { get; set; }
        public DateTime LastWriteTime { get; set; }
    }

    public class BulletinService
    {
        private readonly ILogger<BulletinService> _logger;
        // 用於從組態檔中讀取設定
        private readonly IConfiguration _configuration;

        // 移除檔案系統路徑
        // private readonly string _bulletinPath = @"\\tpea31hwdfs01\專案執行\Common Folder\Bulletin";

        // 用於快取公告列表
        private List<BulletinItem>? _bulletinsCache;
        // 記錄上次載入快取的時間
        private DateTime _lastLoadTime;
        // 設定快取的有效時間，例如 5 分鐘
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        // 使用 SemaphoreSlim 來防止多個執行緒同時更新快取，確保執行緒安全
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // 修改建構式，注入 IConfiguration 以讀取連線字串
        public BulletinService(ILogger<BulletinService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration; // [新增]
        }

        // 這是元件將會呼叫的主要方法 (此方法邏輯不變，僅修改內部呼叫的載入方法名稱)
        public async Task<List<BulletinItem>?> GetBulletinsAsync()
        {
            // 檢查快取是否存在且尚未過期
            if (_bulletinsCache != null && _lastLoadTime.Add(_cacheDuration) > DateTime.UtcNow)
            {
                _logger.LogInformation("從快取中返回公告資料。");
                return _bulletinsCache;
            }

            // 如果快取不存在或已過期，則需要從來源重新載入
            // 進入信號量，確保只有一個執行緒可以執行載入邏輯
            await _semaphore.WaitAsync();
            try
            {
                // 再次檢查，因為在等待信號量期間，可能已有其他執行緒完成了載入
                if (_bulletinsCache != null && _lastLoadTime.Add(_cacheDuration) > DateTime.UtcNow)
                {
                    _logger.LogInformation("從快取中返回公告資料 (在等待後)。");
                    return _bulletinsCache;
                }

                _logger.LogInformation("快取過期或不存在，正在從資料庫重新載入...");
                // 呼叫從資料庫載入的方法
                await LoadBulletinsFromDatabaseAsync();
                return _bulletinsCache;
            }
            finally
            {
                // 釋放信號量
                _semaphore.Release();
            }
        }

        // 此方法已完全重寫，從檔案系統邏輯改為資料庫查詢邏輯
        private async Task LoadBulletinsFromDatabaseAsync()
        {
            try
            {
                // 從 appsettings.json 取得我們設定的連線字串
                var connectionString = _configuration.GetConnectionString("ReadConnection_A31_HWD");

                // 建立並開啟 SQL 連線
                using var connection = new SqlConnection(connectionString);

                // 根據圖片中的資料庫欄位名稱，將 'Topic' 欄位對應到 'Title'，'時間' 欄位對應到 'LastWriteTime'
                const string sql = "SELECT Topic AS Title, 時間 AS LastWriteTime FROM A31_HWD_bulletin ORDER BY 時間 DESC";

                // 使用 Dapper 執行非同步查詢，並將結果對應到 List<BulletinItem>
                var bulletinList = (await connection.QueryAsync<BulletinItem>(sql)).ToList();
                _logger.LogInformation($"從資料庫成功載入 {bulletinList.Count} 則公告。");

                // 更新快取和時間戳記
                _bulletinsCache = bulletinList;
                _lastLoadTime = DateTime.UtcNow;
                _logger.LogInformation("公告資料已成功載入並更新快取。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "從資料庫載入公告時發生錯誤。");
                // 即使發生錯誤，也更新快取為空列表，避免一直重試失敗的操作
                _bulletinsCache = new List<BulletinItem>();
                _lastLoadTime = DateTime.UtcNow;
            }
        }
    }
}