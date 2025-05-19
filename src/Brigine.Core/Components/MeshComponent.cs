namespace Brigine.Core.Components
{
    public class MeshComponent : ComponentBase
    {
        public MeshData MeshData { get; set; }
        
        public MeshComponent(MeshData meshData)
        {
            MeshData = meshData;
        }
        
        public override void Update(float delta)
        {
            // Mesh通常不需要更新
        }
    }
} 