using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class StageEncounterDirector : MonoBehaviour
    {
        [SerializeField] private Transform enemyFleetRoot;
        [SerializeField] private Transform enemyFleetRootDestination;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;

        [Header("적 함대 진입")]
        [SerializeField, Tooltip("정규 슬롯 뒤쪽(+X)에 더할 흩어짐 거리의 최소/최대값입니다.")]
        private Vector2 scatterOffsetXRange = new Vector2(0.10f, 1.05f);
        [SerializeField, Min(0f), Tooltip("정규 슬롯 위아래에 더할 흩어짐 거리의 최대값입니다.")]
        private float scatterOffsetY = 0.22f;

        [Header("적 함대 집결")]
        [SerializeField, Min(0f), Tooltip("함대 루트와 도착 지점의 X 거리가 이 값 이하가 되면 집결을 시작합니다.")]
        private float regroupStartDistance = 0.85f;
        [SerializeField, Tooltip("함선별로 정규 슬롯에 모이는 시간의 최소/최대값(초)입니다.")]
        private Vector2 regroupDurationRange = new Vector2(0.9f, 1.25f);
        [SerializeField, Min(0f), Tooltip("함선별 집결 시작 지연의 최대값(초)입니다.")]
        private float maxRegroupDelay = 0.25f;

        private sealed class RegroupingEnemy
        {
            public Enemy Owner;
            public Transform Parent;
            public int SpawnGeneration;
            public Vector3 OriginalLocalPosition;
            public Vector3 ScatteredLocalPosition;
            public float Delay;
            public float Duration;
            public bool IsComplete;
        }

        private readonly System.Random cosmeticRandom = new System.Random();
        private readonly List<RegroupingEnemy> regroupingEnemies = new List<RegroupingEnemy>();
        private Vector3 enemyFleetRootStartPosition;
        private BattleSpawnData currentBattleSpawnData;
        private float regroupElapsed;
        private bool isRegrouping;
        private bool _isPlaying;

        private void OnEnable()
        {
            if (enemyFleetRoot != null)
                enemyFleetRootStartPosition = enemyFleetRoot.position;

            Bus<StageBattleEndedEvent>.OnEvent += ResetEnemyFleetRootPosition;
        }

        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= ResetEnemyFleetRootPosition;
            CancelEncounterSequence();
        }

        private void Update()
        {
            if (!_isPlaying)
                return;

            if (enemyFleetRoot == null || enemyFleetRootDestination == null)
            {
                CancelEncounterSequence();
                return;
            }

            bool hasArrived = MoveEnemyFleetRoot();
            float remainingDistance = Mathf.Abs(enemyFleetRoot.position.x - enemyFleetRootDestination.position.x);
            if (!isRegrouping && remainingDistance <= Mathf.Max(0f, regroupStartDistance))
                isRegrouping = true;

            if (!isRegrouping)
                return;

            regroupElapsed += Time.deltaTime;
            bool hasRegrouped = UpdateRegrouping();
            if (hasArrived && hasRegrouped)
                CompleteEncounterSequence();
        }

        private bool MoveEnemyFleetRoot()
        {
            Vector3 currentPosition = enemyFleetRoot.position;
            Vector3 targetPosition = currentPosition;
            targetPosition.x = enemyFleetRootDestination.position.x;

            enemyFleetRoot.position = Vector3.MoveTowards(
                currentPosition,
                targetPosition,
                Mathf.Max(0f, moveSpeed) * Time.deltaTime
            );

            return Mathf.Approximately(enemyFleetRoot.position.x, targetPosition.x);
        }

        public void PlayEncounterSequence(BattleSpawnData battleSpawnData)
        {
            CancelEncounterSequence();

            if (enemyFleetRoot == null || enemyFleetRootDestination == null)
            {
                Debug.LogError("StageEncounterDirector requires an enemy fleet root and destination.", this);
                return;
            }

            enemyFleetRoot.position = enemyFleetRootStartPosition;
            currentBattleSpawnData = battleSpawnData;

            foreach (Enemy enemy in battleSpawnData.EnemySpawnData.Enemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead)
                    continue;

                Transform enemyTransform = enemy.transform;
                Vector3 originalLocalPosition = enemyTransform.localPosition;
                Vector3 offset = new Vector3(
                    RandomInRange(scatterOffsetXRange, 0f),
                    RandomBetween(-Mathf.Max(0f, scatterOffsetY), Mathf.Max(0f, scatterOffsetY)),
                    0f);

                RegroupingEnemy entry = new RegroupingEnemy
                {
                    Owner = enemy,
                    Parent = enemyTransform.parent,
                    SpawnGeneration = enemy.SpawnGeneration,
                    OriginalLocalPosition = originalLocalPosition,
                    ScatteredLocalPosition = originalLocalPosition + offset,
                    Delay = RandomBetween(0f, Mathf.Max(0f, maxRegroupDelay)),
                    Duration = RandomInRange(regroupDurationRange, 0.01f)
                };
                regroupingEnemies.Add(entry);
                enemyTransform.localPosition = entry.ScatteredLocalPosition;
            }

            _isPlaying = true;
            Bus<StartEncounterSequence>.Raise(new StartEncounterSequence());
        }

        private bool UpdateRegrouping()
        {
            bool allComplete = true;
            foreach (RegroupingEnemy entry in regroupingEnemies)
            {
                if (entry.IsComplete)
                    continue;

                if (!IsCurrentEnemy(entry))
                {
                    entry.IsComplete = true;
                    continue;
                }

                float progress = Mathf.Clamp01((regroupElapsed - entry.Delay) / entry.Duration);
                entry.Owner.transform.localPosition = Vector3.Lerp(
                    entry.ScatteredLocalPosition,
                    entry.OriginalLocalPosition,
                    Mathf.SmoothStep(0f, 1f, progress));

                if (progress >= 1f)
                {
                    entry.IsComplete = true;
                    entry.Owner.ChangeState(ShipStateType.Idle);
                }
                else
                {
                    allComplete = false;
                }
            }
            return allComplete;
        }

        private bool IsCurrentEnemy(RegroupingEnemy entry)
        {
            Enemy enemy = entry.Owner;
            return enemy != null && enemy.isActiveAndEnabled && !enemy.IsDead &&
                   enemy.SpawnGeneration == entry.SpawnGeneration &&
                   enemy.transform.parent == entry.Parent;
        }

        private void CompleteEncounterSequence()
        {
            BattleSpawnData battleSpawnData = currentBattleSpawnData;
            ClearEncounterState();
            Bus<StopEncounterSequence>.Raise(new StopEncounterSequence());
            Bus<BattleStartEvent>.Raise(new BattleStartEvent(battleSpawnData));
        }

        public void CancelEncounterSequence()
        {
            bool wasPlaying = _isPlaying;
            _isPlaying = false;
            foreach (RegroupingEnemy entry in regroupingEnemies)
            {
                if (!IsCurrentEnemy(entry))
                    continue;

                entry.Owner.transform.localPosition = entry.OriginalLocalPosition;
                if (!entry.IsComplete)
                {
                    entry.IsComplete = true;
                    entry.Owner.ChangeState(ShipStateType.Idle);
                }
            }
            ClearEncounterState();
            if (enemyFleetRoot != null)
                enemyFleetRoot.position = enemyFleetRootStartPosition;
            if (wasPlaying)
                Bus<StopEncounterSequence>.Raise(new StopEncounterSequence());
        }

        private void ClearEncounterState()
        {
            _isPlaying = false;
            isRegrouping = false;
            regroupElapsed = 0f;
            currentBattleSpawnData = default;
            regroupingEnemies.Clear();
        }

        private void ResetEnemyFleetRootPosition(StageBattleEndedEvent evt)
        {
            CancelEncounterSequence();
        }

        private float RandomInRange(Vector2 range, float minimum)
        {
            float lower = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
            float upper = Mathf.Max(lower, Mathf.Max(range.x, range.y));
            return RandomBetween(lower, upper);
        }

        private float RandomBetween(float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)cosmeticRandom.NextDouble());
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            scatterOffsetXRange = ClampRange(scatterOffsetXRange, 0f);
            scatterOffsetY = Mathf.Max(0f, scatterOffsetY);
            regroupStartDistance = Mathf.Max(0f, regroupStartDistance);
            regroupDurationRange = ClampRange(regroupDurationRange, 0.01f);
            maxRegroupDelay = Mathf.Max(0f, maxRegroupDelay);
        }

        private static Vector2 ClampRange(Vector2 range, float minimum)
        {
            float lower = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
            float upper = Mathf.Max(lower, Mathf.Max(range.x, range.y));
            return new Vector2(lower, upper);
        }
    }
}
