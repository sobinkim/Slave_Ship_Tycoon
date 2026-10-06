using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ShipCargoVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer cratePrefab;
        [SerializeField, Tooltip("갑판의 각 열 시작점. 배열 순서대로 채운 뒤 다음 단으로 쌓습니다.")]
        private Transform[] columnBasePoints = new Transform[0];

        [Header("Stacking")]
        [SerializeField, Tooltip("각 열 시작점 기준 다음 단의 로컬 위치 간격.")]
        private Vector3 stackLocalOffset = new Vector3(0f, 0.08f, 0f);
        [SerializeField, Tooltip("뒤쪽 난간보다 높고 앞쪽 난간보다 낮은 정렬 순서.")]
        private int crateSortingOrder = 1;

        [Header("Loading animation")]
        [SerializeField, Min(0.01f)] private float dropSeconds = 0.24f;
        [SerializeField, Min(0f)] private float loadStaggerSeconds = 0.05f;
        [SerializeField, Min(0f)] private float dropLocalHeight = 0.12f;
        [SerializeField] private AnimationCurve dropProgressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float settleSeconds = 0.1f;
        [SerializeField] private Vector3 landingScaleMultiplier = new Vector3(1.06f, 0.9f, 1f);

        private readonly List<SpriteRenderer> crates = new List<SpriteRenderer>();
        private readonly List<Coroutine> loadRoutines = new List<Coroutine>();
        private CargoManager cargoManager;

        public int DisplayedCrateCount { get; private set; }
        public CargoManager CargoManager => cargoManager;

        private void Awake()
        {
            if (cratePrefab == null || columnBasePoints == null || columnBasePoints.Length == 0)
            {
                Debug.LogError("ShipCargoVisualizer requires a crate prefab and explicit column base points.", this);
                enabled = false;
                return;
            }

            foreach (Transform column in columnBasePoints)
            {
                if (column != null)
                    continue;

                Debug.LogError("ShipCargoVisualizer has an unassigned column base point.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            Bus<TransportCargoChangedEvent>.OnEvent += OnCargoChanged;
            RefreshCargo(false);
        }

        private void OnDisable()
        {
            Bus<TransportCargoChangedEvent>.OnEvent -= OnCargoChanged;
            StopAllCoroutines();

            for (int index = 0; index < crates.Count; index++)
            {
                loadRoutines[index] = null;
                crates[index].gameObject.SetActive(false);
            }

            DisplayedCrateCount = 0;
        }

        // Scene references cannot be stored on a prefab. StageSpawnManager binds
        // its explicitly assigned CargoManager after spawning or reusing the ship.
        public void BindCargo(CargoManager manager)
        {
            cargoManager = manager;
            if (isActiveAndEnabled)
                RefreshCargo(false);
        }

        private void OnCargoChanged(TransportCargoChangedEvent evt)
        {
            if (cargoManager != null && evt.CargoManager == cargoManager)
                RefreshCargo(true);
        }

        private void RefreshCargo(bool shouldAnimate)
        {
            if (cargoManager == null || cratePrefab == null ||
                columnBasePoints == null || columnBasePoints.Length == 0)
                return;

            int cargoCount = 0;
            foreach (CargoData cargo in cargoManager.TransportCargoData)
            {
                if (cargo != null && cargo.Item != null)
                    cargoCount += Mathf.Max(0, cargo.Amount);
            }

            int previousCount = DisplayedCrateCount;
            for (int index = cargoCount; index < previousCount; index++)
                HideCrate(index);

            for (int index = previousCount; index < cargoCount; index++)
            {
                SpriteRenderer crate = GetOrCreateCrate(index);
                crate.transform.localPosition = GetStackLocalPosition(index);
                crate.transform.localScale = cratePrefab.transform.localScale;
                crate.sortingOrder = crateSortingOrder;
                crate.enabled = true;
                crate.gameObject.SetActive(true);

                if (shouldAnimate)
                    loadRoutines[index] = StartCoroutine(LoadCrate(index, (index - previousCount) * loadStaggerSeconds));
            }

            DisplayedCrateCount = cargoCount;
        }

        private SpriteRenderer GetOrCreateCrate(int index)
        {
            if (index < crates.Count)
                return crates[index];

            Transform column = columnBasePoints[index % columnBasePoints.Length];
            SpriteRenderer crate = Instantiate(cratePrefab, column, false);
            crate.name = $"CargoCrate_{index + 1:00}";
            crates.Add(crate);
            loadRoutines.Add(null);
            return crate;
        }

        private Vector3 GetStackLocalPosition(int index)
        {
            int stackLevel = index / columnBasePoints.Length;
            return stackLocalOffset * stackLevel;
        }

        private void HideCrate(int index)
        {
            if (loadRoutines[index] != null)
            {
                StopCoroutine(loadRoutines[index]);
                loadRoutines[index] = null;
            }

            crates[index].gameObject.SetActive(false);
        }

        private IEnumerator LoadCrate(int index, float delaySeconds)
        {
            Transform crate = crates[index].transform;
            Vector3 targetLocalPosition = GetStackLocalPosition(index);
            Vector3 startLocalPosition = targetLocalPosition + Vector3.up * dropLocalHeight;
            Vector3 baseLocalScale = cratePrefab.transform.localScale;
            crate.localPosition = startLocalPosition;

            if (delaySeconds > 0f)
            {
                crates[index].enabled = false;
                yield return new WaitForSeconds(delaySeconds);
                crates[index].enabled = true;
            }

            float elapsedSeconds = 0f;
            while (elapsedSeconds < dropSeconds)
            {
                elapsedSeconds += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedSeconds / dropSeconds);
                crate.localPosition = Vector3.LerpUnclamped(startLocalPosition, targetLocalPosition,
                    dropProgressCurve.Evaluate(progress));
                yield return null;
            }

            crate.localPosition = targetLocalPosition;
            elapsedSeconds = 0f;
            while (elapsedSeconds < settleSeconds)
            {
                elapsedSeconds += Time.deltaTime;
                float squash = Mathf.Sin(Mathf.Clamp01(elapsedSeconds / settleSeconds) * Mathf.PI);
                crate.localScale = Vector3.Scale(baseLocalScale,
                    Vector3.Lerp(Vector3.one, landingScaleMultiplier, squash));
                yield return null;
            }

            crate.localScale = baseLocalScale;
            loadRoutines[index] = null;
        }
    }
}
