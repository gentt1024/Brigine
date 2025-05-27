using Brigine.Communication.Client;
using Brigine.Communication.Protos;

Console.WriteLine("=== Brigine 数据即服务架构测试 ===");
Console.WriteLine("测试新的会话管理、场景数据同步和实时事件流");
Console.WriteLine();

try
{
    await RunDataServiceTest();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 测试执行失败: {ex.Message}");
    Console.WriteLine($"详细错误: {ex}");
}

static async Task RunDataServiceTest()
{
    // 创建客户端 - 连接到新的数据服务
    using var client = new BrigineClient("http://localhost:50051");
    
    Console.WriteLine("✅ 连接到Brigine数据服务器...");
    
    // 1. 创建协作会话
    Console.WriteLine("\n🚀 创建协作会话...");
    var createSessionResponse = await client.CreateSessionAsync(
        "TestProject", 
        "TestUser1",
        new Dictionary<string, string> 
        { 
            { "description", "数据即服务架构测试" },
            { "version", "2.0" }
        }
    );
    
    if (!createSessionResponse.Success)
    {
        Console.WriteLine($"❌ 会话创建失败: {createSessionResponse.ErrorMessage}");
        return;
    }
    
    var sessionId = createSessionResponse.SessionId;
    Console.WriteLine($"✅ 会话创建成功，ID: {sessionId}");
    Console.WriteLine($"📋 项目名称: {createSessionResponse.SessionInfo.ProjectName}");
    Console.WriteLine($"👤 创建者: {createSessionResponse.SessionInfo.CreatorId}");
    
    // 2. 加入会话
    Console.WriteLine("\n👥 加入协作会话...");
    var joinResponse = await client.JoinSessionAsync(
        sessionId, 
        "TestUser1", 
        "Unity",
        new Dictionary<string, string> 
        { 
            { "version", "2023.3" },
            { "platform", "Windows" }
        }
    );
    
    if (!joinResponse.Success)
    {
        Console.WriteLine($"❌ 加入会话失败: {joinResponse.ErrorMessage}");
        return;
    }
    
    Console.WriteLine($"✅ 成功加入会话");
    Console.WriteLine($"👥 当前用户数: {joinResponse.ActiveUsers.Count}");
    foreach (var user in joinResponse.ActiveUsers)
    {
        Console.WriteLine($"   - {user.DisplayName} ({user.ClientType}) - {user.Status}");
    }
    
    // 3. 启动会话事件监听
    Console.WriteLine("\n🎧 启动会话事件监听...");
    var sessionEventCancellation = new CancellationTokenSource();
    
    var sessionEventTask = Task.Run(async () =>
    {
        try
        {
            await client.StartSessionEventsAsync(sessionId, "TestUser1", (sessionEvent) =>
            {
                Console.WriteLine($"📡 会话事件: {sessionEvent.EventType} | 用户: {sessionEvent.UserId} | 时间: {DateTimeOffset.FromUnixTimeSeconds(sessionEvent.Timestamp):HH:mm:ss}");
            }, sessionEventCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("🔇 会话事件监听已正常取消");
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
        {
            Console.WriteLine("🔇 会话事件监听已正常取消");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 会话事件监听错误: {ex.Message}");
        }
    });
    
    // 4. 启动场景变更事件监听
    Console.WriteLine("🎧 启动场景变更事件监听...");
    var sceneEventCancellation = new CancellationTokenSource();
    
    var sceneEventTask = Task.Run(async () =>
    {
        try
        {
            await client.StartSceneEventsAsync(sessionId, "TestUser1", (sceneEvent) =>
            {
                Console.WriteLine($"🎭 场景事件: {sceneEvent.ChangeType} | 实体: {sceneEvent.EntityId} | 用户: {sceneEvent.UserId}");
            }, sceneEventCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("🔇 场景事件监听已正常取消");
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
        {
            Console.WriteLine("🔇 场景事件监听已正常取消");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 场景事件监听错误: {ex.Message}");
        }
    });
    
    Console.WriteLine("✅ 事件监听已启动");
    
    // 5. 测试场景数据操作
    Console.WriteLine("\n🎭 测试场景数据操作...");
    
    // 创建多个实体
    var entityIds = new List<string>();
    
    for (int i = 1; i <= 3; i++)
    {
        var entity = BrigineClient.CreateEntity(
            $"DataTestEntity_{i}", 
            "Mesh",
            BrigineClient.CreateTransform(
                i * 2.0f, 1.0f, 0.0f, 
                0, i * 30.0f, 0, 1, 
                1.0f + i * 0.2f, 1.0f + i * 0.2f, 1.0f + i * 0.2f
            ),
            null,
            new Dictionary<string, PropertyValue>
            {
                { "material", new PropertyValue { StringValue = $"Material_{i}" } },
                { "visible", new PropertyValue { BoolValue = true } },
                { "priority", new PropertyValue { IntValue = i } }
            }
        );
        
        var createResponse = await client.CreateEntityAsync(sessionId, "TestUser1", entity);
        
        if (createResponse.Success)
        {
            entityIds.Add(createResponse.EntityId);
            Console.WriteLine($"✅ 创建实体 {i}: {createResponse.EntityId[..8]}... (版本: {createResponse.Version})");
            
            // 等待一下让事件处理
            await Task.Delay(500);
        }
        else
        {
            Console.WriteLine($"❌ 创建实体 {i} 失败: {createResponse.ErrorMessage}");
        }
    }
    
    // 6. 测试实体查询
    Console.WriteLine("\n🔍 测试实体查询...");
    
    var query = BrigineClient.CreateQuery(
        types: new[] { "Mesh" },
        limit: 10
    );
    
    var queryResponse = await client.QueryEntitiesAsync(sessionId, query);
    if (queryResponse.Success)
    {
        Console.WriteLine($"📊 查询到 {queryResponse.Entities.Count} 个Mesh实体:");
        foreach (var entity in queryResponse.Entities)
        {
            Console.WriteLine($"   - {entity.Name} [ID: {entity.EntityId[..8]}...] 版本: {entity.Metadata.Version}");
            Console.WriteLine($"     位置: ({entity.Transform.Position.X:F1}, {entity.Transform.Position.Y:F1}, {entity.Transform.Position.Z:F1})");
            Console.WriteLine($"     属性: {entity.Properties.Count} 个");
        }
    }
    
    // 7. 测试实体锁定
    Console.WriteLine("\n🔒 测试实体锁定机制...");
    
    if (entityIds.Count > 0)
    {
        var lockResponse = await client.LockEntityAsync(sessionId, "TestUser1", entityIds[0], LockType.Exclusive);
        if (lockResponse.Success)
        {
            Console.WriteLine($"✅ 成功锁定实体: {entityIds[0][..8]}...");
            Console.WriteLine($"🔐 锁定类型: {lockResponse.LockInfo.LockType}");
            Console.WriteLine($"⏰ 锁定时间: {DateTimeOffset.FromUnixTimeSeconds(lockResponse.LockInfo.AcquiredTime):HH:mm:ss}");
            
            // 更新被锁定的实体
            var lockedEntity = BrigineClient.CreateEntity(
                "UpdatedEntity", 
                "Mesh",
                BrigineClient.CreateTransform(10, 5, 0)
            );
            lockedEntity.EntityId = entityIds[0];
            
            var updateResponse = await client.UpdateEntityAsync(sessionId, "TestUser1", lockedEntity);
            if (updateResponse.Success)
            {
                Console.WriteLine($"✅ 更新锁定实体成功 (版本: {updateResponse.Version})");
            }
            
            // 解锁实体
            var unlockResponse = await client.UnlockEntityAsync(sessionId, "TestUser1", entityIds[0]);
            if (unlockResponse.Success)
            {
                Console.WriteLine($"🔓 成功解锁实体");
            }
        }
    }
    
    // 8. 测试批量操作
    Console.WriteLine("\n📦 测试批量操作...");
    
    var operations = new List<EntityOperation>();
    
    // 创建批量操作
    for (int i = 4; i <= 5; i++)
    {
        var entity = BrigineClient.CreateEntity(
            $"BatchEntity_{i}", 
            "Light",
            BrigineClient.CreateTransform(i * 3.0f, 2.0f, 0.0f)
        );
        
        operations.Add(new EntityOperation
        {
            OperationType = OperationType.Create,
            Entity = entity
        });
    }
    
    var batchResponse = await client.BatchUpdateAsync(sessionId, "TestUser1", operations);
    if (batchResponse.Success)
    {
        Console.WriteLine($"✅ 批量操作成功 (版本: {batchResponse.Version})");
        Console.WriteLine($"📋 操作结果: {batchResponse.Results.Count} 个");
        foreach (var result in batchResponse.Results)
        {
            if (result.Success)
            {
                Console.WriteLine($"   ✅ 实体: {result.EntityId[..8]}...");
            }
            else
            {
                Console.WriteLine($"   ❌ 失败: {result.ErrorMessage}");
            }
        }
    }
    
    // 9. 获取最终场景状态
    Console.WriteLine("\n📋 获取最终场景状态...");
    
    var sceneDataResponse = await client.GetSceneDataAsync(sessionId);
    if (sceneDataResponse.Success)
    {
        var sceneData = sceneDataResponse.SceneData;
        Console.WriteLine($"🎭 场景: {sceneData.Name} (版本: {sceneData.Version})");
        Console.WriteLine($"📊 总实体数: {sceneData.Entities.Count}");
        
        var entityTypes = sceneData.Entities.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());
        foreach (var kvp in entityTypes)
        {
            Console.WriteLine($"   - {kvp.Key}: {kvp.Value} 个");
        }
    }
    
    // 10. 清理和离开会话
    Console.WriteLine("\n🧹 清理资源...");
    
    // 取消事件监听
    sessionEventCancellation.Cancel();
    sceneEventCancellation.Cancel();
    
    // 等待事件监听任务完成
    try
    {
        await Task.WhenAll(sessionEventTask, sceneEventTask);
    }
    catch (OperationCanceledException)
    {
        // 预期的取消异常，正常情况
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ 清理过程中的异常: {ex.Message}");
    }
    
    // 离开会话
    var leaveResponse = await client.LeaveSessionAsync(sessionId, "TestUser1");
    if (leaveResponse.Success)
    {
        Console.WriteLine("✅ 成功离开会话");
    }
    else
    {
        Console.WriteLine($"⚠️ 离开会话失败: {leaveResponse.ErrorMessage}");
    }
    
    Console.WriteLine("\n🎉 数据即服务架构测试完成！");
    Console.WriteLine("✨ 新架构特点:");
    Console.WriteLine("   - 会话中心的协作管理");
    Console.WriteLine("   - 纯数据的场景同步");
    Console.WriteLine("   - 实时事件驱动更新");
    Console.WriteLine("   - 智能锁定机制");
    Console.WriteLine("   - 高效批量操作");
} 