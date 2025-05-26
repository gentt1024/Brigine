using Brigine.Communication.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置Kestrel以支持HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    // 配置HTTP/2端点用于gRPC
    options.ListenLocalhost(50051, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

// 添加gRPC服务
builder.Services.AddGrpc();

// 添加CORS服务
builder.Services.AddCors();

// 添加日志
builder.Services.AddLogging();

// 注册服务实现为单例，确保依赖关系正确
builder.Services.AddSingleton<FrameworkServiceImpl>();
builder.Services.AddSingleton<AssetServiceImpl>();
builder.Services.AddSingleton<SceneServiceImpl>();

var app = builder.Build();

// 配置CORS（如果需要支持gRPC-Web）
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
          .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
});

// 配置gRPC服务
app.MapGrpcService<FrameworkServiceImpl>();
app.MapGrpcService<AssetServiceImpl>();
app.MapGrpcService<SceneServiceImpl>();

// 配置健康检查端点
app.MapGet("/", () => "Brigine Communication Server is running. Use a gRPC client to communicate.");

Console.WriteLine("Brigine Communication Server starting...");
Console.WriteLine("gRPC endpoint: http://localhost:50051");
Console.WriteLine("Services registered:");
Console.WriteLine("  - FrameworkService: Framework lifecycle management");
Console.WriteLine("  - AssetService: Asset loading and management via Core.AssetManager");
Console.WriteLine("  - SceneService: Scene entity management via Core.ISceneService");

app.Run(); 