using HWDPortal.Client.Pages;
using HWDPortal.Components;
using Microsoft.AspNetCore.Authentication.Negotiate; // �ޥ� Negotiate ���ҳB�z�`��



var builder = WebApplication.CreateBuilder(args);

// �[�J���ҪA�ȡA�ó]�w Windows ���� (Negotiate) ���w�]���
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

// �[�J���v�����A�n�D�Ҧ��ϥΪ̳������O�w���Ҫ��ϥΪ�
builder.Services.AddAuthorization(options =>
{
    // �w�]���p�U�A�Ҧ��ǤJ���ШD���N�ھڹw�]�����i����v
    options.FallbackPolicy = options.DefaultPolicy;
});

// �N�z�� UserInfoService ���U�� Scoped �A��
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

// �b�����n��޽u���ҥ����һP���v�A�����ǫܭ��n
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HWDPortal.Client._Imports).Assembly);
// �w�q���i�ɮשҦb����Ƨ����|
// �O���P Bulletin.blazor �������|�@�P
const string bulletinPath = @"\\tpea31hwdfs01\�M�װ���\Common Folder\Bulletin";

// �ϥ� Minimal API �إߤ@�� GET ���I�A���|�� /download/bulletin
app.MapGet("/download/bulletin", (string fileName, ILogger<Program> logger) =>
{
    try
    {
        // �w�����ˬd�G�ѽX�ɦW�A�ò�������i�઺���|�r���A������|�M������ (Path Traversal)
        var decodedFileName = Uri.UnescapeDataString(fileName);
        var safeFileName = Path.GetFileName(decodedFileName); // �u�O�d�ɦW����
        var fullPath = Path.Combine(bulletinPath, safeFileName + ".msg");

        logger.LogInformation($"�ШD�U���ɮ�: '{safeFileName}.msg'");
        logger.LogInformation($"������|: '{fullPath}'");

        // �A���i��w�������ҡA�T�O�̲׸��|�O�b�w������Ƨ���
        if (!Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(bulletinPath)))
        {
            logger.LogWarning($"�w����ĵ�i�G�������b�����|�M�������C�ШD���ɦW: '{decodedFileName}'");
            // ��^ 400 Bad Request ���~
            return Results.BadRequest("�L�Ī��ɦW�C");
        }

        // �ˬd�ɮ׬O�_�s�b
        if (!File.Exists(fullPath))
        {
            logger.LogError($"���~�G�䤣���ɮ� '{fullPath}'");
            // ��^ 404 Not Found ���~
            return Results.NotFound("�䤣����w�����i�ɮסC");
        }

        // TODO: �b�o�̰���z���ϥΪ̾ާ@�O���޿�
        // record_user_action("Buttetin_" + safeFileName);
        logger.LogInformation($"�ϥΪ̾ާ@�O��: Buttetin_{safeFileName}");


        // Ū���ɮפ��e���줸�հ}�C
        var fileBytes = File.ReadAllBytes(fullPath);

        // �N�ɮפ��e�@�����G�^�ǵ��s����
        // - fileBytes: �ɮת���ڤ��e
        // - contentType: "application/vnd.ms-outlook" �O .msg �ɮת��з� MIME �����A
        //   �o���U���s�����ѧO�ɮ�����
        // - fileDownloadName: �����s�����U���ɭn�ϥΪ��w�]�ɦW
        return Results.File(fileBytes, "application/vnd.ms-outlook", safeFileName + ".msg");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"�U���ɮ� '{fileName}' �ɵo�ͥ��w�������~�C");
        // ��^ 500 Internal Server Error ���~
        return Results.Problem("�B�z�z���ШD�ɵo�ͤ������~�C");
    }
});

app.Run();
