using Brigine.Communication.Protos;

namespace Brigine.Communication.Unity
{
    /// <summary>
    /// Unity特定的Brigine扩展方法
    /// 可以与通用BrigineClient一起使用
    /// </summary>
    public static class BrigineUnityExtensions
    {
#if UNITY_2018_1_OR_NEWER
        /// <summary>
        /// 从Unity Transform创建Brigine Transform
        /// </summary>
        /// <param name="unityTransform">Unity的Transform组件</param>
        /// <returns>Brigine的Transform消息</returns>
        public static Transform CreateTransformFromUnity(UnityEngine.Transform unityTransform)
        {
            return new Transform
            {
                Position = new Vector3
                {
                    X = unityTransform.position.x,
                    Y = unityTransform.position.y,
                    Z = unityTransform.position.z
                },
                Rotation = new Quaternion
                {
                    X = unityTransform.rotation.x,
                    Y = unityTransform.rotation.y,
                    Z = unityTransform.rotation.z,
                    W = unityTransform.rotation.w
                },
                Scale = new Vector3
                {
                    X = unityTransform.localScale.x,
                    Y = unityTransform.localScale.y,
                    Z = unityTransform.localScale.z
                }
            };
        }

        /// <summary>
        /// 将Brigine Transform应用到Unity Transform
        /// </summary>
        /// <param name="unityTransform">Unity的Transform组件</param>
        /// <param name="brigineTransform">Brigine的Transform消息</param>
        public static void ApplyToUnityTransform(UnityEngine.Transform unityTransform, Transform brigineTransform)
        {
            if (brigineTransform.Position != null)
            {
                unityTransform.position = new UnityEngine.Vector3(
                    brigineTransform.Position.X,
                    brigineTransform.Position.Y,
                    brigineTransform.Position.Z
                );
            }

            if (brigineTransform.Rotation != null)
            {
                unityTransform.rotation = new UnityEngine.Quaternion(
                    brigineTransform.Rotation.X,
                    brigineTransform.Rotation.Y,
                    brigineTransform.Rotation.Z,
                    brigineTransform.Rotation.W
                );
            }

            if (brigineTransform.Scale != null)
            {
                unityTransform.localScale = new UnityEngine.Vector3(
                    brigineTransform.Scale.X,
                    brigineTransform.Scale.Y,
                    brigineTransform.Scale.Z
                );
            }
        }

        /// <summary>
        /// 从Unity GameObject创建Brigine EntityInfo
        /// </summary>
        /// <param name="gameObject">Unity的GameObject</param>
        /// <param name="entityType">实体类型，默认为"GameObject"</param>
        /// <returns>Brigine的EntityInfo消息</returns>
        public static EntityInfo CreateEntityFromGameObject(UnityEngine.GameObject gameObject, string entityType = "GameObject")
        {
            return new EntityInfo
            {
                Name = gameObject.name,
                Type = entityType,
                Transform = CreateTransformFromUnity(gameObject.transform)
            };
        }
#endif
    }
} 