using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.AttackCompo;
using SB.Scripts.Projectiles;
using UnityEngine;

namespace SB.Scripts
{
    public enum MainShipDefeatPhase
    {
        WaitingForBattle,
        Combat,
        PreparingSalvo,
        SalvoInFlight,
        ShowingDefeat,
        Defeated
    }

    [DisallowMultipleComponent]
    public sealed class MainShipDefeatDirector : MonoBehaviour
    {
        [Header("Final salvo")]
        [SerializeField] private NormalProjectile salvoProjectilePrefab;
        [SerializeField] private GameObject salvoMuzzleEffectPrefab;
        [SerializeField, Min(0f), Tooltip("기존 공격 중단 후 일제사격까지의 대기 시간.")]
        private float preparationSeconds = 0.35f;
        [SerializeField, Min(0.05f), Tooltip("모든 적 포탄이 같은 시간에 도착할 비행 시간.")]
        private float salvoFlightSeconds = 0.7f;
        [SerializeField, Min(0f)] private float salvoArcHeight = 0.65f;
        [SerializeField, Min(0.05f), Tooltip("포탄 비행 시간에 더하는 수명 여유.")]
        private float projectileLifeMarginSeconds = 0.5f;
        [SerializeField] private string enemyAttackStateName = "Attack";

        [Header("Defeat timing")]
        [SerializeField, Min(0f), Tooltip("전탄 명중 후 메인선이 사라진 상태를 보여주고 패배 처리까지 기다리는 시간.")]
        private float defeatHoldSeconds = 0.8f;

        private readonly HashSet<BaseProjectile> pendingShots = new HashSet<BaseProjectile>();
        private BattleSpawnData battle;
        private Coroutine sequence;
        private bool wasSalvoInterrupted;
        private int mainShipSpawnGeneration;

        public MainShipDefeatPhase Phase { get; private set; }
        public int SalvoShotCount { get; private set; }
        public int PendingShotCount => pendingShots.Count;
        public int SalvoImpactCount { get; private set; }
        public int SalvoLaunchFrame { get; private set; } = -1;

        private void Awake()
        {
            if (salvoProjectilePrefab != null)
                return;

            Debug.LogError("MainShipDefeatDirector requires an explicit salvo projectile prefab.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            Bus<StageBattleInitializedEvent>.OnEvent += HandleBattleInitialized;
            Bus<BattleStartEvent>.OnEvent += HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        }

        private void OnDisable()
        {
            Bus<StageBattleInitializedEvent>.OnEvent -= HandleBattleInitialized;
            Bus<BattleStartEvent>.OnEvent -= HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            ResetSequence();
        }

        private void HandleBattleInitialized(StageBattleInitializedEvent evt)
        {
            ResetSequence();
            battle = evt.BattleSpawnData;
        }

        private void HandleBattleStarted(BattleStartEvent evt)
        {
            ResetSequence();
            battle = evt.BattleSpawnData;
            if (battle.MainShip == null || battle.MainShip.Destruction == null)
                return;

            mainShipSpawnGeneration = battle.MainShip.SpawnGeneration;
            SalvoShotCount = 0;
            SalvoImpactCount = 0;
            SalvoLaunchFrame = -1;

            // Creatures keep attacking the main ship directly after the escorts fall.
            // Their encounter does not use the enemy fleet's cannon defeat sequence.
            foreach (Enemy enemy in battle.EnemySpawnData.Enemies)
                if (enemy is SeaCreatureEnemy)
                    return;

            Phase = MainShipDefeatPhase.Combat;
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt)
        {
            ResetSequence();
        }

        // LateUpdate observes all ship death/state updates before taking ownership
        // of the defeat sequence; normal combat stays in the existing state machines.
        private void LateUpdate()
        {
            if (Phase != MainShipDefeatPhase.Combat || !IsCurrentMainShipAlive())
                return;

            foreach (Ship escort in battle.EscortShips)
                if (IsAlive(escort))
                    return;

            bool hasLivingEnemy = false;
            foreach (Enemy enemy in battle.EnemySpawnData.Enemies)
                hasLivingEnemy |= IsAlive(enemy);
            if (!hasLivingEnemy)
                return;

            sequence = StartCoroutine(PlayFinalSalvo());
        }

        private IEnumerator PlayFinalSalvo()
        {
            Phase = MainShipDefeatPhase.PreparingSalvo;
            foreach (Enemy enemy in battle.EnemySpawnData.Enemies)
                if (IsAlive(enemy))
                    enemy.GetCompo<Base_ShipAttackCompo>()?.SuspendCombat();

            Bus<MainShipFinalSalvoStartedEvent>.Raise(new MainShipFinalSalvoStartedEvent(battle.MainShip));
            if (preparationSeconds > 0f)
                yield return new WaitForSeconds(preparationSeconds);
            if (!IsCurrentMainShipAlive())
                yield break;

            Phase = MainShipDefeatPhase.SalvoInFlight;
            SalvoLaunchFrame = Time.frameCount;
            foreach (Enemy enemy in battle.EnemySpawnData.Enemies)
            {
                if (!IsAlive(enemy))
                    continue;

                ArcProjectileAttackCompo weapon = enemy.GetCompo<ArcProjectileAttackCompo>();
                if (weapon == null || !weapon.TryGetFirePosition(out Vector3 startPosition))
                {
                    Debug.LogError("Final salvo enemy requires an arc weapon with an assigned fire point.", enemy);
                    wasSalvoInterrupted = true;
                    continue;
                }

                Vector3 targetPosition = battle.MainShip.GetCompo<ShipProjectileAnchorCompo>().HitPosition;
                var flight = new ProjectileFlightData
                {
                    FlightSpeed = Mathf.Max(0.01f, Vector3.Distance(startPosition, targetPosition) / salvoFlightSeconds),
                    ArcHeight = salvoArcHeight,
                    MinimumFlightDuration = salvoFlightSeconds,
                    LifeTime = salvoFlightSeconds + projectileLifeMarginSeconds
                };
                NormalProjectile projectile = PoolingManager.Instance.Get(
                    salvoProjectilePrefab, startPosition, Quaternion.identity);
                pendingShots.Add(projectile);
                projectile.Resolved += HandleShotResolved;
                SalvoShotCount++;
                weapon.PlayFinalSalvoAnimation(enemyAttackStateName);
                if (salvoMuzzleEffectPrefab != null)
                {
                    Vector3 direction = targetPosition - startPosition;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    PoolingManager.Instance.Get(salvoMuzzleEffectPrefab, startPosition,
                        Quaternion.Euler(0f, 0f, angle));
                }
                projectile.Launch(new ProjectileLaunchData(
                    battle.MainShip, startPosition, targetPosition, targetPosition,
                    flight, 0f, ProjectileHitMode.Guaranteed));
            }

            while (pendingShots.Count > 0)
                yield return null;
            if (wasSalvoInterrupted || SalvoShotCount == 0 || !IsCurrentMainShipAlive())
            {
                Phase = MainShipDefeatPhase.WaitingForBattle;
                sequence = null;
                yield break;
            }

            Phase = MainShipDefeatPhase.ShowingDefeat;
            battle.MainShip.Destruction.BeginDefeatPresentation();
            if (defeatHoldSeconds > 0f)
                yield return new WaitForSeconds(defeatHoldSeconds);
            if (IsCurrentMainShipAlive())
            {
                Phase = MainShipDefeatPhase.Defeated;
                battle.MainShip.Destruction.CompleteDefeat();
            }
            sequence = null;
        }

        private void HandleShotResolved(BaseProjectile projectile, ProjectileResolution resolution)
        {
            if (!pendingShots.Remove(projectile))
                return;

            projectile.Resolved -= HandleShotResolved;
            if (resolution == ProjectileResolution.TargetHit)
            {
                SalvoImpactCount++;
                battle.MainShip.Destruction.PlayImpact(projectile.transform.position);
            }
            else
                wasSalvoInterrupted = true;
        }

        private void ResetSequence()
        {
            Phase = MainShipDefeatPhase.WaitingForBattle;
            if (sequence != null)
                StopCoroutine(sequence);
            sequence = null;

            foreach (BaseProjectile projectile in pendingShots)
            {
                if (projectile == null)
                    continue;
                projectile.Resolved -= HandleShotResolved;
                projectile.CancelFlight();
            }
            pendingShots.Clear();
            wasSalvoInterrupted = false;
        }

        private bool IsCurrentMainShipAlive() =>
            IsAlive(battle.MainShip) && battle.MainShip.SpawnGeneration == mainShipSpawnGeneration;

        private static bool IsAlive(Ship ship) =>
            ship != null && ship.isActiveAndEnabled && !ship.IsDead;
    }
}
