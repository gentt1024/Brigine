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

// 注册新的数据即服务架构的服务实现
builder.Services.AddSingleton<SessionServiceImpl>();
builder.Services.AddSingleton<SceneDataServiceImpl>();
builder.Services.AddSingleton<EventStreamServiceImpl>();

var app = builder.Build();

// 配置CORS（如果需要支持gRPC-Web）
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
          .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
});

// 配置新的gRPC服务
app.MapGrpcService<SessionServiceImpl>();
app.MapGrpcService<SceneDataServiceImpl>();
app.MapGrpcService<EventStreamServiceImpl>();

// 配置健康检查端点
app.MapGet("/", () => "Brigine Data Service Server is running. Use a gRPC client to communicate.");

Console.WriteLine("=== Brigine 数据即服务架构服务器启动 ===");
Console.WriteLine("gRPC endpoint: http://localhost:50051");
Console.WriteLine("新架构服务:");
Console.WriteLine("  - SessionService: 协作会话管理");
Console.WriteLine("  - SceneDataService: 场景数据和实体管理");
Console.WriteLine("  - EventStreamService: 实时事件流");
Console.WriteLine();
Console.WriteLine("架构特点:");
Console.WriteLine("  ✨ 会话中心的协作管理");
Console.WriteLine("  ✨ 纯数据的场景同步");
Console.WriteLine("  ✨ 实时事件驱动更新");
Console.WriteLine("  ✨ 智能锁定机制");
Console.WriteLine("  ✨ 高效批量操作");
Console.WriteLine();

app.Run(); 