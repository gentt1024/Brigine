# Brigine 项目文档中心

> 📚 **文档导航** | 📊 **当前版本**: v0.1.5-dev | 🎯 **下一里程碑**: v0.2.0

欢迎来到 Brigine 项目文档中心！这里提供了项目相关的所有文档和资源。

## 🚀 新对话快速上手

### ⚡ 最快方式（推荐）
如果你是AI助手，用户只需要说：
```
请阅读 PROGRESS_STATUS.md 文档，了解 Brigine 跨引擎3D场景协作工具的当前开发状态
```

### 🤖 AI助手完整工作指导
AI助手还需要阅读工作流程规范：
```
请同时阅读 DEVELOPMENT_WORKFLOW.md，了解工作流程规范和文档维护要求
```

### 📖 完整了解方式
1. **[PROGRESS_STATUS.md](../PROGRESS_STATUS.md)** - 📊 当前进度状态（必读）
2. **[DEVELOPMENT_WORKFLOW.md](../DEVELOPMENT_WORKFLOW.md)** - 🤖 AI工作流程指导（AI必读）
3. **[README.md](../README.md)** - 项目概述和功能特性
4. **[ROADMAP.md](../ROADMAP.md)** - 长期发展计划
5. **[架构文档](../private/BrigineDocs/)** - 技术决策记录

## 🚀 快速导航

### 📋 核心文档
- **[项目概述](../README.md)** - 项目介绍、功能特性、快速开始
- **[开发路线图](../ROADMAP.md)** - 详细的版本计划和发展方向
- **[当前进度状态](../PROGRESS_STATUS.md)** - 📊 实时开发进度和功能状态

### 🏗️ 架构文档  
- **[通信架构总结](../private/BrigineDocs/COMMUNICATION_ARCHITECTURE_FINAL.md)** - gRPC架构优化完成总结
- **[客户端服务端分离](../private/BrigineDocs/CLIENT_SERVER_SEPARATION_SUMMARY.md)** - 项目重构技术方案

### 📖 使用指南

#### 新贡献者必读
1. **[PROGRESS_STATUS.md](../PROGRESS_STATUS.md)** - 了解当前开发状态
2. **[ROADMAP.md](../ROADMAP.md)** - 理解项目发展方向
3. **[README.md](../README.md)** - 快速开始指南

#### 开发者指南
1. **环境设置**: 按照README中的"开发环境设置"部分
2. **架构理解**: 查看private/BrigineDocs/下的技术文档
3. **API参考**: 检查各服务的Proto定义文件
4. **测试验证**: 按照快速开始章节验证功能

## 📊 项目状态概览

| 组件 | 状态 | 完成度 | 说明 |
|------|------|--------|------|
| 🔧 gRPC通信架构 | ✅ 完成 | 90% | 服务器可运行，客户端功能完整 |
| 🏗️ 核心框架系统 | ✅ 完成 | 95% | FrameworkManager、ServiceRegistry、AssetManager |
| 🎮 Unity编辑器集成 | 🔄 开发中 | 20% | 基础结构完成，编辑器插件开发中 |
| 🎯 Godot运行时集成 | 🔄 开发中 | 15% | 项目结构就绪，功能实现中 |
| 📦 USD完整支持 | ❌ 计划中 | 10% | 基础框架就绪，核心功能待开发 |

## 🔗 外部资源

### 技术栈文档
- [gRPC .NET](https://grpc.io/docs/languages/csharp/) - gRPC官方C#文档
- [Protocol Buffers](https://developers.google.com/protocol-buffers) - Proto定义语法
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) - 服务器框架
- [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) - Unity包管理
- [Godot C# API](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/) - Godot C#绑定

### USD (Universal Scene Description)
- [USD 官方文档](https://graphics.pixar.com/usd/docs/index.html)
- [USD.NET](https://github.com/Unity-Technologies/usd-unity-sdk) - Unity USD集成

## 📝 文档贡献

### 文档更新原则
1. **实时性**: 代码变更后及时更新相关文档
2. **准确性**: 确保文档与实际实现一致
3. **完整性**: 提供足够的上下文和示例
4. **可读性**: 使用清晰的语言和结构

### 重要文档维护
- **PROGRESS_STATUS.md**: 每次开发会话后更新
- **ROADMAP.md**: 版本计划变更时更新
- **README.md**: 重大功能完成后更新
- **架构文档**: 重要技术决策后更新

## 🤝 获取帮助

### 开发问题
1. 查看 **PROGRESS_STATUS.md** 了解当前状态
2. 检查 **private/BrigineDocs/** 中的技术决策
3. 参考 **ROADMAP.md** 理解设计思路

### 功能疑问
1. 查看 **README.md** 的API示例
2. 检查Proto定义文件了解接口
3. 查看服务器日志了解运行状态

---

**📧 联系信息**: 项目维护者  
**📅 最后更新**: 2025年5月26日  
**🔄 更新频率**: 每个开发周期 