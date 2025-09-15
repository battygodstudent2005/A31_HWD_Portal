using HWDPortal.Client.Pages;
using HWDPortal.Components;
using Microsoft.AspNetCore.Authentication.Negotiate; // 引用 Negotiate 驗證處理常式



var builder = WebApplication.CreateBuilder(args);

// 加入驗證服務，並設定 Windows 驗證 (Negotiate) 為預設方案
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

// 加入授權策略，要求所有使用者都必須是已驗證的使用者
builder.Services.AddAuthorization(options =>
{
    // 預設情況下，所有傳入的請求都將根據預設策略進行授權
    options.FallbackPolicy = options.DefaultPolicy;
});

// 將您的 UserInfoService 註冊為 Scoped 服務
builder.Services.AddScoped<UserInfoService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<HWDPortal.Services.BulletinService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// 在中介軟體管線中啟用驗證與授權，此順序很重要
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HWDPortal.Client._Imports).Assembly);
// 定義公告檔案所在的資料夾路徑
// 保持與 Bulletin.blazor 中的路徑一致
const string bulletinPath = @"\\tpea31hwdfs01\專案執行\Common Folder\Bulletin";

// 使用 Minimal API 建立一個 GET 端點，路徑為 /download/bulletin
app.MapGet("/download/bulletin", (string fileName, ILogger<Program> logger) =>
{
    try
    {
        // 安全性檢查：解碼檔名，並移除任何可能的路徑字元，防止路徑遍歷攻擊 (Path Traversal)
        var decodedFileName = Uri.UnescapeDataString(fileName);
        var safeFileName = Path.GetFileName(decodedFileName); // 只保留檔名部分
        var fullPath = Path.Combine(bulletinPath, safeFileName + ".msg");

        logger.LogInformation($"請求下載檔案: '{safeFileName}.msg'");
        logger.LogInformation($"完整路徑: '{fullPath}'");

        // 再次進行安全性驗證，確保最終路徑是在預期的資料夾內
        if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(bulletinPath)))
        {
            logger.LogWarning($"安全性警告：偵測到潛在的路徑遍歷攻擊。請求的檔名: '{decodedFileName}'");
            // 返回 400 Bad Request 錯誤
            return Results.BadRequest("無效的檔名。");
        }

        // 檢查檔案是否存在
        if (!File.Exists(fullPath))
        {
            logger.LogError($"錯誤：找不到檔案 '{fullPath}'");
            // 返回 404 Not Found 錯誤
            return Results.NotFound("找不到指定的公告檔案。");
        }

        // TODO: 在這裡執行您的使用者操作記錄邏輯
        // record_user_action("Buttetin_" + safeFileName);
        logger.LogInformation($"使用者操作記錄: Buttetin_{safeFileName}");


        // 讀取檔案內容為位元組陣列
        var fileBytes = File.ReadAllBytes(fullPath);

        // 將檔案內容作為結果回傳給瀏覽器
        // - fileBytes: 檔案的實際內容
        // - contentType: "application/vnd.ms-outlook" 是 .msg 檔案的標準 MIME 類型，
        //   這有助於瀏覽器識別檔案類型
        // - fileDownloadName: 提示瀏覽器下載時要使用的預設檔名
        return Results.File(fileBytes, "application/vnd.ms-outlook", safeFileName + ".msg");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"下載檔案 '{fileName}' 時發生未預期的錯誤。");
        // 返回 500 Internal Server Error 錯誤
        return Results.Problem("處理您的請求時發生內部錯誤。");
    }
});

app.Run();
