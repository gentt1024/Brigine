using System.Numerics;

namespace Brigine.Core
{
    public struct Transform
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public Transform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public static Transform Identity => new Transform(Vector3.Zero, Quaternion.Identity);
    }
}