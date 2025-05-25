# BrigineUnity Project

这是Brigine框架的Unity集成示例项目。

## 项目结构

```
BrigineUnity/
├── Assets/                     # Unity资源文件
│   └── Scripts/               # 脚本文件
│       ├── FrameworkStartup.cs # 框架启动脚本
│       └── BrigineGrpcExample.cs # gRPC客户端示例
├── Packages/                   # Unity包管理
│   └── manifest.json          # 包清单文件
├── ProjectSettings/            # Unity项目设置
├── .gitignore                 # Git忽略文件
└── README.md                  # 项目说明
```

## 开发环境要求

- Unity 2021.3 LTS 或更高版本
- .NET Standard 2.1
- Windows 10/11

## 快速开始

1. 使用Unity Hub打开项目目录 `projects/BrigineUnity/`
2. Unity会自动解析包依赖
3. 等待包导入完成
4. 在场景中添加 `FrameworkStartup` 组件到任意GameObject
5. 运行项目

## 包依赖说明

项目使用相对路径引用Brigine Unity包：

```json
{
  "dependencies": {
    "com.brigine.unity": "file:../../packages/com.brigine.unity"
  }
}
```

### 优势

- **可移植性**: 使用相对路径，项目可以在任何机器上正常工作
- **团队协作**: 不依赖特定的绝对路径
- **版本控制友好**: 路径配置可以安全地提交到Git

## 功能特性

- **跨引擎协作**: 通过Brigine框架与其他游戏引擎协作
- **USD支持**: 导入和导出Universal Scene Description文件
- **gRPC通信**: 与其他Brigine实例进行实时通信
- **场景同步**: 跨引擎同步实体、变换和组件

## 使用示例

### 基础用法

```csharp
using UnityEngine;
using Brigine.Core;
using Brigine.Unity;

public class FrameworkExample : MonoBehaviour
{
    private Framework _framework;

    void Start()
    {
        // 创建服务注册表
        var serviceRegistry = new ServiceRegistry();
        
        // 注册Unity特定的服务
        UnityServiceProvider.RegisterUnityServices(serviceRegistry);
        
        // 创建并启动框架
        _framework = new Framework(serviceRegistry);
        _framework.Start();
        
        Debug.Log("Brigine Framework started!");
    }

    void OnDestroy()
    {
        // 停止框架
        if (_framework != null && _framework.IsRunning)
        {
            _framework.Stop();
        }
    }
}
```

### 加载USD资源

```csharp
// 加载USD文件
string usdPath = "path/to/your/model.usda";
_framework.LoadAsset(usdPath);
```

### gRPC客户端使用

**注意**: 当前Unity包中尚未包含gRPC相关的DLL文件。要使用gRPC功能，需要：

1. 将以下DLL添加到Unity包中：
   - `Brigine.Communication.dll`
   - `Google.Protobuf.dll`
   - `Grpc.Core.Api.dll`
   - `Grpc.Net.Client.dll`

2. 使用 `BrigineGrpcExample` 组件：

```csharp
using Brigine.Communication;

public class GrpcExample : MonoBehaviour
{
    private BrigineClient _client;

    async void Start()
    {
        // 连接到Brigine服务器
        _client = new BrigineClient("http://localhost:50051");
        
        // 启动远程框架
        var response = await _client.StartFrameworkAsync(new[] { "Unity" });
        
        if (response.Success)
        {
            Debug.Log($"Remote framework started: {response.FrameworkId}");
        }
    }
}
```

### 使用FrameworkStartup组件

项目包含一个预制的 `FrameworkStartup` 组件，提供以下功能：

- **自动启动**: 在游戏开始时自动初始化框架
- **测试资源加载**: 可配置的测试资源自动加载
- **生命周期管理**: 自动处理框架的启动和停止
- **错误处理**: 完整的异常处理和日志记录

在Inspector中可以配置：
- `Auto Start`: 是否自动启动框架
- `Load Test Asset`: 是否加载测试资源
- `Test Asset Path`: 测试资源的相对路径

### 使用BrigineGrpcExample组件

项目包含 `BrigineGrpcExample` 组件，演示gRPC功能：

- **本地框架**: 可选择是否启动本地Brigine框架
- **远程连接**: 连接到Brigine gRPC服务器
- **资源加载**: 通过gRPC远程加载资源
- **场景操作**: 远程添加实体到场景

在Inspector中可以配置：
- `Server Address`: gRPC服务器地址
- `Connect On Start`: 是否自动连接
- `Use Local Framework`: 是否同时使用本地框架
- `Test Asset Path`: 测试资源路径

## gRPC集成状态

### ⚠️ 当前状态
- Unity包中**尚未包含**gRPC相关的DLL文件
- `BrigineGrpcExample.cs` 提供了完整的使用示例
- Assembly Definition已配置gRPC DLL引用

### ✅ 完整集成需要
1. 编译 `Brigine.Communication.dll`
2. 获取gRPC相关的DLL文件
3. 将DLL文件复制到Unity包的Runtime目录
4. 测试gRPC功能

### 🚀 使用步骤
1. 启动Brigine Communication Server: `dotnet run --project src/Brigine.Communication.Server`
2. 在Unity中添加 `BrigineGrpcExample` 组件
3. 配置服务器地址（默认: http://localhost:50051）
4. 运行Unity项目并测试连接

## Git忽略说明

项目包含Unity专用的 `.gitignore` 文件，忽略以下内容：

- **临时文件**: `Temp/`, `Library/`
- **用户设置**: `UserSettings/`
- **构建输出**: `Builds/`
- **IDE文件**: `.vs/`, `.idea/`
- **日志文件**: `*.log`

## 注意事项

- 确保Brigine Unity包位于 `packages/com.brigine.unity/` 目录
- 项目使用相对路径引用，保持目录结构完整
- 首次打开项目时，Unity会自动下载和导入依赖包
- 使用现代的 `UnityServiceProvider.RegisterUnityServices()` API
- 框架需要正确的生命周期管理（启动和停止）
- gRPC功能需要额外的DLL文件支持

## 相关文档

- [Brigine主项目](../../README.md)
- [Brigine Unity包文档](../../packages/com.brigine.unity/README.md)
- [Brigine Communication文档](../../src/Brigine.Communication/README.md)
- [项目结构重组说明](../../docs/PROJECT_STRUCTURE_REORGANIZATION.md) 