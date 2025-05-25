# Brigine - 跨引擎 3D 场景协作工具

Brigine 是一个基于 USD 格式的跨引擎 3D 场景编辑和运行时工具，通过 gRPC 通信实现不同游戏引擎间的实时场景同步和资产共享。

## 🎯 核心功能

### 1. 跨引擎场景同步
- 在 Unity 编辑器中修改场景，实时同步到 Godot 运行时
- 支持几何体、变换、材质、灯光的实时同步
- 基于 USD 格式的无损数据转换

### 2. 统一资产管理
- USD 格式作为中间格式，支持 Unity、Godot 间的资产转换
- 实时资产加载和卸载
- 资产依赖关系管理

### 3. 分布式编辑
- 多个编辑器可以同时编辑同一个场景
- 实时变更广播和冲突解决
- 支持本地和网络协作

## 🏗️ 技术架构

```
┌─────────────────┐    gRPC     ┌─────────────────┐
│  Unity Editor   │◄──────────► │ Brigine Server  │
│  + Plugin       │             │                 │
└─────────────────┘             │  ┌───────────┐  │
                                │  │Framework  │  │
┌─────────────────┐             │  │Service    │  │
│  Godot Editor   │◄────────────┤  └───────────┘  │
│  + Plugin       │             │  ┌───────────┐  │
└─────────────────┘             │  │Asset      │  │
                                │  │Service    │  │
┌─────────────────┐             │  └───────────┘  │
│  Godot Runtime  │◄────────────┤  ┌───────────┐  │
│  + Integration  │             │  │Scene      │  │
└─────────────────┘             │  │Service    │  │
                                │  └───────────┘  │
                                └─────────────────┘
```

## 🔧 具体工具组件

### Brigine.Communication.Server
独立的 gRPC 服务器，负责：
- 管理多个引擎客户端连接
- 处理场景数据的转换和同步
- 提供 REST API 用于监控和管理

### Unity Plugin (com.brigine.unity)
Unity Package，提供：
- 场景导出为 USD 格式
- 实时场景变更监听
- gRPC 客户端集成
- 编辑器 UI 面板

### Godot Integration (Brigine.Godot)
Godot 插件，提供：
- USD 场景加载器
- 实时场景更新接收
- C# 和 GDScript 绑定

## 📋 实际使用场景

### 场景 1：Unity 编辑 + Godot 预览
```bash
# 1. 启动 Brigine 服务器
cd src/Brigine.Communication.Server
dotnet run

# 2. Unity 中安装插件并连接
# 3. Godot 中加载 Brigine 插件并连接
# 4. 在 Unity 中编辑场景，Godot 中实时预览
```

### 场景 2：多人协作编辑
```bash
# 团队成员 A 使用 Unity 编辑器
# 团队成员 B 使用 Godot 编辑器
# 两人同时编辑同一个场景，实时看到对方的修改
```

### 场景 3：资产管道
```bash
# 美术师在 Maya 中导出 USD 文件
# 程序员在 Unity 中导入并调整
# 设计师在 Godot 中查看最终效果
```

## 🚀 快速开始

### 环境要求
- .NET 8.0 SDK
- Unity 2022.3+ (可选)
- Godot 4.0+ (可选)

### 1. 启动服务器
```bash
git clone https://github.com/your-org/Brigine.git
cd Brigine/src/Brigine.Communication.Server
dotnet run
```
服务器将在 `http://localhost:50051` 启动

### 2. Unity 集成
```bash
# 复制 Unity Package 到项目
cp -r com.brigine.unity /path/to/unity/project/Packages/

# 或通过 Package Manager 添加本地包
```

在 Unity 中：
1. 打开 `Window > Brigine > Connection Panel`
2. 连接到 `localhost:50051`
3. 创建或加载场景
4. 点击 "Start Sync" 开始同步

### 3. Godot 集成
```bash
# 复制插件到 Godot 项目
cp -r addons/brigine /path/to/godot/project/addons/
```

在 Godot 中：
1. 启用 Brigine 插件
2. 在场景中添加 BrigineSync 节点
3. 设置服务器地址为 `localhost:50051`
4. 运行场景开始接收同步数据

## 📊 支持的数据类型

### 几何体
- ✅ Mesh (顶点、法线、UV)
- ✅ Transform (位置、旋转、缩放)
- 🔄 Curves (计划中)
- 🔄 Volumes (计划中)

### 材质
- ✅ 基础材质属性 (颜色、金属度、粗糙度)
- ✅ 纹理贴图
- 🔄 节点材质 (计划中)

### 灯光
- ✅ 方向光、点光源、聚光灯
- ✅ 强度、颜色、阴影设置
- 🔄 区域光 (计划中)

### 动画
- 🔄 关键帧动画 (计划中)
- 🔄 骨骼动画 (计划中)

## 🔌 API 示例

### C# 客户端
```csharp
using var client = new BrigineClient("http://localhost:50051");

// 启动框架
var framework = await client.StartFrameworkAsync(new[] { "Unity" });

// 加载 USD 资产
var asset = await client.LoadAssetAsync(framework.FrameworkId, "cube.usda");

// 创建实体
var entity = BrigineClient.CreateEntity("MyCube", "Mesh");
await client.AddEntityToSceneAsync(framework.FrameworkId, entity);

// 更新变换
var transform = BrigineClient.CreateTransform(1, 2, 3);
await client.UpdateEntityTransformAsync(framework.FrameworkId, entity.EntityId, transform);
```

### REST API
```bash
# 获取框架状态
curl http://localhost:50051/api/framework/status

# 列出已加载资产
curl http://localhost:50051/api/assets

# 获取场景实体
curl http://localhost:50051/api/scene/entities
```

## 🛠️ 开发和扩展

### 添加新引擎支持
1. 实现 `IEngineAdapter` 接口
2. 创建引擎特定的插件
3. 注册到 `BrigineFrameworkService`

### 自定义数据类型
1. 扩展 USD Schema
2. 更新 Proto 定义
3. 实现转换器

## 📈 性能指标

### 当前性能
- 场景同步延迟：< 50ms (本地网络)
- 支持实体数量：1000+ 个
- 内存占用：< 100MB (服务器)

### 优化特性
- 增量同步：只传输变更的数据
- 压缩传输：gRPC 自动压缩
- 智能缓存：LRU 缓存策略

## 🐛 已知限制

- 目前只支持静态几何体，不支持动画
- 材质系统仅支持基础 PBR 属性
- 大型场景 (>10000 实体) 可能有性能问题
- 网络中断时需要手动重连

## 📋 开发路线图

### v0.2.0 (下个版本)
- [ ] 完善 USD 材质支持
- [ ] 添加场景层次结构同步
- [ ] 实现自动重连机制
- [ ] 性能优化和内存管理

### v0.3.0
- [ ] 支持基础动画同步
- [ ] 添加 Unreal Engine 集成
- [ ] Web 管理界面
- [ ] 插件市场基础设施

## 🤝 贡献指南

### 报告问题
- 使用 GitHub Issues
- 提供详细的复现步骤
- 包含日志文件和环境信息

### 提交代码
1. Fork 仓库
2. 创建功能分支
3. 编写测试
4. 提交 Pull Request

### 开发环境设置
```bash
git clone https://github.com/your-org/Brigine.git
cd Brigine
dotnet restore
dotnet build
dotnet test
```

## 📄 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件

---

**Brigine - 专业的跨引擎 3D 场景协作工具** 🔧⚡