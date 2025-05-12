using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Brigine.Core
{
    public class AssetManager
    {
        private readonly ServiceRegistry _registry;

        public AssetManager(ServiceRegistry registry)
        {
            this._registry = registry;
        }

        public Entity LoadAsset(string assetPath)
        {
            var assetLoader = _registry.GetService<IAssetLoader>();
            var json = assetLoader?.LoadAsset(assetPath);
            if (json == null) return null;

            var entity = new Entity(_registry);
            var data = JsonConvert.DeserializeObject<AssetData>(json);

            if (data.Components.TryGetValue("Transform", out var transformData))
            {
                var posArray = transformData["position"].ToObject<float[]>();
                var rotArray = transformData["rotation"].ToObject<float[]>();
                entity.Transform = new Transform(
                    new Vector3(posArray[0], posArray[1], posArray[2]),
                    new Quaternion(rotArray[0], rotArray[1], rotArray[2], rotArray[3])
                );
            }

            if (data.Components.TryGetValue("Rotate", out var rotateData))
            {
                float speed = rotateData["speed"].ToObject<float>();
                entity.AddComponent(new Components.RotateComponent(speed));
            }
            return entity;
        }

        private class AssetData
        {
            public string Type { get; set; }
            public Dictionary<string, JToken> Components { get; set; }
        }
    }
}