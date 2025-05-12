using Brigine.Core;

var serviceRegistry = new ServiceRegistry();
serviceRegistry.RegisterProvider(new DefaultFunctionProvider());
var framework = new Framework(serviceRegistry);
string basePath = AppDomain.CurrentDomain.BaseDirectory;
string projectRoot = Path.GetFullPath(Path.Combine(basePath, @"..\..\..\..\.."));
string assetPath = Path.Combine(projectRoot, "assets", "cube.json");
framework.LoadScene(assetPath);
while (true){Thread.Sleep(100);}