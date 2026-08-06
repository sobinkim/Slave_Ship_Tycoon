using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SB.Core
{
    public abstract class Entity : MonoBehaviour
    {
        [SerializeField] private string entityName;
        [SerializeField] private UnityEvent onHitEvent = new();
        [SerializeField] private UnityEvent onDeathEvent = new();

        private readonly Dictionary<Type, IEntityComponent> components = new();

        public UnityEvent OnHitEvent => onHitEvent;
        public UnityEvent OnDeathEvent => onDeathEvent;
        public string EntityName
        {
            get => entityName;
            set => entityName = value;
        }

        public bool IsDead { get; set; }
        public bool IsInvincible { get; private set; }

        protected virtual void Awake()
        {
            CacheComponents();
            InitializeComponents();
            AfterInitializeComponents();
        }

        public void EntityDestroy()
        {
            Destroy(gameObject);
        }

        protected virtual void CacheComponents()
        {
            components.Clear();

            IEntityComponent[] foundComponents = GetComponentsInChildren<IEntityComponent>(true);
            foreach (IEntityComponent component in foundComponents)
            {
                Type type = component.GetType();
                if (!components.ContainsKey(type))
                    components.Add(type, component);
            }
        }

        protected virtual void InitializeComponents()
        {
            foreach (IEntityComponent component in components.Values)
                component.Initialize(this);
        }

        protected virtual void AfterInitializeComponents()
        {
            foreach (IEntityComponent component in components.Values)
            {
                if (component is IAfterInitialize afterInitialize)
                    afterInitialize.AfterInitialize();
            }
        }

        public T GetCompo<T>() where T : class, IEntityComponent
        {
            foreach (IEntityComponent component in components.Values)
            {
                if (component is T targetComponent)
                    return targetComponent;
            }

            return null;
        }

        public bool TryGetCompo<T>(out T component) where T : class, IEntityComponent
        {
            component = GetCompo<T>();
            return component != null;
        }

        public T RequireCompo<T>() where T : class, IEntityComponent
        {
            T component = GetCompo<T>();
            if (component != null)
                return component;

            Debug.LogError($"{name} requires an entity component of type {typeof(T).Name}.", this);
            return null;
        }

        public IEntityComponent GetCompo(Type type)
        {
            if (type == null)
                return null;

            foreach (IEntityComponent component in components.Values)
            {
                if (type.IsInstanceOfType(component))
                    return component;
            }

            return null;
        }

        public bool TryGetCompo(Type type, out IEntityComponent component)
        {
            component = GetCompo(type);
            return component != null;
        }

        public void RotateToTarget(Vector3 targetPosition, bool isSmooth = false, float smoothRotateSpeed = 15f)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = isSmooth
                ? Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * smoothRotateSpeed)
                : targetRotation;
        }

        public void SetInvincible(bool isInvincible)
        {
            IsInvincible = isInvincible;
        }
    }
}
