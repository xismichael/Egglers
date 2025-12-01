using UnityEngine;

namespace Egglers
{
    /// <summary>
    /// Simple controller that adjusts particle emission/size/lifetime for pollution stats.
    /// </summary>
    public class PollutionParticles : MonoBehaviour
    {
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem resistanceSystem;
        [SerializeField] private ParticleSystem attackSystem;
        [SerializeField] private ParticleSystem spreadSystem;

        private ParticleSystem.EmissionModule resistanceEmission;
        private ParticleSystem.EmissionModule attackEmission;
        private ParticleSystem.EmissionModule spreadEmission;

        private ParticleSystem.MainModule resistanceMain;
        private ParticleSystem.MainModule attackMain;
        private ParticleSystem.MainModule spreadMain;

        void Awake()
        {
            if (resistanceSystem != null)
            {
                resistanceEmission = resistanceSystem.emission;
                resistanceMain = resistanceSystem.main;
            }
            if (attackSystem != null)
            {
                attackEmission = attackSystem.emission;
                attackMain = attackSystem.main;
            }
            if (spreadSystem != null)
            {
                spreadEmission = spreadSystem.emission;
                spreadMain = spreadSystem.main;
            }

            DisableVisuals();
        }

        public void SetResistanceIntensity(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            resistanceEmission.rateOverTime = Mathf.Lerp(0f, 5f, normalized);
            resistanceMain.startSize = Mathf.Lerp(1.0f, 1.5f, normalized);
            resistanceMain.startLifetime = Mathf.Lerp(2.5f, 5.0f, normalized);
        }

        public void SetAttackIntensity(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            attackEmission.rateOverTime = Mathf.Lerp(0f, 30f, normalized);
            attackMain.startSize = Mathf.Lerp(0.1f, 0.8f, normalized);
            attackMain.startLifetime = Mathf.Lerp(0.6f, 2.2f, normalized);
        }

        public void SetSpreadIntensity(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            spreadEmission.rateOverTime = Mathf.Lerp(0f, 25f, normalized);
            spreadMain.startSize = Mathf.Lerp(0.05f, 0.6f, normalized);
            spreadMain.startLifetime = Mathf.Lerp(0.2f, 1.6f, normalized);
        }

        public void EnableVisuals()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void DisableVisuals()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

