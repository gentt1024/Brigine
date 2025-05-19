namespace Brigine.Core
{
    public class MeshData
    {
        public float[] Vertices { get; set; }
        
        public int[] FaceVertexCounts { get; set; }
        public int[] FaceVertexIndices { get; set; }
        public float[] Normals { get; set; }
        public float[] UVs { get; set; }
    }
} 