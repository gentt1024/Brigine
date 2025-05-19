using System;
using System.IO;
using Brigine.Core;
using Brigine.Core.Components;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using pxr;

namespace Brigine.Player
{
    class Program
    {
        private const string LOG_FILE_PATH = "usd_wrapper_debug.log";
        static void Main(string[] args)
        {
            string usdPath = "assets/models/cube.usda";
            
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Exception ex = (Exception)eventArgs.ExceptionObject;
                LogError("未处理异常", ex);
            };
            
            try
            {
                // 记录启动信息
                Console.WriteLine($"Brigine Player启动 - 版本 {Assembly.GetExecutingAssembly().GetName().Version}");
                Console.WriteLine($"OS: {Environment.OSVersion}");
                Console.WriteLine($"运行时: {RuntimeInformation.FrameworkDescription}");
                Console.WriteLine($"进程架构: {RuntimeInformation.ProcessArchitecture}");
                Console.WriteLine();
                
                // 先启用日志记录
                EnableLogging();
                
                Console.WriteLine("Brigine USD Demo");
                
                // 创建框架 - 使用USD.NET功能
                var framework = new Framework();
                framework.Start();
                
                // 获取一个ILogger用于输出
                var logger = framework.Services.GetService<ILogger>();
                if (logger == null)
                {
                    Console.WriteLine("警告: 未找到Logger服务");
                }
                
                // 获取场景服务
                var sceneService = framework.Services.GetService<ISceneService>();
                if (sceneService == null)
                {
                    Console.WriteLine("警告: 未找到ISceneService服务");
                }
                
                try
                {
                    // 尝试加载USD资产
                    Console.WriteLine($"正在加载USD文件: {usdPath}");
                    
                    // 通过框架加载场景
                    Console.WriteLine("通过Framework.LoadScene加载场景...");
                    
                    // 使用安全加载辅助方法
                    SafeLoadAsset(framework, usdPath);
                    
                    // 检查加载的实体
                    if (sceneService != null)
                    {
                        var entities = sceneService.GetEntities().ToList();
                        Console.WriteLine($"成功: 场景已加载，有 {entities.Count} 个实体");
                        
                        // 打印每个实体的信息
                        foreach (var entity in entities)
                        {
                            PrintEntityInfo(entity);
                        }
                    }
                    else
                    {
                        Console.WriteLine("警告: 无法获取场景实体");
                    }
                    
                    // 简单循环以让框架有时间处理
                    Console.WriteLine("场景已加载。按Enter退出...");
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    LogError("加载场景时出错", ex);
                }
            }
            catch (Exception ex)
            {
                LogError("程序初始化失败", ex);
            }
        }
        
        // 安全加载资产的辅助方法
        private static void SafeLoadAsset(Framework framework, string assetPath)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(assetPath))
                {
                    Console.WriteLine($"错误: 文件不存在 - {assetPath}");
                    Console.WriteLine($"当前目录: {Directory.GetCurrentDirectory()}");
                    return;
                }
                
                // 如果USD日志文件存在，先清空
                string logFilePath = "D:/usd_wrapper_debug.log";
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
                
                Console.WriteLine($"开始加载资产: {assetPath}");
                framework.LoadAsset(assetPath);
                
                // 检查USD日志
                if (File.Exists(logFilePath))
                {
                    string logContent = File.ReadAllText(logFilePath);
                    Console.WriteLine($"USD包装器日志:\n{logContent}");
                }
                
                Console.WriteLine($"加载完成: {assetPath}");
            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine($"缺少DLL: {ex.Message}");
                Console.WriteLine("请确保所有必要的USD DLL都已正确复制到输出目录");
            }
            catch (EntryPointNotFoundException ex)
            {
                Console.WriteLine($"找不到入口点: {ex.Message}");
                Console.WriteLine("可能使用了不兼容版本的USD库");
            }
            catch (SEHException ex)
            {
                Console.WriteLine($"USD本机代码异常: {ex.Message}");
                Console.WriteLine($"错误代码: 0x{ex.ErrorCode:X8}");
                Console.WriteLine("这可能是由于vector操作错误导致的");
            }
            catch (Exception ex)
            {
                LogError("加载资产时出错", ex);
            }
        }
        
        // 错误日志辅助方法
        private static void LogError(string message, Exception ex)
        {
            Console.WriteLine($"错误: {message}");
            Console.WriteLine($"异常: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            
            // 保存到日志文件
            try
            {
                string crashLog = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_crash.log";
                File.WriteAllText(crashLog, $"时间: {DateTime.Now}\n" +
                                            $"消息: {message}\n" +
                                            $"异常: {ex.GetType().Name}: {ex.Message}\n" +
                                            $"堆栈跟踪: {ex.StackTrace}\n\n" +
                                            $"内部异常: {ex.InnerException}");
                Console.WriteLine($"已将错误详情写入: {crashLog}");
            }
            catch
            {
                Console.WriteLine("无法写入错误日志文件");
            }
        }
        
        // 启用日志记录的辅助方法
        private static void EnableLogging()
        {
            string usdLogPath = Path.Combine(Directory.GetCurrentDirectory(), LOG_FILE_PATH);
            
            // 确保日志目录存在
            try
            {
                if (File.Exists(usdLogPath))
                {
                    File.Delete(usdLogPath);
                }
                
                // 创建一个空的日志文件，确认有权限
                File.WriteAllText(usdLogPath, "日志启动: " + DateTime.Now + Environment.NewLine);
                Console.WriteLine($"已初始化USD日志: {usdLogPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告: 无法初始化USD日志文件: {ex.Message}");
            }
        }
        
        // 打印实体信息的辅助方法
        private static void PrintEntityInfo(Entity entity)
        {
            if (entity == null)
                return;
                
            Console.WriteLine($"实体信息: Transform={entity.Transform}");
            
            // 使用GetComponent来获取所有组件的信息
            var meshComponent = entity.GetComponent<MeshComponent>();
            if (meshComponent != null)
            {
                Console.WriteLine($"- 包含MeshComponent组件");
                
                // 如果有MeshData，显示一些网格信息
                var meshData = meshComponent.GetType().GetProperty("Mesh")?.GetValue(meshComponent) as MeshData;
                if (meshData != null)
                {
                    Console.WriteLine($"  顶点数量: {meshData.Vertices?.Length / 3 ?? 0}");
                    Console.WriteLine($"  索引数量: {meshData.FaceVertexIndices?.Length ?? 0}");
                }
            }
            
            var rotateComponent = entity.GetComponent<RotateComponent>();
            if (rotateComponent != null)
            {
                Console.WriteLine($"- 包含RotateComponent组件");
            }
            
            // 显示所有组件类型
            Console.WriteLine("- 所有组件类型:");
            foreach (var component in entity.Components)
            {
                if (component != null)
                {
                    Console.WriteLine($"  {component.GetType().Name}");
                }
            }
        }
    }
}