# Unity .meta文件管理策略

> 📋 **策略制定时间**: 2025年5月26日  
> 🎯 **目标**: 简化.meta文件管理，让Unity正确处理引用关系  
> ✅ **新策略**: 不再忽略.meta文件，全部由Git跟踪

## 🔄 策略变更

### 📚 变更原因
用户反馈：
- .meta文件对Unity的引用管理很重要
- .meta文件通常很小，不会占用太多空间
- 全局忽略.meta文件可能导致Unity引用问题

### 🎯 新策略：全部跟踪
**决定**：移除对.meta文件的全局忽略，让所有.meta文件都被Git跟踪

## 📋 当前.gitignore配置

### Unity相关规则
```gitignore
# Unity - 不再全局忽略.meta文件，让Unity正确管理引用关系

# Unity Package - 忽略从Core项目自动拷贝的DLL文件
engine_packages/com.brigine.unity/Runtime/*.dll
engine_packages/com.brigine.unity/Runtime/*.pdb

# Unity Package - 忽略USD相关的native库文件
engine_packages/com.brigine.unity/Runtime/Runtime/
engine_packages/com.brigine.unity/Runtime/Runtime/**/*

# 忽略Unity包内部的.gitignore文件（使用项目根目录统一管理）
engine_packages/com.brigine.unity/.gitignore
```

### 🎯 简化的管理策略

#### ✅ **被Git跟踪的文件**
- **所有.meta文件** - Unity引用管理需要
- **所有源代码文件** - 项目核心内容
- **配置文件** - 项目设置

#### ❌ **被Git忽略的文件**
- **自动拷贝的DLL/PDB文件** - 从Core项目生成
- **USD native库文件** - Runtime/Runtime/目录下的二进制文件
- **Unity包内部的.gitignore** - 使用项目根目录统一管理

## 📊 当前跟踪的.meta文件

### Unity包中的.meta文件
```
engine_packages/com.brigine.unity/README.md.meta
engine_packages/com.brigine.unity/Runtime.meta
engine_packages/com.brigine.unity/Runtime/Brigine.Communication.Client.dll.meta
engine_packages/com.brigine.unity/Runtime/Brigine.Communication.Client.pdb.meta
engine_packages/com.brigine.unity/Runtime/Brigine.Communication.Protos.dll.meta
engine_packages/com.brigine.unity/Runtime/Brigine.Communication.Protos.pdb.meta
engine_packages/com.brigine.unity/Runtime/Brigine.Core.dll.meta
engine_packages/com.brigine.unity/Runtime/Brigine.Core.pdb.meta
engine_packages/com.brigine.unity/Runtime/Brigine.USD.dll.meta
engine_packages/com.brigine.unity/Runtime/USD.NET.dll.meta
engine_packages/com.brigine.unity/Runtime/Brigine.Unity.asmdef.meta
engine_packages/com.brigine.unity/Runtime/BrigineUnityExtensions.cs.meta
engine_packages/com.brigine.unity/Runtime/Runtime.meta
engine_packages/com.brigine.unity/Runtime/UnityServiceProvider.cs.meta
engine_packages/com.brigine.unity/package.json.meta
```

## 💡 优势

### ✅ 简化管理
- **无需复杂规则** - 不再需要例外规则
- **Unity友好** - 让Unity完全管理.meta文件
- **团队协作** - 所有成员共享相同的引用配置

### ✅ 避免问题
- **引用丢失** - 防止Unity引用关系断裂
- **导入设置** - 保持一致的导入配置
- **GUID管理** - 确保资源GUID的一致性

## 🎯 设计原则

1. **简单优先** → 减少复杂的忽略规则
2. **Unity优先** → 让Unity管理自己的文件
3. **团队协作** → 确保所有成员配置一致
4. **功能优先** → 只忽略真正不需要的文件

---

**✅ 新策略**: 简单、直接、Unity友好！ 