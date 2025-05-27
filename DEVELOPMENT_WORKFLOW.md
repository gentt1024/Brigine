# Brigine 开发工作流程

**最后更新**: 2025-05-27  
**适用对象**: 开发团队、AI助手、贡献者  

## 📁 文档组织结构

### 根目录文档（4个核心文档）
- **README.md** - 项目概述、快速开始、核心功能介绍
- **ROADMAP.md** - 长期发展规划和版本里程碑
- **PROGRESS_STATUS.md** - 当前开发进度和功能状态
- **DEVELOPMENT_WORKFLOW.md** - 本文档，开发工作流程和规范

### 详细技术文档位置
所有详细的架构决策、技术分析、变更记录等文档统一存放在：
```
private/BrigineDocs/
├── ARCHITECTURE_DECISIONS.md          # 架构决策记录(ADR)
├── ARCHITECTURE_CHANGE_SUMMARY.md     # 重大架构变更总结
├── CLIENT_SERVER_SEPARATION_SUMMARY.md # 客户端服务器分离总结
├── COMMUNICATION_ARCHITECTURE_FINAL.md # 通信架构最终设计
├── CROSS_ENGINE_ARCHITECTURE.md       # 跨引擎架构设计
├── VISION.md                          # 项目愿景和目标
└── ... (其他技术文档)
```

**重要说明**: 根目录保持简洁，只包含4个核心文档。所有详细的技术文档、架构分析、变更记录等都应放在`private/BrigineDocs/`目录下。

## 🤖 AI助手工作指导

### 新对话快速上手
1. **必读文档**（按优先级）：
   - `PROGRESS_STATUS.md` - 了解当前开发状态
   - `README.md` - 项目整体概述
   - `ROADMAP.md` - 发展规划
   - `private/BrigineDocs/ARCHITECTURE_DECISIONS.md` - 关键架构决策

2. **快速定位当前重点**：
   - 查看PROGRESS_STATUS.md中的"下一步工作计划"
   - 确认当前版本目标和优先级

### 开发任务流程
1. **任务分析**：
   - 明确要实现的功能模块
   - 检查PROGRESS_STATUS.md中的完成状态
   - 查阅相关架构文档

2. **方案设计**：
   - 基于现有架构设计方案
   - 参考private/BrigineDocs/中的技术文档
   - 确保与项目愿景一致

3. **实现验证**：
   - 编写代码并测试
   - 确保编译通过
   - 验证功能正确性

4. **文档更新**：
   - 更新PROGRESS_STATUS.md中的完成状态
   - 如有重大变更，在private/BrigineDocs/中创建相应文档
   - 必要时更新README.md

## 🔄 版本管理规范

### 版本号规则
- **主版本.次版本.修订版本-状态**
- 例如：v0.1.6-dev, v0.2.0-alpha, v1.0.0-release

### 里程碑管理
- **v0.2.0**: 完整的实时协作功能
- **v0.3.0**: USD完整支持
- **v0.4.0**: 多用户协作系统

### 分支策略
- **main**: 稳定版本
- **develop**: 开发版本
- **feature/***: 功能分支

## 📝 文档维护规范

### 文档更新原则
1. **及时性**: 代码变更后立即更新相关文档
2. **准确性**: 确保文档与实际实现一致
3. **完整性**: 重要变更需要完整的记录和说明

### 文档类型分类
- **用户文档**: README.md（面向用户和新开发者）
- **开发文档**: PROGRESS_STATUS.md, ROADMAP.md（面向开发团队）
- **技术文档**: private/BrigineDocs/（详细技术分析和决策）

### 重大变更记录
当发生重大架构变更时：
1. 在private/BrigineDocs/中创建详细的变更文档
2. 更新PROGRESS_STATUS.md中的相关状态
3. 必要时更新README.md中的架构说明
4. 在ROADMAP.md中调整相关计划

## 🎯 当前开发重点

### 短期目标（v0.2.0）
- Unity编辑器插件开发
- 实时场景同步功能
- 多客户端协作测试

### 技术债务管理
- 定期review代码质量
- 及时重构过时的实现
- 保持测试覆盖率

### 质量保证
- 所有新功能必须有对应测试
- 重要变更需要文档记录
- 保持编译无错误无警告

## 🔗 相关资源

### 外部参考
- **Remedy Entertainment GDC 2024**: "Real-Time World Editing Technology in Northlight"
- **OpenUSD**: 资产格式标准
- **gRPC**: 通信协议

### 内部文档
- **架构决策**: private/BrigineDocs/ARCHITECTURE_DECISIONS.md
- **技术愿景**: private/BrigineDocs/VISION.md
- **跨引擎设计**: private/BrigineDocs/CROSS_ENGINE_ARCHITECTURE.md

---

**注意**: 本工作流程会根据项目发展不断完善，请定期查看更新。 