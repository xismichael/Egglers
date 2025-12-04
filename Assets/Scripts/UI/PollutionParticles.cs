using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Egglers
{
    public class PollutionParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem resistanceSystem;
        [SerializeField] private ParticleSystem attackSystem;
        [SerializeField] private ParticleSystem spreadSystem;
        [SerializeField] private float activeDuration = 1.5f;

        [Header("Emission multipliers")]
        [SerializeField] private float resistanceEmissionMultiplier = 2f;
        [SerializeField] private float attackEmissionMultiplier = 8f;
        [SerializeField] private float spreadEmissionMultiplier = 3f;

        [Header("Burst counts")]
        [SerializeField] private int resistanceBurstCount = 20;
        [SerializeField] private int attackBurstCount = 80;
        [SerializeField] private int spreadBurstCount = 15;

        private Coroutine plantDeathRoutine;
        private Coroutine pollutionDeathRoutine;
        private ParticleSystem[] systems;
        private float resistanceBaseRate;
        private float attackBaseRate;
        private float spreadBaseRate;

        private void Awake()
        {
            systems = GetAvailableSystems();
            CacheBaseEmissionRates();

            foreach (var ps in systems)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear();
            }
        }

        public void PlayPlantDeath()
        {
            systems = systems ?? GetAvailableSystems();
            List<ParticleSystem> targets = new List<ParticleSystem>();
            if (attackSystem != null) targets.Add(attackSystem);
            if (resistanceSystem != null) targets.Add(resistanceSystem);
            StartRoutine(ref plantDeathRoutine, targets, () => plantDeathRoutine = null);
        }

        public void PlayPollutionDeath()
        {
            systems = systems ?? GetAvailableSystems();
            List<ParticleSystem> targets = new List<ParticleSystem>();
            if (spreadSystem != null)
            {
                targets.Add(spreadSystem);
            }
            StartRoutine(ref pollutionDeathRoutine, targets, () => pollutionDeathRoutine = null);
        }

        private void StartRoutine(ref Coroutine routine, List<ParticleSystem> targets, Action clearAction)
        {
            if (targets == null || targets.Count == 0) return;

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(PlayRoutine(targets, clearAction));
        }

        private IEnumerator PlayRoutine(List<ParticleSystem> targets, Action onComplete)
        {
            foreach (var ps in targets)
            {
                if (ps == null) continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear();
                ApplyEmission(ps, true);
                EmitBurst(ps);
                ps.Play(true);
            }

            if (activeDuration > 0f)
            {
                yield return new WaitForSeconds(activeDuration);
            }
            else
            {
                yield return null;
            }

            foreach (var ps in targets)
            {
                if (ps == null) continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                ApplyEmission(ps, false);
            }

            onComplete?.Invoke();
        }

        private void ApplyEmission(ParticleSystem system, bool boosted)
        {
            if (system == null) return;

            var emission = system.emission;
            float baseRate = GetBaseRate(system);
            float multiplier = boosted ? GetMultiplier(system) : 1f;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(baseRate * multiplier);
        }

        private float GetMultiplier(ParticleSystem ps)
        {
            if (ps == attackSystem) return attackEmissionMultiplier;
            if (ps == resistanceSystem) return resistanceEmissionMultiplier;
            if (ps == spreadSystem) return spreadEmissionMultiplier;
            return 1f;
        }

        private void EmitBurst(ParticleSystem ps)
        {
            int burst = GetBurstCount(ps);
            if (ps != null && burst > 0)
            {
                ps.Emit(burst);
            }
        }

        private int GetBurstCount(ParticleSystem ps)
        {
            if (ps == attackSystem) return attackBurstCount;
            if (ps == resistanceSystem) return resistanceBurstCount;
            if (ps == spreadSystem) return spreadBurstCount;
            return 0;
        }

        private ParticleSystem[] GetAvailableSystems()
        {
            return new[]
            {
                resistanceSystem ?? transform.Find("Resistance")?.GetComponent<ParticleSystem>(),
                attackSystem ?? transform.Find("Attack")?.GetComponent<ParticleSystem>(),
                spreadSystem ?? transform.Find("Spread")?.GetComponent<ParticleSystem>()
            };
        }

        private void CacheBaseEmissionRates()
        {
            resistanceBaseRate = GetCurrentRate(resistanceSystem);
            attackBaseRate = GetCurrentRate(attackSystem);
            spreadBaseRate = GetCurrentRate(spreadSystem);
        }

        private float GetCurrentRate(ParticleSystem ps)
        {
            if (ps == null) return 0f;
            var emission = ps.emission;
            return emission.rateOverTime.constant;
        }

        private float GetBaseRate(ParticleSystem ps)
        {
            if (ps == attackSystem) return attackBaseRate;
            if (ps == resistanceSystem) return resistanceBaseRate;
            if (ps == spreadSystem) return spreadBaseRate;
            return 0f;
        }
    }
}

