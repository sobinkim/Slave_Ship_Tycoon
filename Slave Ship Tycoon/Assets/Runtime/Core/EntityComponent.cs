using UnityEngine;

namespace SB.Core
{
    public abstract class EntityComponent : MonoBehaviour, IEntityComponent
    {
        public Entity Owner { get; private set; }

        public virtual void Initialize(Entity entity)
        {
            Owner = entity;
        }

        protected T GetCompo<T>() where T : class, IEntityComponent
        {
            return Owner != null ? Owner.GetCompo<T>() : null;
        }

        protected bool TryGetCompo<T>(out T component) where T : class, IEntityComponent
        {
            if (Owner == null)
            {
                component = null;
                return false;
            }

            return Owner.TryGetCompo(out component);
        }

        protected T RequireCompo<T>() where T : class, IEntityComponent
        {
            return Owner != null ? Owner.RequireCompo<T>() : null;
        }
    }
}
