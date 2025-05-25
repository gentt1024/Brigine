using System;
using System.Collections.Generic;
using System.Numerics;
using pxr;

namespace Brigine.Core
{
    public class UsdNetAssetSerializer : IAssetSerializer
    {
        public object Load(string assetPath)
        {
            // 对于非USD资产，仍然使用JSON
            if (!assetPath.EndsWith(".usd", StringComparison.OrdinalIgnoreCase) && 
                !assetPath.EndsWith(".usda", StringComparison.OrdinalIgnoreCase) && 
                !assetPath.EndsWith(".usdc", StringComparison.OrdinalIgnoreCase) &&
                !assetPath.EndsWith(".usdz", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Not a USD file");
                return null;
            }
            
            return LoadUsdAsset(assetPath);
        }
        
        private Entity LoadUsdAsset(string usdPath)
        {
            Console.WriteLine($"Loading USD entity graph from: {usdPath}");

            try 
            {
                // 验证能否访问USD.NET的功能
                Console.WriteLine("尝试使用USD.NET API...");
                
                // 确保文件存在
                if (!System.IO.File.Exists(usdPath))
                {
                    Console.WriteLine($"错误: USD文件不存在: {usdPath}");
                    return null;
                }
                
                // 打开USD舞台
                var stage = UsdStage.Open(usdPath);
                if (stage == null)
                {
                    Console.WriteLine($"错误: 无法打开USD舞台: {usdPath}");
                    return null;
                }
                
                Console.WriteLine($"成功: USD舞台已打开: {usdPath}");
                
                // 创建根实体
                Entity rootEntity = new Entity();
                rootEntity.Transform = Transform.Identity;
                
                // 获取默认Prim
                var defaultPrim = stage.GetDefaultPrim();
                
                // 获取根节点列表
                var rootPrims = new List<UsdPrim>();
                if (defaultPrim.IsValid())
                {
                    // 如果有默认Prim，从它开始
                    rootPrims.Add(defaultPrim);
                    Console.WriteLine($"使用默认Prim作为根节点: {defaultPrim.GetPath().GetAsString()}");
                }
                else
                {
                    // 否则获取所有根节点
                    var rootRange = stage.GetPseudoRoot().GetChildren();
                    foreach (var prim in rootRange)
                    {
                        rootPrims.Add(prim);
                    }
                    Console.WriteLine($"找到 {rootPrims.Count} 个根节点");
                }
                
                // 处理所有根节点
                foreach (var prim in rootPrims)
                {
                    ProcessUsdPrim(prim, rootEntity);
                }
                
                // 关闭USD舞台 (USD.NET会自动管理，不需要显式关闭)
                
                return rootEntity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"USD.NET错误: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"堆栈跟踪:\n{ex.StackTrace}");
                
                // 检查是否是DLL加载问题
                if (ex is System.DllNotFoundException)
                {
                    Console.WriteLine("这是DLL加载问题，请确保所有USD本机库都正确放置并可访问");
                    // 输出当前路径环境变量
                    Console.WriteLine($"当前 PATH 环境变量: {Environment.GetEnvironmentVariable("PATH")}");
                    Console.WriteLine($"PXR_PLUGINPATH_NAME: {Environment.GetEnvironmentVariable("PXR_PLUGINPATH_NAME")}");
                }
                // 检查是否是类型解析问题
                else if (ex is System.TypeLoadException || ex is System.TypeInitializationException)
                {
                    Console.WriteLine("这是类型加载问题，可能是USD.NET引用不正确或版本不兼容");
                    Console.WriteLine("请确保已正确设置环境变量并复制所有必需的DLL");
                    
                    // 输出更多调试信息
                    try
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        Console.WriteLine($"应用程序基础目录: {baseDir}");
                        
                        // 列出DLL
                        var dllFiles = System.IO.Directory.GetFiles(baseDir, "*.dll");
                        Console.WriteLine("当前目录中的DLL文件:");
                        foreach (var dll in dllFiles)
                        {
                            Console.WriteLine($"  - {System.IO.Path.GetFileName(dll)}");
                        }
                    }
                    catch (Exception dex)
                    {
                        Console.WriteLine($"获取调试信息时出错: {dex.Message}");
                    }
                }
                
                return null;
            }
        }
        
        // 递归处理USD Prim
        private void ProcessUsdPrim(UsdPrim prim, Entity parentEntity)
        {
            if (!prim.IsValid())
            {
                Console.WriteLine($"跳过无效Prim");
                return;
            }
            
            string primPath = prim.GetPath().GetAsString();
            string primType = prim.GetTypeName().GetText();
            string primName = prim.GetName().GetText();
            Console.WriteLine($"处理Prim: {primPath}, 类型: {primType}, 名称: {primName}");
            
            // 创建新实体，使用 Prim 的名称
            Entity entity = new Entity(primName, parentEntity);
            
            // 处理变换矩阵
            ProcessTransform(prim, entity);
            
            // 处理几何数据
            if (primType == "Mesh")
            {
                try
                {
                    ProcessMesh(prim, entity);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理网格时出错: {ex.Message}");
                }
            }
            
            // 递归处理子节点
            var children = prim.GetChildren();
            foreach (var child in children)
            {
                ProcessUsdPrim(child, entity);
            }
        }
        
        // 处理变换数据
        private void ProcessTransform(UsdPrim prim, Entity entity)
        {
            try
            {
                // 默认设置为单位矩阵
                entity.Transform = Transform.Identity;
                
                try
                {
                    // 尝试创建UsdGeomXform
                    UsdGeomXform xform = new UsdGeomXform(prim);
                    if (!xform.GetPrim().IsValid())
                    {
                        return;
                    }
                    
                    Console.WriteLine($"处理变换: {prim.GetPath().GetAsString()}");
                    
                    // 时间码
                    UsdTimeCode timeCode = new UsdTimeCode(0);
                    
                    // 尝试获取transform属性
                    UsdAttribute translateAttr = xform.GetPrim().GetAttribute(new TfToken("xformOp:translate"));
                    UsdAttribute rotateXYZAttr = xform.GetPrim().GetAttribute(new TfToken("xformOp:rotateXYZ"));
                    UsdAttribute scaleAttr = xform.GetPrim().GetAttribute(new TfToken("xformOp:scale"));
                    
                    // 获取平移
                    Vector3 position = Vector3.Zero;
                    if (translateAttr != null && translateAttr.IsValid())
                    {
                        GfVec3d translation = new GfVec3d(0, 0, 0);
                        if (translateAttr.Get(translation, timeCode))
                        {
                            position = new Vector3(
                                (float)translation[0],
                                (float)translation[1],
                                (float)translation[2]
                            );
                            Console.WriteLine($"设置位置: {position}");
                        }
                    }
                    
                    // 获取旋转
                    Quaternion rotation = Quaternion.Identity;
                    if (rotateXYZAttr != null && rotateXYZAttr.IsValid())
                    {
                        GfVec3f rotationVec = new GfVec3f(0, 0, 0);
                        if (rotateXYZAttr.Get(rotationVec, timeCode))
                        {
                            // 转换为弧度
                            float radiansX = rotationVec[0] * MathF.PI / 180.0f;
                            float radiansY = rotationVec[1] * MathF.PI / 180.0f;
                            float radiansZ = rotationVec[2] * MathF.PI / 180.0f;
                            
                            // 创建旋转四元数
                            Quaternion rotX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, radiansX);
                            Quaternion rotY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, radiansY);
                            Quaternion rotZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, radiansZ);
                            
                            // 组合旋转
                            rotation = rotZ * rotY * rotX;
                            Console.WriteLine($"设置旋转: {rotation}");
                        }
                    }
                    
                    // 获取缩放
                    Vector3 scaling = Vector3.One;
                    if (scaleAttr != null && scaleAttr.IsValid())
                    {
                        GfVec3f scale = new GfVec3f(1, 1, 1);
                        if (scaleAttr.Get(scale, timeCode))
                        {
                            scaling = new Vector3(
                                (float)scale[0],
                                (float)scale[1],
                                (float)scale[2]
                            );
                            Console.WriteLine($"设置缩放: {scaling}");
                        }
                    }
                    
                    // 应用变换
                    Transform transform = new Transform();
                    transform.Position = position;
                    transform.Rotation = rotation;
                    
                    // 注意：检查Transform是否有Scale属性，如果没有，可能需要修改Transform类或使用其他属性名
                    // 由于我们不确定Scale属性的名称，我们应该查看Transform类的实现
                    // 这里假设它使用了Scale、Scaling或者类似的名称
                    try
                    {
                        // 反射来查找Scale属性
                        var scaleProperty = typeof(Transform).GetProperty("Scale");
                        if (scaleProperty != null)
                        {
                            scaleProperty.SetValue(transform, scaling);
                        }
                        else
                        {
                            var scalingProperty = typeof(Transform).GetProperty("Scaling");
                            if (scalingProperty != null)
                            {
                                scalingProperty.SetValue(transform, scaling);
                            }
                            else
                            {
                                Console.WriteLine("警告: 无法找到Transform类的缩放属性，将使用默认缩放");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"设置缩放时出错: {ex.Message}");
                    }
                    
                    entity.Transform = transform;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法创建UsdGeomXform: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理变换时出错: {ex.Message}\n{ex.StackTrace}");
                // 使用默认变换
                entity.Transform = Transform.Identity;
            }
        }
        
        // 处理网格数据
        private void ProcessMesh(UsdPrim prim, Entity entity)
        {
            try
            {
                // 创建网格数据
                MeshData meshData = new MeshData();
                var timeCode = UsdTimeCode.EarliestTime();
                
                // 创建网格访问对象
                UsdGeomMesh usdMesh = new UsdGeomMesh(prim);
                
                // 获取顶点
                var pointsAttr = usdMesh.GetPointsAttr();
                if (pointsAttr != null)
                {
                    VtVec3fArray points = pointsAttr.Get(timeCode);
                    
                    if (points.size() > 0)
                    {
                        Console.WriteLine($"获取到 {points.size()} 个顶点");
                        
                        // 复制顶点数据
                        int vertexCount = (int)points.size();
                        meshData.Vertices = new float[vertexCount * 3];
                        
                        for (int i = 0; i < vertexCount; i++)
                        {
                            var point = points[i];
                            meshData.Vertices[i * 3] = point[0];
                            meshData.Vertices[i * 3 + 1] = point[1];
                            meshData.Vertices[i * 3 + 2] = point[2];
                        }
                    }
                    else
                    {
                        Console.WriteLine("未找到顶点数据");
                        return;
                    }
                    
                    // 获取面索引
                    var indicesAttr = usdMesh.GetFaceVertexIndicesAttr();
                    if (indicesAttr != null)
                    {
                        VtIntArray faceVertexIndices = indicesAttr.Get(timeCode);

                        if (faceVertexIndices.size() > 0)
                        {
                            Console.WriteLine($"获取到 {faceVertexIndices.size()} 个顶点索引");
                            
                            // 复制索引数据
                            int indexCount = (int)faceVertexIndices.size();
                            meshData.FaceVertexIndices = new int[indexCount];
                            
                            for (int i = 0; i < indexCount; i++)
                            {
                                meshData.FaceVertexIndices[i] = faceVertexIndices[i];
                            }
                        }
                    }
                    
                    // 获取面顶点计数
                    var countsAttr = usdMesh.GetFaceVertexCountsAttr();
                    if (countsAttr != null)
                    {
                        VtIntArray faceVertexCounts = countsAttr.Get(timeCode);
                        
                        if (faceVertexCounts.size() > 0)
                        {
                            Console.WriteLine($"获取到 {faceVertexCounts.size()} 个面");
                            
                            // 复制面顶点计数数据
                            int faceCount = (int)faceVertexCounts.size();
                            meshData.FaceVertexCounts = new int[faceCount];
                            
                            for (int i = 0; i < faceCount; i++)
                            {
                                meshData.FaceVertexCounts[i] = faceVertexCounts[i];
                            }
                        }
                    }
                    
                    // 添加网格组件
                    entity.AddComponent(new Components.MeshComponent(meshData));
                    Console.WriteLine($"已添加网格组件: {meshData.Vertices.Length / 3} 顶点, {meshData.FaceVertexCounts?.Length ?? 0} 面");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理网格时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}