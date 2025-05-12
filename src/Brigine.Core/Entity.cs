using System.Collections.Generic;

namespace Brigine.Core
{
    public class Entity
    {
        public Transform Transform { get; set; } = Transform.Identity;
        private readonly List<IComponent> _components = new();
        private readonly ServiceRegistry _registry;

        public ServiceRegistry Registry => _registry;

        public Entity(ServiceRegistry registry, Entity parent = null)
        {
            _registry = registry;
        }

        public void AddComponent(IComponent component)
        {
            _components.Add(component);
            component.Entity = this;
        }

        public void Update(float delta)
        {
            foreach (var comp in _components)
            {
                comp.Update(delta);
            }

            var sceneService = _registry.GetService<ISceneService>();
            sceneService?.UpdateTransform(this, Transform);
        }
    }

    public interface IComponent
    {
        Entity Entity { get; set; }
        void Update(float delta);
    }
    
    public abstract class ComponentBase : IComponent
    {
        public Entity Entity { get; set; }
        public abstract void Update(float delta);
    }
}