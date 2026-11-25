using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Pool generico y ligero de componentes/prefabs (particulas, proyectiles y elementos de UI reutilizables)
    /// para evitar llamadas repetidas a Instantiate/Destroy y picos de Garbage Collector en tiempo de ejecucion.
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        readonly T prefab;
        readonly Transform defaultParent;
        readonly Stack<T> inactive = new Stack<T>();
        readonly List<T> active = new List<T>();

        public int ActiveCount => active.Count;
        public int InactiveCount => inactive.Count;

        public ComponentPool(T prefab, Transform defaultParent = null, int initialCapacity = 0)
        {
            this.prefab = prefab;
            this.defaultParent = defaultParent;
            if (initialCapacity > 0) Prewarm(initialCapacity);
        }

        public void Prewarm(int count)
        {
            if (prefab == null) return;
            for (int i = 0; i < count; i++)
            {
                T inst = Object.Instantiate(prefab, defaultParent);
                inst.gameObject.SetActive(false);
                inactive.Push(inst);
            }
        }

        public T Get(Transform parent = null)
        {
            Transform targetParent = parent != null ? parent : defaultParent;
            T inst = null;
            while (inactive.Count > 0 && inst == null)
                inst = inactive.Pop();

            if (inst == null)
            {
                if (prefab == null) return null;
                inst = Object.Instantiate(prefab, targetParent);
            }
            else if (targetParent != null && inst.transform.parent != targetParent)
            {
                inst.transform.SetParent(targetParent, false);
            }

            inst.transform.SetAsLastSibling();
            inst.gameObject.SetActive(true);
            active.Add(inst);
            return inst;
        }

        public T Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            T inst = Get(parent);
            if (inst == null) return null;
            inst.transform.SetPositionAndRotation(position, rotation);
            return inst;
        }

        public void Release(T inst)
        {
            if (inst == null) return;
            if (active.Remove(inst))
            {
                inst.gameObject.SetActive(false);
                inactive.Push(inst);
            }
        }

        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                T inst = active[i];
                if (inst != null)
                {
                    inst.gameObject.SetActive(false);
                    inactive.Push(inst);
                }
            }
            active.Clear();
        }
    }

    /// <summary>
    /// Pool estatico para efectos de particulas y proyectiles reutilizables en escena.
    /// </summary>
    public static class GameObjectPool
    {
        static readonly Dictionary<int, Stack<GameObject>> pools = new Dictionary<int, Stack<GameObject>>();
        static readonly Dictionary<GameObject, int> instanceToKey = new Dictionary<GameObject, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pools.Clear();
            instanceToKey.Clear();
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            int key = prefab.GetInstanceID();
            GameObject inst = null;

            if (pools.TryGetValue(key, out var stack))
            {
                while (stack.Count > 0 && inst == null)
                    inst = stack.Pop();
            }

            if (inst == null)
            {
                inst = Object.Instantiate(prefab, position, rotation, parent);
                instanceToKey[inst] = key;
            }
            else
            {
                if (parent != null) inst.transform.SetParent(parent, false);
                inst.transform.SetPositionAndRotation(position, rotation);
                inst.SetActive(true);
            }
            return inst;
        }

        public static ParticleSystem SpawnParticle(ParticleSystem prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            GameObject go = Get(prefab.gameObject, position, rotation, parent);
            if (go == null) return null;
            var ps = go.GetComponent<ParticleSystem>();
            var autoRelease = go.GetComponent<PooledParticleEffect>();
            if (autoRelease == null) autoRelease = go.AddComponent<PooledParticleEffect>();
            autoRelease.Arm(ps);
            return ps;
        }

        public static void Release(GameObject instance)
        {
            if (instance == null) return;
            if (!instanceToKey.TryGetValue(instance, out int key))
                key = instance.GetInstanceID();

            instance.SetActive(false);
            if (!pools.TryGetValue(key, out var stack))
            {
                stack = new Stack<GameObject>();
                pools[key] = stack;
            }
            stack.Push(instance);
        }
    }

    /// <summary>
    /// Devuelve automaticamente un sistema de particulas o proyectil efimero a <see cref="GameObjectPool"/>
    /// cuando termina su emision o su tiempo de vida, sin destruir el GameObject.
    /// </summary>
    public sealed class PooledParticleEffect : MonoBehaviour
    {
        ParticleSystem ps;
        float releaseAt;

        public void Arm(ParticleSystem system, float fallbackLifetime = 2f)
        {
            ps = system != null ? system : GetComponent<ParticleSystem>();
            float duration = fallbackLifetime;
            if (ps != null)
            {
                var main = ps.main;
                duration = main.duration + main.startLifetime.constantMax + 0.1f;
                ps.Clear(true);
                ps.Play(true);
            }
            releaseAt = Time.time + Mathf.Max(0.1f, duration);
        }

        void Update()
        {
            if (Time.time < releaseAt) return;
            if (ps != null && ps.IsAlive(true)) return;
            GameObjectPool.Release(gameObject);
        }
    }
}
