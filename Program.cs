using static System.Net.WebRequestMethods;
using System;
using System.Text;
using System.IO.Compression;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var packageUrl = System.Environment.GetEnvironmentVariable("PACKAGEURL")!;
        var entryPoint = System.Environment.GetEnvironmentVariable("HANDLER")!;
                
        var targetFolder = @"/opt/console/";

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }
        
        var package = new PackageProcessor(targetFolder);        
        var result = await package.DownloadPackage(packageUrl);
        var fileList = package.UnzipPackage();

        var entryPointPart = entryPoint.Split(new[] { "::" }, 3, StringSplitOptions.RemoveEmptyEntries);
        var packageName = entryPointPart[0];
        var className = entryPointPart[1];
        var functionName = entryPointPart[2];

        var programTemplate = new ProgramTemplate(targetFolder);
        await programTemplate.GenerateCode(packageName, className, functionName);
        await programTemplate.GenerateCSProj(fileList.Where(f => f.EndsWith(".dll")).ToArray());
    }
}

public class PackageProcessor
{

    private String mDestFolder = null!;
    public PackageProcessor(String destFolder)
    {
        this.mDestFolder = destFolder;
    }

    public async Task<Boolean> DownloadPackage(String packageUrl)
    {
        HttpClient client = new HttpClient();
        var response = await client.GetAsync(packageUrl);
        if (response.IsSuccessStatusCode)
        {
            var targetFile = Path.Combine(mDestFolder, "package.zip");

            if (System.IO.File.Exists(targetFile))
            {
                System.IO.File.Delete(targetFile);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            // Save File
            var buffer = new Byte[65536];
            var count = 0;
            using (var file = System.IO.File.Create(targetFile, 65536))
            {
                while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await file.WriteAsync(buffer, 0, count);
                }
                await file.FlushAsync();
                file.Close();

            }
            return true;
        }
        return false;
    }

    public List<String> UnzipPackage()
    {
        List<String> fileList = new List<string>();
        var targetFile = Path.Combine(mDestFolder, "package.zip");
        using (var file = System.IO.File.Open(targetFile, FileMode.Open, FileAccess.ReadWrite))
        {
            using (var zip = new System.IO.Compression.ZipArchive(file))
            {
                zip.Entries.ToList().ForEach(e =>
                {
                    using (var entryStream = e.Open())
                    {
                        var entryFileName = e.Name;
                        var entryTargetFile = Path.Combine(mDestFolder, entryFileName);
                        e.ExtractToFile(entryTargetFile);
                        fileList.Add(entryFileName);
                    }
                });
            }
            file.Close();
        }
        return fileList;

    }
}

public class ProgramTemplate
{
    private String mTargetFolder = null!;
    public ProgramTemplate(String targetFolder)
    {
        this.mTargetFolder = targetFolder;
    }

    private String CodeTemplate = @"
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Runner;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);        
        builder.Services.AddSingleton<[CLASSNAME]>();
        var app = builder.Build();

        app.MapPost(""/2015-03-31/functions/{functionName}/invocations"",
            async (HttpContext context, String functionName) =>
            {
                var requestId = context.Request.Headers[""amz-sdk-invocation-id""].ToString();

                if (String.IsNullOrEmpty(requestId))
                {
                    requestId = Guid.NewGuid().ToString().ToLower();
                }

                var lambdaContext = new RunnerContext()
                {
                    AwsRequestId = requestId,
                    FunctionName = functionName,
                    FunctionVersion = ""default"",
                    InvokedFunctionArn = $""arn:aws:lambda:ap-local-1:000000000:{functionName}"",
                    MemoryLimitInMB = 128,
                    RemainingTime = new TimeSpan(0, 30, 0)
                };

                var bodyLength = context.Request.Headers.ContentLength ?? 0;
                var bodyBuffer = new Byte[bodyLength];
                var position = 0;
                var count = 0;
                while(position < bodyLength)
                {
                    count = await context.Request.Body.ReadAsync(bodyBuffer, position, bodyBuffer.Length - position);
                    position += count;
                }

                var jsonString = System.Text.Encoding.UTF8.GetString(bodyBuffer);
                var request = JsonSerializer.Deserialize<APIGatewayProxyRequest>(jsonString);

                var lambdaFunction = context.RequestServices.GetService<[CLASSNAME]>()!;                
                APIGatewayProxyResponse response = await lambdaFunction.[FUNCTIONNAME](request, lambdaContext);

                context.Response.StatusCode = 200;
                context.Response.ContentType = ""binary/octet-stream"";

                var outputJson = JsonSerializer.Serialize(response);
                var outputBuffer = System.Text.Encoding.UTF8.GetBytes(outputJson);
                await context.Response.Body.WriteAsync(outputBuffer, 0, outputBuffer.Length);
            });

        await app.RunAsync();
    }    
}

internal class RunnerContext : ILambdaContext
{
    public string AwsRequestId { get; set; } = null!;
    public IClientContext ClientContext { get; set; } = null!;
    public string FunctionName { get; set; } = null!;
    public string FunctionVersion { get; set; } = null!;
    public ICognitoIdentity Identity { get; set; } = null!;
    public string InvokedFunctionArn { get; set; } = null!;
    public ILambdaLogger Logger { get; set; } = null!;
    public string LogGroupName { get; set; } = null!;
    public string LogStreamName { get; set; } = null!;
    public int MemoryLimitInMB { get; set; } = 128;
    public TimeSpan RemainingTime { get; set; } = new TimeSpan(0, 30, 0);
}
";

    private String CSProjTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App""></FrameworkReference>
  </ItemGroup>

  <ItemGroup>
[REFERENCE]
  </ItemGroup>

</Project>

";

    /// <summary>
    /// PACKAGE::CLASS::FUNCTIONNAME
    /// </summary>
    /// <param name="package"></param>
    /// <param name="className"></param>
    /// <param name="functionName"></param>
    /// <returns></returns>
    public async Task<String> GenerateCode(String package, String className, String functionName)
    {
        var code = CodeTemplate.Replace("[CLASSNAME]", className).Replace("[FUNCTIONNAME]", functionName);

        var targetFile = Path.Combine(mTargetFolder, "Program.cs");
        using (var file = System.IO.File.Create(targetFile))
        {
            using (var sw = new StreamWriter(file))
            {
                await sw.WriteAsync(code);
                await sw.FlushAsync();
                sw.Close();
            }
            file.Close();
        }
        return code;
    }
    public async Task<String> GenerateCSProj(String[] zipFileName)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var f in zipFileName)
        {
            if (f.EndsWith(".dll"))
            {
                sb.AppendFormat($@"
    <Reference Include=""{f.Replace(".dll", "")}"">
      <HintPath>{f}</HintPath>
    </Reference>
");
            }
        }

        var csproj = CSProjTemplate.Replace("[REFERENCE]", sb.ToString());

        var targetFile = Path.Combine(mTargetFolder, "console.csproj");
        using (var file = System.IO.File.Create(targetFile))
        {
            using (var sw = new StreamWriter(file))
            {
                await sw.WriteAsync(csproj);
                await sw.FlushAsync();
                sw.Close();
            }
            file.Close();
        }
        return csproj;
    }
}