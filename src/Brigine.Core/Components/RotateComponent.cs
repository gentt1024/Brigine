using System;
using System.Numerics;

namespace Brigine.Core.Components
{
    public class RotateComponent : ComponentBase
    {
        public float Speed { get; set; } // 度/秒
        public float Angle { get; set; }

        public RotateComponent(float speed)
        {
            this.Speed = speed;
        }

        public override void Update(float delta)
        {
            Angle += Speed * delta;
            var t = Entity.Transform;
            t.Rotation = Quaternion.CreateFromYawPitchRoll(Angle * MathF.PI / 180f, 0, 0);
            Entity.Transform = t;
        }
    }
}