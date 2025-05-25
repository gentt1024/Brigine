using System;
using System.IO;
using UnityEngine;
using Brigine.Core;
using Brigine.Unity;

public class FrameworkStartup : MonoBehaviour
{
    [Header("Framework Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool loadTestAsset = true;
    [SerializeField] private string testAssetPath = @"models\cube.usda";

    private Framework _framework;

    void Start()
    {
        if (autoStart)
        {
            InitializeFramework();
        }
    }

    public void InitializeFramework()
    {
        try
        {
            // 创建服务注册表
            var serviceRegistry = new ServiceRegistry();
            
            // 注册Unity特定的服务
            UnityServiceProvider.RegisterUnityServices(serviceRegistry);
            
            // 创建并启动框架
            _framework = new Framework(serviceRegistry);
            _framework.Start();
            
            Debug.Log("[Brigine] Framework initialized and started successfully");
            
            // 加载测试资源（如果启用）
            if (loadTestAsset)
            {
                LoadTestAsset();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to initialize framework: {ex.Message}");
        }
    }

    private void LoadTestAsset()
    {
        try
        {
            // 构建资源路径
            string assetsPath = Application.dataPath;
            string projectRoot = Directory.GetParent(assetsPath).FullName;
            string parentDirectory = Directory.GetParent(projectRoot).FullName;
            string externalAssetsPath = Path.Combine(parentDirectory, "assets");
            string modelPath = Path.Combine(externalAssetsPath, testAssetPath);
            
            if (File.Exists(modelPath))
            {
                _framework.LoadAsset(modelPath);
                Debug.Log($"[Brigine] Test asset loaded: {modelPath}");
            }
            else
            {
                Debug.LogWarning($"[Brigine] Test asset not found: {modelPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Brigine] Failed to load test asset: {ex.Message}");
        }
    }

    public void StopFramework()
    {
        if (_framework != null && _framework.IsRunning)
        {
            _framework.Stop();
            Debug.Log("[Brigine] Framework stopped");
        }
    }

    void OnDestroy()
    {
        StopFramework();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            StopFramework();
        }
        else if (_framework != null && !_framework.IsRunning)
        {
            _framework.Start();
        }
    }
}
