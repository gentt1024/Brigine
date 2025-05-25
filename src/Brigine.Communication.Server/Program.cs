using Brigine.Communication.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加gRPC服务
builder.Services.AddGrpc();

// 添加日志
builder.Services.AddLogging();

var app = builder.Build();

// 配置gRPC服务
app.MapGrpcService<FrameworkServiceImpl>();

// 配置健康检查端点
app.MapGet("/", () => "Brigine Communication Server is running. Use a gRPC client to communicate.");

// 配置CORS（如果需要支持gRPC-Web）
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
          .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
});

Console.WriteLine("Brigine Communication Server starting...");
Console.WriteLine("gRPC endpoint: http://localhost:50051");

app.Run("http://localhost:50051"); 