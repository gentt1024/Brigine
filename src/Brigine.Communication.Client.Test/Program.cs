using Brigine.Communication.Client;

Console.WriteLine("=== Brigine Communication Client Test ===");
Console.WriteLine("测试真实的gRPC服务器和客户端功能");

try
{
    // 创建客户端 - 连接到真实的gRPC服务器
    using var client = new BrigineClient("http://localhost:50051");
    
    Console.WriteLine("✅ 连接到Brigine服务器...");
    
    // 启动框架 - 使用真实的Framework Manager
    Console.WriteLine("\n🚀 启动框架...");
    var startResponse = await client.StartFrameworkAsync(
        new[] { "Default" },
        new Dictionary<string, string> { 
            { "test_mode", "client_test" },
        }
    );
    
    if (!startResponse.Success)
    {
        Console.WriteLine($"❌ 框架启动失败: {startResponse.ErrorMessage}");
        return;
    }
    
    Console.WriteLine($"✅ 框架启动成功，ID: {startResponse.FrameworkId}");
    
    // 获取框架状态 - 验证真实的FrameworkManager状态
    var statusResponse = await client.GetFrameworkStatusAsync(startResponse.FrameworkId);
    if (statusResponse.Success)
    {
        Console.WriteLine($"📊 框架状态: 运行中={statusResponse.Status.IsRunning}");
        Console.WriteLine($"🔧 可用服务: {string.Join(", ", statusResponse.Status.AvailableServices)}");
        Console.WriteLine($"⚙️  配置项: {string.Join(", ", statusResponse.Status.Configuration.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        Console.WriteLine($"⏰ 启动时间: {DateTimeOffset.FromUnixTimeSeconds(statusResponse.Status.StartTime):yyyy-MM-dd HH:mm:ss}");
    }
    
    // 测试真实的资产加载 - 使用cube.usda文件
    Console.WriteLine("\n📦 测试资产加载...");
    
    // 获取当前工作目录并构建cube.usda的完整路径
    var currentDirectory = Directory.GetCurrentDirectory();
    var usdPath = Path.Combine(currentDirectory, "assets", "models", "cube.usda");
    
    Console.WriteLine($"📂 资产路径: {usdPath}");
    
    if (!File.Exists(usdPath))
    {
        Console.WriteLine($"❌ 资产文件不存在: {usdPath}");
        Console.WriteLine($"📁 当前工作目录: {currentDirectory}");
    }
    else
    {
        Console.WriteLine($"✅ 找到资产文件: {new FileInfo(usdPath).Length} bytes");
        
        var loadResponse = await client.LoadAssetAsync(
            startResponse.FrameworkId,
            usdPath,
            async: false,
            new Dictionary<string, string> { 
                { "type", "usd_scene" },
                { "format", "usda" }
            }
        );
        
        if (loadResponse.Success)
        {
            Console.WriteLine($"✅ 资产加载成功!");
            Console.WriteLine($"   📋 资产ID: {loadResponse.AssetId}");
            Console.WriteLine($"   📄 资产名称: {loadResponse.AssetInfo.Name}");
            Console.WriteLine($"   📂 资产路径: {loadResponse.AssetInfo.Path}");
            Console.WriteLine($"   🏷️  资产类型: {loadResponse.AssetInfo.Type}");
            Console.WriteLine($"   📊 文件大小: {loadResponse.AssetInfo.Size} bytes");
            Console.WriteLine($"   ⏰ 最后修改: {DateTimeOffset.FromUnixTimeSeconds(loadResponse.AssetInfo.LastModified):yyyy-MM-dd HH:mm:ss}");
            
            // 测试资产信息获取
            var assetInfoResponse = await client.GetAssetInfoAsync(startResponse.FrameworkId, loadResponse.AssetId);
            if (assetInfoResponse.Success)
            {
                Console.WriteLine($"✅ 资产信息获取成功: {assetInfoResponse.AssetInfo.Name}");
            }
            
            // 测试资产列表
            var listAssetsResponse = await client.ListAssetsAsync(startResponse.FrameworkId);
            if (listAssetsResponse.Success)
            {
                Console.WriteLine($"📋 已加载资产列表: {listAssetsResponse.Assets.Count} 个资产");
                foreach (var asset in listAssetsResponse.Assets)
                {
                    Console.WriteLine($"   - {asset.Name} ({asset.Type})");
                }
            }
            
            // 测试资产卸载
            var unloadResponse = await client.UnloadAssetAsync(startResponse.FrameworkId, loadResponse.AssetId);
            if (unloadResponse.Success)
            {
                Console.WriteLine("✅ 资产卸载成功");
            }
        }
        else
        {
            Console.WriteLine($"❌ 资产加载失败: {loadResponse.ErrorMessage}");
        }
    }
    
    // 测试真实的场景操作 - 使用Core.ISceneService
    Console.WriteLine("\n🎭 测试场景操作...");
    
    var cubeEntity = BrigineClient.CreateEntity(
        "CubeEntity", 
        "Mesh",
        BrigineClient.CreateTransform(0, 0, 0, 0, 0, 0, 1, 1, 1, 1)
    );
    
    var addResponse = await client.AddEntityToSceneAsync(
        startResponse.FrameworkId,
        cubeEntity
    );
    
    if (addResponse.Success)
    {
        Console.WriteLine($"✅ 实体添加成功，ID: {addResponse.EntityId}");
        
        // 测试变换更新
        var newTransform = BrigineClient.CreateTransform(
            posX: 2.5f, posY: 1.0f, posZ: -0.5f,
            rotY: 45.0f,
            scaleX: 1.5f, scaleY: 1.5f, scaleZ: 1.5f
        );
        
        var updateResponse = await client.UpdateEntityTransformAsync(
            startResponse.FrameworkId,
            addResponse.EntityId,
            newTransform
        );
        
        if (updateResponse.Success)
        {
            Console.WriteLine("✅ 实体变换更新成功");
            Console.WriteLine($"   📍 新位置: ({newTransform.Position.X:F1}, {newTransform.Position.Y:F1}, {newTransform.Position.Z:F1})");
            Console.WriteLine($"   🔄 新旋转: Y={newTransform.Rotation.Y:F1}°");
            Console.WriteLine($"   📏 新缩放: ({newTransform.Scale.X:F1}, {newTransform.Scale.Y:F1}, {newTransform.Scale.Z:F1})");
        }
        
        // 测试获取场景实体列表
        var entitiesResponse = await client.GetSceneEntitiesAsync(startResponse.FrameworkId);
        if (entitiesResponse.Success)
        {
            Console.WriteLine($"📋 场景实体列表: {entitiesResponse.Entities.Count} 个实体");
            foreach (var entity in entitiesResponse.Entities)
            {
                Console.WriteLine($"   - {entity.Name} ({entity.Type}) [ID: {entity.EntityId[..8]}...]");
            }
        }
        
        // 测试实体信息获取
        var entityInfoResponse = await client.GetEntityInfoAsync(startResponse.FrameworkId, addResponse.EntityId);
        if (entityInfoResponse.Success)
        {
            Console.WriteLine($"ℹ️  实体详细信息: {entityInfoResponse.Entity.Name}");
        }
        
        // 测试实体删除
        var removeResponse = await client.RemoveEntityFromSceneAsync(startResponse.FrameworkId, addResponse.EntityId);
        if (removeResponse.Success)
        {
            Console.WriteLine("✅ 实体删除成功");
        }
    }
    else
    {
        Console.WriteLine($"❌ 实体添加失败: {addResponse.ErrorMessage}");
    }
    
    // 停止框架
    Console.WriteLine("\n🛑 停止框架...");
    var stopResponse = await client.StopFrameworkAsync(startResponse.FrameworkId);
    if (stopResponse.Success)
    {
        Console.WriteLine("✅ 框架停止成功");
    }
    else
    {
        Console.WriteLine($"❌ 框架停止失败: {stopResponse.ErrorMessage}");
    }
    
    Console.WriteLine("\n🎉 所有测试完成!");
}
catch (Grpc.Core.RpcException rpcEx)
{
    Console.WriteLine($"❌ gRPC连接错误: {rpcEx.Status}");
    Console.WriteLine("💡 请确保Brigine服务器正在运行: dotnet run (在Brigine.Communication.Server目录)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 测试失败: {ex.Message}");
    Console.WriteLine($"📄 详细信息: {ex}");
}

Console.WriteLine("\n⌨️  按任意键退出...");
Console.ReadKey(); 