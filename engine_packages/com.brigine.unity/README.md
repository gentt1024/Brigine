# Brigine Unity 包

## 📋 概述

这个包提供了Brigine Communication在Unity中的集成。它包含了：

- Brigine.Communication.Client 的Unity集成
- Unity特定的扩展方法
- 示例MonoBehaviour组件

## 🚀 快速开始

1. 将 `BrigineUnityClient` 组件添加到场景中的GameObject上
2. 配置服务器URL（默认为 `http://localhost:50051`）
3. 运行场景，组件会自动：
   - 连接到Brigine服务器
   - 启动框架
   - 将GameObject添加到场景

## 📦 使用方式

### 基本用法

```csharp
using Brigine.Communication.Client;
using Brigine.Communication.Unity;
using UnityEngine;

public class MyBrigineComponent : MonoBehaviour
{
    private BrigineClient client;
    
    async void Start()
    {
        // 创建客户端（Unity中需要HttpHandler）
        var handler = new YetAnotherHttpHandler();
        client = new BrigineClient("http://localhost:50051", handler);
        
        // 启动框架
        var response = await client.StartFrameworkAsync(
            new[] { "Unity" },
            new Dictionary<string, string> { { "scene", gameObject.scene.name } }
        );
        
        if (response.Success)
        {
            // 使用Unity扩展方法
            var entity = BrigineUnityExtensions.CreateEntityFromGameObject(gameObject);
            await client.AddEntityToSceneAsync(response.FrameworkId, entity);
        }
    }
}
```

### 使用Unity扩展方法

```csharp
// 从Unity Transform创建Brigine Transform
var transform = BrigineUnityExtensions.CreateTransformFromUnity(gameObject.transform);

// 将Brigine Transform应用到Unity Transform
BrigineUnityExtensions.ApplyToUnityTransform(gameObject.transform, brigineTransform);

// 从Unity GameObject创建Brigine EntityInfo
var entity = BrigineUnityExtensions.CreateEntityFromGameObject(gameObject);
```

## ⚙️ 配置

### 服务器URL

在Inspector中设置 `BrigineUnityClient` 组件的 `Server Url` 字段，或通过代码设置：

```csharp
brigineClient.serverUrl = "http://your-server:50051";
```

### 框架配置

启动框架时可以配置提供者和参数：

```csharp
var response = await client.StartFrameworkAsync(
    new[] { "Unity", "CustomProvider" },
    new Dictionary<string, string> 
    { 
        { "scene", "MainScene" },
        { "customParam", "value" }
    }
);
```

## 🔧 注意事项

1. **HTTP处理器**：Unity中必须使用 `YetAnotherHttpHandler`
2. **异步操作**：所有Brigine操作都是异步的，使用 `async/await`
3. **错误处理**：所有操作都应该包含适当的错误处理
4. **资源清理**：在 `OnDestroy` 中停止框架并释放客户端

## 📝 示例场景

查看 `Assets/Scenes/SampleScene.unity` 获取完整示例。

## 🎯 最佳实践

1. **单例模式**：考虑使用单例模式管理Brigine客户端
2. **错误处理**：实现适当的错误处理和重试机制
3. **资源管理**：确保正确清理资源
4. **日志记录**：使用Unity的Debug.Log记录重要事件

## 🔍 调试

使用Unity的Console窗口查看日志和错误信息。所有操作都会记录详细的日志。