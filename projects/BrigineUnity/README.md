# BrigineUnity Project

这是Brigine框架的Unity集成示例项目，展示了简化的本地Framework架构。

## 🎯 核心理念

### 本地Framework + 远程协作模式
```
Unity Client (本地Framework) ←→ gRPC Server (协作服务) ←→ 其他客户端
```

- **本地Framework**：处理Unity特定的场景操作和资产加载
- **gRPC客户端**：处理多用户协作和实时同步
- **清晰分离**：本地操作和远程协作职责明确

## 📁 项目结构

```
BrigineUnity/
├── Assets/                     # Unity资源文件
│   └── Scripts/               # 脚本文件
│       ├── FrameworkStartup.cs # 本地Framework启动脚本
│       ├── BrigineGrpcExample.cs # gRPC协作示例
│       └── BrigineSceneSync.cs # 场景同步组件
├── Packages/                   # Unity包管理
│   └── manifest.json          # 包清单文件
├── ProjectSettings/            # Unity项目设置
├── .gitignore                 # Git忽略文件
└── README.md                  # 项目说明
```

## 🚀 开发环境要求

- Unity 2021.3 LTS 或更高版本
- .NET Standard 2.1
- Windows 10/11

## ⚡ 快速开始

### 1. 打开项目
1. 使用Unity Hub打开项目目录 `projects/BrigineUnity/`
2. Unity会自动解析包依赖
3. 等待包导入完成

### 2. 本地Framework使用
1. 在场景中添加 `FrameworkStartup` 组件到任意GameObject
2. 配置组件参数：
   - **Auto Start**: 自动启动Framework
   - **Load Test Asset**: 自动加载测试资产
   - **Test Asset Path**: 测试资产路径
3. 运行项目

### 3. 协作功能测试
1. 启动Brigine gRPC服务器：
   ```bash
   cd ../../src/Brigine.Communication.Server
   dotnet run
   ```
2. 在场景中添加 `BrigineGrpcExample` 组件
3. 配置服务器地址（默认：http://localhost:50051）
4. 使用Inspector中的Context Menu测试功能

## 🔧 核心组件

### FrameworkStartup - 本地Framework管理
```csharp
public class FrameworkStartup : MonoBehaviour
{
    private Framework _framework;
    
    void Start()
    {
        // 创建本地Framework实例
        var serviceRegistry = new ServiceRegistry();
        UnityServiceProvider.RegisterUnityServices(serviceRegistry);
        
        _framework = new Framework(serviceRegistry, "Unity");
        _framework.Start();
    }
}
```

**功能特性**：
- **自动启动**：游戏开始时自动初始化Framework
- **资产加载**：支持USD、FBX等多种格式
- **生命周期管理**：自动处理启动、更新、停止
- **状态监控**：提供Framework状态查询

### BrigineGrpcExample - 协作功能示例
```csharp
public class BrigineGrpcExample : MonoBehaviour
{
    private Framework _localFramework;    // 本地Framework
    private BrigineClient _grpcClient;    // 协作客户端
    
    async void ConnectToServer()
    {
        _grpcClient = new BrigineClient("http://localhost:50051");
        
        // 创建协作会话
        var session = await _grpcClient.CreateSessionAsync("MyProject", "User1");
        
        // 监听场景变更
        await _grpcClient.StartSceneEventsAsync(session.SessionId, "User1", OnSceneEvent);
    }
}
```

**功能特性**：
- **会话管理**：创建和加入协作会话
- **实时同步**：监听和响应场景变更事件
- **本地集成**：将远程变更应用到本地场景
- **双向协作**：本地变更同步到远程

## 📦 包依赖说明

项目使用相对路径引用Brigine Unity包：

```json
{
  "dependencies": {
    "com.brigine.unity": "file:../../engine_packages/com.brigine.unity"
  }
}
```

### 优势
- **可移植性**: 使用相对路径，项目可以在任何机器上正常工作
- **团队协作**: 不依赖特定的绝对路径
- **版本控制友好**: 路径配置可以安全地提交到Git

## 🎮 使用示例

### 基础Framework使用

```csharp
using UnityEngine;
using Brigine.Core;
using Brigine.Unity;

public class BasicExample : MonoBehaviour
{
    private Framework _framework;

    void Start()
    {
        // 创建服务注册表
        var serviceRegistry = new ServiceRegistry();
        
        // 注册Unity特定的服务
        UnityServiceProvider.RegisterUnityServices(serviceRegistry);
        
        // 创建并启动框架
        _framework = new Framework(serviceRegistry, "Unity");
        _framework.Start();
        
        Debug.Log("Brigine Framework started!");
    }

    void OnDestroy()
    {
        // 完整清理资源
        _framework?.Dispose();
    }
}
```

### 加载USD资源

```csharp
// 加载USD文件到本地场景
string usdPath = "path/to/your/model.usda";
_framework.LoadAsset(usdPath);

// 获取场景实体
var entities = _framework.GetSceneEntities();
Debug.Log($"Scene has {entities.Count()} entities");
```

### 协作功能使用

```csharp
using Brigine.Communication.Client;

public class CollaborationExample : MonoBehaviour
{
    private BrigineClient _client;
    private string _sessionId;

    async void Start()
    {
        // 连接到协作服务器
        _client = new BrigineClient("http://localhost:50051");
        
        // 创建会话
        var session = await _client.CreateSessionAsync("MyProject", "User1");
        _sessionId = session.SessionId;
        
        // 监听场景事件
        await _client.StartSceneEventsAsync(_sessionId, "User1", OnSceneEvent);
    }
    
    private void OnSceneEvent(SceneChangeEvent evt)
    {
        Debug.Log($"Scene changed: {evt.ChangeType} on {evt.EntityId}");
        // 更新本地场景
    }
    
    async void AddEntity()
    {
        var entity = CreateEntityFromGameObject(someGameObject);
        await _client.CreateEntityAsync(_sessionId, "User1", entity);
    }
}
```

## 🔧 gRPC功能说明

**注意**: 当前Unity包中尚未包含gRPC相关的DLL文件。要使用gRPC功能，需要：

### 安装依赖
1. 安装 `YetAnotherHttpHandler` 包（Unity中使用gRPC必需）
2. 将以下DLL添加到Unity包中：
   - `Brigine.Communication.Client.dll`
   - `Brigine.Communication.Protos.dll`
   - `Google.Protobuf.dll`
   - `Grpc.Core.Api.dll`
   - `Grpc.Net.Client.dll`

### 使用YetAnotherHttpHandler
```csharp
using Cysharp.Net.Http;

// 创建HTTP处理器
var httpHandler = new YetAnotherHttpHandler();

// 创建gRPC客户端
var client = new BrigineClient("http://localhost:50051", httpHandler);
```

## 🎯 架构优势

### 简化设计
- **无FrameworkManager**：每个客户端直接管理自己的Framework实例
- **职责清晰**：本地Framework处理引擎操作，gRPC处理协作
- **易于理解**：架构简单直接，便于开发和维护

### 性能优化
- **本地优先**：本地操作无网络延迟
- **增量同步**：只同步变更的数据
- **事件驱动**：实时响应，避免轮询

### 扩展性
- **引擎无关**：Core层完全独立于Unity
- **插件化**：通过ServiceProvider轻松扩展
- **协作友好**：原生支持多用户编辑

## 🔍 调试和监控

### Framework状态监控
```csharp
// 获取Framework状态
var status = frameworkStartup.GetFrameworkStatus();
Debug.Log(status);

// 输出示例：
// Framework Status:
// - Engine: Unity
// - Running: True
// - Entities: 5
```

### 日志记录
所有操作都会记录详细的日志，使用Unity的Console窗口查看：
- `[Brigine]` 前缀标识Brigine相关日志
- 不同级别：Info、Warning、Error
- 详细的操作追踪和错误信息

### Context Menu调试
组件提供了丰富的Context Menu选项：
- **Initialize Framework**: 手动初始化Framework
- **Load Asset Locally**: 加载测试资产
- **Get Framework Status**: 查看Framework状态
- **Connect to Server**: 连接协作服务器（需要gRPC支持）

## 🎯 最佳实践

### 1. 资源管理
```csharp
void OnDestroy()
{
    // 使用Dispose进行完整清理
    _framework?.Dispose();
    _grpcClient?.Dispose();
}
```

### 2. 错误处理
```csharp
try
{
    _framework.LoadAsset(assetPath);
}
catch (Exception ex)
{
    Debug.LogError($"Asset loading failed: {ex.Message}");
}
```

### 3. 异步操作
```csharp
// 所有gRPC操作都是异步的
async void SomeMethod()
{
    var response = await _client.CreateEntityAsync(sessionId, userId, entity);
    if (!response.Success)
    {
        Debug.LogError($"Operation failed: {response.ErrorMessage}");
    }
}
```

## 📝 示例场景

查看 `Assets/Scenes/SampleScene.unity` 获取完整示例，包含：
- 预配置的FrameworkStartup组件
- BrigineGrpcExample组件示例
- 测试用的GameObject和资源

## 🚀 下一步

1. **完善gRPC集成**：添加必要的DLL和依赖
2. **增强协作功能**：实现更多实时协作特性
3. **性能优化**：优化大场景的同步性能
4. **工具集成**：开发Unity Editor插件

## 🤝 贡献

欢迎提交Issue和Pull Request来改进这个示例项目！ 