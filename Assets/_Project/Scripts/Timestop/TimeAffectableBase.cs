using UnityEngine;

namespace Reloj.TimeStop
{
    public abstract class TimeAffectableBase : MonoBehaviour, ITimeAffectable
    {
        [Header("Que congelar (se detecta solo)")]
        [SerializeField] bool affectRigidbody = true;
        [SerializeField] bool affectAnimators = true;
        [SerializeField] bool affectParticles = true;

        Rigidbody rb;
        Animator[] animators;
        float[] animatorSpeeds;
        ParticleSystem[] particles;
        bool[] particleWasPlaying;

        bool rbWasKinematic;
        Vector3 savedLinear;
        Vector3 savedAngular;

        public bool IsFrozen { get; private set; }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animators = GetComponentsInChildren<Animator>(true);
            particles = GetComponentsInChildren<ParticleSystem>(true);
            animatorSpeeds = new float[animators.Length];
            particleWasPlaying = new bool[particles.Length];
        }

        protected virtual void OnEnable()
        {
            TimeStopController.Register(this);
        }

        protected virtual void OnDisable()
        {
            TimeStopController.Unregister(this);
            if (IsFrozen) Unfreeze(); // evita dejar un Rigidbody kinematic para siempre
        }

        public void Freeze()
        {
            if (IsFrozen) return;
            IsFrozen = true;

            if (affectRigidbody && rb != null)
            {
                rbWasKinematic = rb.isKinematic;
                if (!rbWasKinematic)
                {
                    // Guardar ANTES de volverlo kinematic (un kinematic reporta velocidad 0).
                    savedLinear = rb.linearVelocity;
                    savedAngular = rb.angularVelocity;
                    rb.isKinematic = true;
                }
            }

            if (affectAnimators)
            {
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null) continue;
                    animatorSpeeds[i] = animators[i].speed;
                    animators[i].speed = 0f;
                }
            }

            if (affectParticles)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i] == null) continue;
                    particleWasPlaying[i] = particles[i].isPlaying;
                    particles[i].Pause(true);
                }
            }

            OnFrozen();
        }

        public void Unfreeze()
        {
            if (!IsFrozen) return;
            IsFrozen = false;

            if (affectRigidbody && rb != null && !rbWasKinematic)
            {
                rb.isKinematic = false;
                rb.linearVelocity = savedLinear;
                rb.angularVelocity = savedAngular;
            }

            if (affectAnimators)
            {
                for (int i = 0; i < animators.Length; i++)
                    if (animators[i] != null) animators[i].speed = animatorSpeeds[i];
            }

            if (affectParticles)
            {
                for (int i = 0; i < particles.Length; i++)
                    if (particles[i] != null && particleWasPlaying[i]) particles[i].Play(true);
            }

            OnUnfrozen();
        }

        protected virtual void OnFrozen() { }
        protected virtual void OnUnfrozen() { }
    }
}