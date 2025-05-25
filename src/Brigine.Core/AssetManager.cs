namespace Brigine.Core
{
    public class AssetManager
    {
        private readonly IServiceRegistry _registry;

        public AssetManager(IServiceRegistry registry)
        {
            this._registry = registry;
        }

        public object LoadAsset(string assetPath)
        {
            var assetLoader = _registry.GetService<IAssetSerializer>();
            if (assetLoader == null) return null;
            
            var loadedAsset = assetLoader.Load(assetPath);

            return loadedAsset;
        }

        
    }
}