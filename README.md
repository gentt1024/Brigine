# Brigine - 跨引擎3D场景协作框架

**Brigine** 是一个现代化的跨引擎3D场景协作框架，支持Unity、Godot、Unreal Engine等多个游戏引擎之间的实时场景同步和协作编辑。

## 🎯 核心理念

### 简化的客户端-服务器架构
```
Unity Client (本地Framework) ←→ gRPC Server (纯数据服务) ←→ Godot Client (本地Framework)
```

- **本地Framework**：每个客户端运行自己的Framework实例，处理引擎特定的场景操作
- **gRPC服务器**：专注于数据同步和协作管理，不涉及引擎逻辑
- **事件驱动**：通过实时事件流确保所有客户端状态同步

### 设计原则
- **引擎平等**：所有引擎都是一等公民，无主从关系
- **数据驱动**：纯数据同步，引擎只负责渲染和交互
- **简单直接**：避免过度工程化，每个组件职责清晰
- **实时协作**：支持多用户同时编辑同一场景

## 🏗️ 架构概览

### Core层 - 跨引擎抽象
```csharp
// 每个客户端创建一个Framework实例
var serviceRegistry = new ServiceRegistry();
UnityServiceProvider.RegisterUnityServices(serviceRegistry);

var framework = new Framework(serviceRegistry, "Unity");
framework.Start();

// 加载资产
framework.LoadAsset("models/scene.usda");
```

### gRPC层 - 协作通信
```csharp
// 连接到协作服务器
var client = new BrigineClient("http://localhost:50051");

// 创建协作会话
var session = await client.CreateSessionAsync("MyProject", "User1");

// 监听实时场景变更
await client.StartSceneEventsAsync(session.SessionId, "User1", OnSceneChanged);
```

### 引擎扩展层 - 具体实现
```csharp
// Unity特定的场景服务
public class UnitySceneService : ISceneService
{
    public void AddToScene(Entity entity, Entity parent)
    {
        var gameObject = CreateGameObjectFromEntity(entity);
        // Unity特定的场景操作
    }
}
```

## 📁 项目结构

```
Brigine/
├── src/                              # 核心源代码
│   ├── Brigine.Core/                    # 核心Framework和服务接口
│   ├── Brigine.Communication.Server/    # gRPC服务器实现
│   ├── Brigine.Communication.Client/    # gRPC客户端库
│   ├── Brigine.Communication.Protos/    # Protocol Buffers定义
│   └── Brigine.Communication.Client.Test/ # 客户端测试程序
├── engine_packages/
│   ├── com.brigine.unity/              # Unity引擎集成包
│   ├── com.brigine.godot/              # Godot引擎集成包
│   └── com.brigine.unreal/             # Unreal引擎集成包
├── projects/
│   ├── BrigineUnity/                   # Unity示例项目
│   └── BrigineGodot/                   # Godot示例项目
└── assets/                             # 测试资产文件
```

## 🔧 核心功能

### 本地Framework管理
- **引擎集成**：每个引擎有自己的Framework实例
- **资产加载**：支持USD、FBX、OBJ、GLTF等多种格式
- **场景管理**：统一的Entity-Component模型
- **生命周期**：完整的启动、更新、停止流程

### 实时协作同步
- **会话管理**：多用户协作会话创建和管理
- **事件流**：基于gRPC流的实时事件通知
- **状态同步**：确保所有客户端场景状态一致
- **冲突解决**：智能的编辑冲突检测和锁定机制

### 跨引擎支持
- **Unity**：完整的Unity集成，支持GameObject和Component
- **Godot**：Godot 4.x集成，支持Node和Scene
- **Unreal**：Unreal Engine 5集成（开发中）

## 🚀 快速开始

### 1. 启动gRPC服务器
```bash
cd src/Brigine.Communication.Server
dotnet run
```

### 2. Unity客户端
```csharp
using Brigine.Core;
using Brigine.Unity;

public class BrigineExample : MonoBehaviour
{
    private Framework _framework;
    
    void Start()
    {
        // 创建本地Framework
        var serviceRegistry = new ServiceRegistry();
        UnityServiceProvider.RegisterUnityServices(serviceRegistry);
        
        _framework = new Framework(serviceRegistry, "Unity");
        _framework.Start();
        
        // 加载USD场景
        _framework.LoadAsset("path/to/scene.usda");
    }
    
    void OnDestroy()
    {
        _framework?.Dispose();
    }
}
```

### 3. 协作功能
```csharp
using Brigine.Communication.Client;

public class CollaborationExample : MonoBehaviour
{
    private BrigineClient _client;
    
    async void Start()
    {
        _client = new BrigineClient("http://localhost:50051");
        
        // 创建协作会话
        var session = await _client.CreateSessionAsync("MyProject", "User1");
        
        // 监听场景变更
        await _client.StartSceneEventsAsync(session.SessionId, "User1", OnSceneEvent);
    }
    
    private void OnSceneEvent(SceneChangeEvent evt)
    {
        Debug.Log($"Scene changed: {evt.ChangeType} on {evt.EntityId}");
        // 更新本地场景
    }
}
```

## 🛠️ 开发指南

### 添加新引擎支持

1. **创建引擎包**:
   ```
   engine_packages/com.brigine.{engine}/
   ├── Runtime/
   │   ├── {Engine}ServiceProvider.cs
   │   ├── {Engine}SceneService.cs
   │   └── {Engine}Extensions.cs
   └── package.json
   ```

2. **实现服务接口**:
   ```csharp
   public class {Engine}SceneService : ISceneService
   {
       public void AddToScene(Entity entity, Entity parent)
       {
           // 引擎特定的场景操作实现
       }
       
       // 实现其他接口方法...
   }
   ```

3. **注册服务提供者**:
   ```csharp
   public static class {Engine}ServiceProvider
   {
       public static void Register{Engine}Services(IServiceRegistry registry)
       {
           registry.RegisterSingleton<ISceneService>(() => new {Engine}SceneService());
           registry.RegisterSingleton<IUpdateService>(() => new {Engine}UpdateService());
           registry.RegisterSingleton<ILogger, {Engine}Logger>();
       }
   }
   ```

### 自定义资产加载器

```csharp
public class CustomAssetSerializer : IAssetSerializer
{
    public object Load(string assetPath)
    {
        // 实现自定义资产加载逻辑
        return LoadCustomFormat(assetPath);
    }
}

// 注册自定义加载器
serviceRegistry.RegisterSingleton<IAssetSerializer>(() => new CustomAssetSerializer());
```

## 📊 性能特性

- **增量同步**：只传输变更的数据
- **事件驱动**：避免轮询，实时响应变更
- **本地优化**：每个引擎使用最适合的数据结构
- **并发支持**：多用户同时编辑不同区域

## 🔍 与其他方案的对比

### vs 传统引擎插件
- ✅ **跨引擎**：不限制于单一引擎生态
- ✅ **实时协作**：原生支持多用户编辑
- ✅ **数据驱动**：纯数据同步，性能更好

### vs 云端渲染方案
- ✅ **本地性能**：充分利用本地GPU性能
- ✅ **离线工作**：不依赖网络连接进行本地编辑
- ✅ **引擎原生**：保持各引擎的原生工作流

## 🎯 使用场景

- **跨引擎团队协作**：Unity美术 + Godot程序 + Unreal设计师
- **实时预览**：在不同引擎中同时预览同一场景
- **资产管道**：统一的USD资产管道，支持多引擎导出
- **远程协作**：分布式团队的实时场景编辑

## 📝 开发状态

### ✅ 已完成 (95%+)
- **Core架构**：Framework、ServiceRegistry、Entity系统
- **gRPC通信**：完整的服务器和客户端实现
- **Unity集成**：完整的Unity支持和示例项目
- **协作功能**：会话管理、实时事件、锁定机制

### 🚧 进行中 (15-20%)
- **Godot集成**：基础结构完成，运行时集成进行中
- **Unreal集成**：项目结构创建，核心功能开发中
- **USD支持**：框架存在，完整USD.NET集成待完成

### 🎯 下一步计划
1. 完善Godot运行时集成
2. 实现完整的USD资产管道
3. 添加Unreal Engine支持
4. 性能优化和压力测试

## 🤝 贡献指南

1. Fork项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启Pull Request

## 📄 许可证

本项目采用MIT许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

- [Remedy Entertainment](https://www.remedygames.com/) - 实时世界编辑技术的灵感来源
- [Pixar USD](https://graphics.pixar.com/usd/) - 通用场景描述格式
- [gRPC](https://grpc.io/) - 高性能RPC框架
- [Unity](https://unity.com/)、[Godot](https://godotengine.org/)、[Unreal Engine](https://www.unrealengine.com/) - 游戏引擎支持