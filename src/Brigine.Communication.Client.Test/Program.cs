using Brigine.Communication.Client;

Console.WriteLine("=== Brigine Communication Client Test ===");

try
{
    // 创建客户端 - 所有平台统一使用BrigineClient
    using var client = new BrigineClient("http://localhost:50051");
    
    Console.WriteLine("连接到Brigine服务器...");
    
    // 启动框架
    Console.WriteLine("启动框架...");
    var startResponse = await client.StartFrameworkAsync(
        new[] { "Unity", "Default" },
        new Dictionary<string, string> { { "test", "value" } }
    );
    
    if (startResponse.Success)
    {
        Console.WriteLine($"框架启动成功，ID: {startResponse.FrameworkId}");
        
        // 获取框架状态
        var statusResponse = await client.GetFrameworkStatusAsync(startResponse.FrameworkId);
        if (statusResponse.Success)
        {
            Console.WriteLine($"框架状态: 运行中={statusResponse.Status.IsRunning}");
            Console.WriteLine($"可用服务: {string.Join(", ", statusResponse.Status.AvailableServices)}");
        }
        
        // 测试资产加载
        Console.WriteLine("测试资产加载...");
        var loadResponse = await client.LoadAssetAsync(
            startResponse.FrameworkId,
            "test_scene.usd"
        );
        
        if (loadResponse.Success)
        {
            Console.WriteLine($"资产加载成功，ID: {loadResponse.AssetId}");
        }
        else
        {
            Console.WriteLine($"资产加载失败: {loadResponse.ErrorMessage}");
        }
        
        // 测试场景操作
        Console.WriteLine("测试场景操作...");
        var entity = BrigineClient.CreateEntity("TestEntity", "Entity");
        var addResponse = await client.AddEntityToSceneAsync(
            startResponse.FrameworkId,
            entity
        );
        
        if (addResponse.Success)
        {
            Console.WriteLine($"实体添加成功，ID: {addResponse.EntityId}");
            
            // 测试变换更新
            var transform = BrigineClient.CreateTransform(1.0f, 2.0f, 3.0f);
            var updateResponse = await client.UpdateEntityTransformAsync(
                startResponse.FrameworkId,
                addResponse.EntityId,
                transform
            );
            
            if (updateResponse.Success)
            {
                Console.WriteLine("实体变换更新成功");
            }
        }
        else
        {
            Console.WriteLine($"实体添加失败: {addResponse.ErrorMessage}");
        }
        
        // 停止框架
        Console.WriteLine("停止框架...");
        var stopResponse = await client.StopFrameworkAsync(startResponse.FrameworkId);
        if (stopResponse.Success)
        {
            Console.WriteLine("框架停止成功");
        }
        else
        {
            Console.WriteLine($"框架停止失败: {stopResponse.ErrorMessage}");
        }
    }
    else
    {
        Console.WriteLine($"框架启动失败: {startResponse.ErrorMessage}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"测试失败: {ex.Message}");
    Console.WriteLine("请确保Brigine服务器正在运行 (dotnet run --project src/Brigine.Communication.Server)");
}

Console.WriteLine("\n=== 架构说明 ===");
Console.WriteLine("✅ 所有平台统一使用 BrigineClient");
Console.WriteLine("✅ Unity特定功能通过 BrigineUnityExtensions 扩展方法提供");
Console.WriteLine("✅ 简化的架构，减少不必要的复杂性");

Console.WriteLine("\n按任意键退出...");
Console.ReadKey(); 