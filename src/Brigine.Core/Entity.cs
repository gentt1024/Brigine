using System.Collections.Generic;

namespace Brigine.Core
{
    public class Entity
    {
        public Transform Transform { get; set; } = Transform.Identity;
        private readonly List<IComponent> _components = new();
        public IReadOnlyList<IComponent> Components => _components;
        public Entity Parent;
        public List<Entity> Children = new();
        
        public Entity(Entity parent = null)
        {
            Parent = parent;
            Parent?.Children.Add(this);
        }

        public void AddComponent(IComponent component)
        {
            _components.Add(component);
            component.Entity = this;
        }

        public T GetComponent<T>() where T : class, IComponent
        {
            foreach (var component in _components)
            {
                if (component is T typedComponent)
                {
                    return typedComponent;
                }
            }
            return null;
        }

        public void Update(float delta)
        {
            foreach (var comp in _components)
            {
                comp.Update(delta);
            }
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