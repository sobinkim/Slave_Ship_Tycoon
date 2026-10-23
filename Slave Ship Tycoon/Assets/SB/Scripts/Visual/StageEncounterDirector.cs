using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public enum EncounterPresentation { Standard, Transport, Boss, Pursuit }

    public class StageEncounterDirector : MonoBehaviour
    {
        [SerializeField] private Transform enemyFleetRoot;
        [SerializeField] private Transform enemyFleetRootDestination;
        [SerializeField] private ParallaxLayerManager parallaxLayers;
        [SerializeField, Min(0.01f)] private float moveSpeed = 2f;

        [Header("Arrival timing")]
        [SerializeField, Min(0.1f)] private float transportEntrySeconds = 1.1f;
        [SerializeField, Min(0.1f)] private float bossEntrySeconds = 1.7f;
        [SerializeField, Min(0.01f)] private float accelerationSeconds = 0.2f;
        [SerializeField, Min(0.01f)] private float arrivalBlendSeconds = 0.4f;
        [SerializeField, Min(0f), Tooltip("본체 뒤에서 호위가 집결을 시작하기까지의 최소 시간입니다.")]
        private float bossEscortLeadSeconds = 0.25f;
        [SerializeField, Min(0f), Tooltip("보스 호위를 진입 초반에 뒤로 밀어 두는 거리입니다. 최종 슬롯은 바꾸지 않습니다.")]
        private float bossEscortEntryOffsetX = 2.6f;

        [Header("적 함대 진입")]
        [SerializeField] private Vector2 scatterOffsetXRange = new Vector2(0.10f, 1.05f);
        [SerializeField, Min(0f)] private float scatterOffsetY = 0.22f;
        [Header("적 함대 집결")]
        [SerializeField, Min(0f)] private float regroupStartDistance = 0.85f;
        [SerializeField] private Vector2 regroupDurationRange = new Vector2(0.9f, 1.25f);
        [SerializeField, Min(0f)] private float maxRegroupDelay = 0.25f;

        private sealed class RegroupingEnemy
        {
            public Enemy Owner;
            public Transform Parent;
            public int SpawnGeneration;
            public Vector3 OriginalLocalPosition;
            public Vector3 ScatteredLocalPosition;
            public float StartSeconds;
            public float Duration;
        }

        private readonly System.Random cosmeticRandom = new System.Random();
        private readonly List<RegroupingEnemy> regroupingEnemies = new List<RegroupingEnemy>();
        private readonly List<Ship> playerShips = new List<Ship>();
        private Vector3 enemyFleetRootStartPosition;
        private bool hasFleetRootStartPosition;
        private BattleSpawnData currentBattleSpawnData;
        private float elapsed;
        private float entrySeconds;
        private bool departurePreview;
        private bool continuedSailing;
        private bool _isPlaying;
        public bool IsPlaying => _isPlaying;
        public bool IsDeparturePreviewPlaying => departurePreview;

        private void OnEnable()
        {
            hasFleetRootStartPosition = enemyFleetRoot != null;
            if (hasFleetRootStartPosition) enemyFleetRootStartPosition = enemyFleetRoot.position;
            Bus<StageBattleEndedEvent>.OnEvent += ResetEnemyFleetRootPosition;
        }

        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= ResetEnemyFleetRootPosition;
            CancelEncounterSequence();
            hasFleetRootStartPosition = false;
        }

        public void PlayDeparturePreview(List<Ship> ships)
        {
            if (_isPlaying || departurePreview) return;
            playerShips.Clear();
            foreach (Ship ship in ships)
                if (ship != null && !ship.IsDead)
                {
                    playerShips.Add(ship);
                    ship.ChangeState(ShipStateType.Move);
                }
            elapsed = 0f;
            departurePreview = true;
            Bus<StartEncounterSequence>.Raise(new StartEncounterSequence());
            ApplyTravelStrength(0f);
        }

        public void CancelDeparturePreview()
        {
            if (departurePreview) CancelEncounterSequence();
        }

        private void Update()
        {
            if (!_isPlaying && !departurePreview) return;
            elapsed += Time.deltaTime;
            if (departurePreview)
            {
                ApplyTravelStrength(Mathf.Clamp01(elapsed / Mathf.Max(0.01f, accelerationSeconds)));
                return;
            }
            if (enemyFleetRoot == null || enemyFleetRootDestination == null)
            {
                CancelEncounterSequence();
                return;
            }
            float progress = Mathf.Clamp01(elapsed / entrySeconds);
            float brakeFraction = Mathf.Min(1f, arrivalBlendSeconds / entrySeconds);
            float cruiseFraction = 1f - brakeFraction;
            float brakeProgress = Mathf.Clamp01((progress - cruiseFraction) / Mathf.Max(0.0001f, brakeFraction));
            float travelled = progress <= cruiseFraction ? progress
                : cruiseFraction + brakeFraction * (brakeProgress - brakeProgress * brakeProgress * 0.5f);
            travelled /= 1f - brakeFraction * 0.5f;
            Vector3 position = enemyFleetRootStartPosition;
            position.x = Mathf.Lerp(enemyFleetRootStartPosition.x, enemyFleetRootDestination.position.x, travelled);
            enemyFleetRoot.position = position;
            float launch = continuedSailing ? 1f : Mathf.Clamp01(elapsed / Mathf.Max(0.01f, accelerationSeconds));
            ApplyTravelStrength(launch * (1f - brakeProgress));
            foreach (RegroupingEnemy entry in regroupingEnemies)
            {
                if (!IsCurrentEnemy(entry)) continue;
                float blend = Mathf.Clamp01((elapsed - entry.StartSeconds) / entry.Duration);
                entry.Owner.transform.localPosition = Vector3.Lerp(entry.ScatteredLocalPosition,
                    entry.OriginalLocalPosition, Mathf.SmoothStep(0f, 1f, blend));
            }
            if (progress >= 1f) CompleteEncounterSequence();
        }

        public void PlayEncounterSequence(BattleSpawnData battleSpawnData,
            EncounterPresentation presentation = EncounterPresentation.Standard, Enemy focus = null)
        {
            bool wasSailing = departurePreview;
            CancelEncounterSequence();
            if (enemyFleetRoot == null || enemyFleetRootDestination == null)
            {
                Debug.LogError("StageEncounterDirector requires an enemy fleet root and destination.", this);
                return;
            }
            enemyFleetRoot.position = enemyFleetRootStartPosition;
            currentBattleSpawnData = battleSpawnData;
            continuedSailing = wasSailing;
            if (battleSpawnData.MainShip != null) playerShips.Add(battleSpawnData.MainShip);
            foreach (Ship ship in battleSpawnData.EscortShips)
                if (ship != null) playerShips.Add(ship);
            foreach (Ship ship in playerShips)
                if (!ship.IsDead) ship.ChangeState(ShipStateType.Move);
            float distance = Mathf.Abs(enemyFleetRootStartPosition.x - enemyFleetRootDestination.position.x);
            entrySeconds = presentation == EncounterPresentation.Transport ? transportEntrySeconds
                : presentation == EncounterPresentation.Boss ? bossEntrySeconds
                : distance / Mathf.Max(0.01f, moveSpeed);
            entrySeconds = Mathf.Max(0.1f, entrySeconds);
            float latestStart = Mathf.Max(0f, entrySeconds - Mathf.Max(regroupDurationRange.x, regroupDurationRange.y) - maxRegroupDelay);
            float distanceStart = distance > 0f ? Mathf.Clamp01(1f - regroupStartDistance / distance) * entrySeconds : 0f;
            float regroupStart = Mathf.Min(latestStart, distanceStart);
            foreach (Enemy enemy in battleSpawnData.EnemySpawnData.Enemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead) continue;
                bool isFocus = presentation == EncounterPresentation.Boss && enemy == focus;
                bool isBossEscort = presentation == EncounterPresentation.Boss && focus != null && !isFocus;
                Vector3 original = enemy.transform.localPosition;
                Vector3 offset = isFocus ? Vector3.zero : new Vector3(RandomInRange(scatterOffsetXRange, 0f),
                    RandomBetween(-scatterOffsetY, scatterOffsetY), 0f);
                if (isBossEscort) offset.x += bossEscortEntryOffsetX;
                float start = isFocus ? 0f : regroupStart + RandomBetween(0f, maxRegroupDelay);
                if (isBossEscort) start = Mathf.Max(start, bossEscortLeadSeconds);
                start = Mathf.Min(start, entrySeconds - 0.01f);
                var entry = new RegroupingEnemy {
                    Owner = enemy, Parent = enemy.transform.parent, SpawnGeneration = enemy.SpawnGeneration,
                    OriginalLocalPosition = original, ScatteredLocalPosition = original + offset,
                    StartSeconds = start, Duration = Mathf.Min(RandomInRange(regroupDurationRange, 0.01f), entrySeconds - start)
                };
                regroupingEnemies.Add(entry);
                enemy.transform.localPosition = entry.ScatteredLocalPosition;
                enemy.ChangeState(ShipStateType.Move);
            }
            elapsed = 0f;
            _isPlaying = true;
            Bus<StartEncounterSequence>.Raise(new StartEncounterSequence());
            ApplyTravelStrength(wasSailing ? 1f : 0f);
        }

        private void ApplyTravelStrength(float strength)
        {
            parallaxLayers?.SetTravelStrength(strength);
            foreach (Ship ship in playerShips)
                if (ship != null && !ship.IsDead) ship.SetSailingPresentation(strength);
            foreach (RegroupingEnemy entry in regroupingEnemies)
                if (IsCurrentEnemy(entry)) entry.Owner.SetSailingPresentation(strength);
        }

        private bool IsCurrentEnemy(RegroupingEnemy entry) => entry.Owner != null && entry.Owner.isActiveAndEnabled &&
            !entry.Owner.IsDead && entry.Owner.SpawnGeneration == entry.SpawnGeneration && entry.Owner.transform.parent == entry.Parent;

        private void CompleteEncounterSequence()
        {
            BattleSpawnData battle = currentBattleSpawnData;
            ApplyTravelStrength(0f);
            RestoreShips();
            ClearEncounterState();
            Bus<StopEncounterSequence>.Raise(new StopEncounterSequence());
            Bus<BattleStartEvent>.Raise(new BattleStartEvent(battle));
        }

        public void CancelEncounterSequence()
        {
            bool wasPlaying = _isPlaying || departurePreview;
            if (wasPlaying) ApplyTravelStrength(0f);
            RestoreShips();
            ClearEncounterState();
            if (enemyFleetRoot != null && hasFleetRootStartPosition) enemyFleetRoot.position = enemyFleetRootStartPosition;
            if (wasPlaying) Bus<StopEncounterSequence>.Raise(new StopEncounterSequence());
        }

        private void RestoreShips()
        {
            foreach (RegroupingEnemy entry in regroupingEnemies)
                if (IsCurrentEnemy(entry))
                {
                    entry.Owner.transform.localPosition = entry.OriginalLocalPosition;
                    entry.Owner.ClearSailingPresentation();
                    entry.Owner.ChangeState(ShipStateType.Idle);
                }
            foreach (Ship ship in playerShips)
                if (ship != null)
                {
                    ship.ClearSailingPresentation();
                    if (!ship.IsDead) ship.ChangeState(ShipStateType.Idle);
                }
        }

        private void ClearEncounterState()
        {
            _isPlaying = false;
            departurePreview = false;
            elapsed = 0f;
            currentBattleSpawnData = default;
            regroupingEnemies.Clear();
            playerShips.Clear();
        }

        private void ResetEnemyFleetRootPosition(StageBattleEndedEvent evt) => CancelEncounterSequence();

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            transportEntrySeconds = Mathf.Max(0.1f, transportEntrySeconds);
            bossEntrySeconds = Mathf.Max(0.1f, bossEntrySeconds);
            accelerationSeconds = Mathf.Max(0.01f, accelerationSeconds);
            arrivalBlendSeconds = Mathf.Max(0.01f, arrivalBlendSeconds);
            maxRegroupDelay = Mathf.Max(0f, maxRegroupDelay);
            scatterOffsetY = Mathf.Max(0f, scatterOffsetY);
            regroupStartDistance = Mathf.Max(0f, regroupStartDistance);
            scatterOffsetXRange = ClampRange(scatterOffsetXRange, 0f);
            regroupDurationRange = ClampRange(regroupDurationRange, 0.01f);
        }

        private static Vector2 ClampRange(Vector2 range, float minimum) => new Vector2(
            Mathf.Max(minimum, Mathf.Min(range.x, range.y)), Mathf.Max(minimum, Mathf.Max(range.x, range.y)));
        private float RandomInRange(Vector2 range, float minimum) => RandomBetween(Mathf.Max(minimum, Mathf.Min(range.x, range.y)), Mathf.Max(minimum, Mathf.Max(range.x, range.y)));
        private float RandomBetween(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)cosmeticRandom.NextDouble());
    }
}
