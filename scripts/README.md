# Brigine DLL 自动拷贝系统

> 📦 **用途**: 自动将Core项目生成的DLL文件拷贝到Unity包中  
> 🎯 **目标**: 避免手动管理DLL文件，确保Unity包始终包含最新版本  
> ⚡ **更新时间**: 2025年5月26日

## 🚀 功能特性

### ✅ 自动化构建后拷贝
- 每次编译Core相关项目时自动触发
- 支持Debug和Release配置
- 支持.NET 8.0和.NET Standard 2.1框架

### 📦 支持的DLL文件
- **Brigine.Core.dll** + .pdb
- **Brigine.Communication.Client.dll** + .pdb  
- **Brigine.Communication.Protos.dll** + .pdb
- **Brigine.USD.dll** + .pdb
- **USD.NET.dll** (从NuGet包依赖)

### 🎯 目标位置
```
engine_packages/com.brigine.unity/Runtime/
├── Brigine.Core.dll
├── Brigine.Core.pdb
├── Brigine.Communication.Client.dll
├── Brigine.Communication.Client.pdb
├── Brigine.Communication.Protos.dll
├── Brigine.Communication.Protos.pdb
├── Brigine.USD.dll
├── Brigine.USD.pdb
├── USD.NET.dll
└── Runtime/ (USD相关文件)
```

## 🔧 使用方法

### 方法1：自动触发（推荐）
```bash
# 编译任何Core相关项目时自动触发
dotnet build src/Brigine.Core
dotnet build src/Brigine.Communication.Client
dotnet build src/Brigine.Communication.Protos
dotnet build src/Brigine.USD

# 或者编译整个解决方案
dotnet build Brigine.sln
```

### 方法2：手动执行PowerShell脚本
```powershell
# 使用默认参数 (Debug, netstandard2.1)
.\scripts\CopyDllsToUnity.ps1

# 指定配置和框架
.\scripts\CopyDllsToUnity.ps1 -Configuration Release -TargetFramework net8.0
```

### 方法3：手动执行批处理文件
```cmd
# 使用默认参数
.\scripts\CopyDllsToUnity.bat

# 指定配置
.\scripts\CopyDllsToUnity.bat Release

# 指定配置和框架
.\scripts\CopyDllsToUnity.bat Release net8.0
```

## 📋 Git管理策略

### ✅ 已配置的.gitignore规则
```gitignore
# Unity Package - 忽略从Core项目自动拷贝的DLL文件
engine_packages/com.brigine.unity/Runtime/*.dll
engine_packages/com.brigine.unity/Runtime/*.pdb
engine_packages/com.brigine.unity/Runtime/Runtime/*.dll
engine_packages/com.brigine.unity/Runtime/Runtime/*.pdb

# 但保留.meta文件，Unity需要这些文件
!engine_packages/com.brigine.unity/Runtime/*.meta
!engine_packages/com.brigine.unity/Runtime/Runtime/*.meta
```

### 🎯 管理原则
- **DLL文件**: 不纳入Git管理，每次构建时自动生成
- **Meta文件**: 纳入Git管理，Unity需要这些文件来识别资源
- **源代码**: 只管理src/目录下的源代码

## 🔍 故障排除

### 常见问题

#### ❌ PowerShell执行策略错误
```powershell
# 临时允许脚本执行
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# 或者使用-ExecutionPolicy参数
powershell -ExecutionPolicy Bypass -File .\scripts\CopyDllsToUnity.ps1
```

#### ❌ 找不到源DLL文件
```bash
# 确保先编译相关项目
dotnet build src/Brigine.Core
dotnet build src/Brigine.Communication.Client
dotnet build src/Brigine.Communication.Protos
```

#### ❌ Unity包路径不存在
```bash
# 检查Unity包目录是否存在
ls engine_packages/com.brigine.unity/Runtime/
```

### 调试信息
脚本会输出详细的拷贝过程信息：
- 📁 源路径和目标路径
- ✅ 成功拷贝的文件
- ⚠️ 跳过的文件（文件不存在）
- ❌ 拷贝失败的文件
- 📊 最终统计信息

## 🛠️ 自定义配置

### 修改拷贝的项目
编辑 `CopyDllsToUnity.ps1` 中的 `$ProjectsToCopy` 数组：
```powershell
$ProjectsToCopy = @(
    @{
        Name = "Brigine.Core"
        Files = @("Brigine.Core.dll", "Brigine.Core.pdb")
    },
    # 添加新项目...
)
```

### 修改USD相关文件
编辑 `$UsdNetFiles` 数组：
```powershell
$UsdNetFiles = @("USD.NET.dll", "其他USD文件.dll")
```

### 禁用自动拷贝
如果需要临时禁用自动拷贝，可以注释掉项目文件中的构建后事件：
```xml
<!-- 
<Target Name="CopyDllsToUnity" AfterTargets="Build">
    ...
</Target>
-->
```

## 📈 版本历史

- **v1.0** (2025年5月26日): 初始版本，支持基础DLL拷贝
- 支持多框架目标 (.NET 8.0, .NET Standard 2.1)
- 支持Debug/Release配置
- 集成到MSBuild构建流程

---

**💡 提示**: 这个系统确保Unity包始终包含最新的Brigine DLL文件，无需手动管理！ 