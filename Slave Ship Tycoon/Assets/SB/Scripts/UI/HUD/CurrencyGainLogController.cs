using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.UI.HUD
{
    public class CurrencyGainLogController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _preloadCount = 5;
        [SerializeField, Min(1)] private int _maxVisibleCount = 5;
        [SerializeField, Min(0f)] private float _visibleDuration = 1f;
        [SerializeField, Min(0.01f)] private float _fadeOutDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float _overflowFadeOutDuration = 0.1f;
        [SerializeField, Min(0f)] private float _itemSpacing = 8f;
        [SerializeField, Min(0f)] private float _spawnInterval = 0.1f;
        [SerializeField, Min(0.01f)] private float _moveDuration = 0.2f;
        [SerializeField] private RectTransform _logRoot;
        [SerializeField] private CurrencyGainLogItemView _logItemPrefab;
        [SerializeField] private CurrencyHudPresenter _currencyHudPresenter;

        private readonly List<CurrencyGainLogItemView> _activeLogs = new();
        private readonly Stack<CurrencyGainLogItemView> _inactiveLogs = new();
        private readonly Queue<CurrencyAddedEvent> _pendingCurrencyEvents = new();

        private Coroutine _spawnCoroutine;

        private void Awake()
        {
            if (_currencyHudPresenter == null)
                _currencyHudPresenter = GetComponent<CurrencyHudPresenter>();

            if (_logRoot == null || _logItemPrefab == null)
            {
                Debug.LogError($"{nameof(CurrencyGainLogController)} needs a log root and item prefab.", this);
                return;
            }

            PreloadLogs();
        }

        private void OnEnable()
        {
            Bus<CurrencyAddedEvent>.OnEvent += HandleCurrencyAdded;
        }

        private void OnDisable()
        {
            Bus<CurrencyAddedEvent>.OnEvent -= HandleCurrencyAdded;

            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            _pendingCurrencyEvents.Clear();
        }

        private void HandleCurrencyAdded(CurrencyAddedEvent evt)
        {
            if (_logRoot == null || _logItemPrefab == null)
                return;

            _pendingCurrencyEvents.Enqueue(evt);

            if (_spawnCoroutine == null)
                _spawnCoroutine = StartCoroutine(SpawnLogs());
        }

        private System.Collections.IEnumerator SpawnLogs()
        {
            while (_pendingCurrencyEvents.Count > 0)
            {
                SpawnLog(_pendingCurrencyEvents.Dequeue());

                if (_pendingCurrencyEvents.Count > 0)
                    yield return new WaitForSecondsRealtime(_spawnInterval);
            }

            _spawnCoroutine = null;
        }

        private void SpawnLog(CurrencyAddedEvent evt)
        {
            CurrencyGainLogItemView logItem = GetLogItem();
            Sprite icon = GetCurrencyIcon(evt.CurrencyType);

            _activeLogs.Insert(0, logItem);
            logItem.transform.SetAsLastSibling();
            RefreshLogPositions();
            logItem.Play(icon, evt.AddedAmount, _visibleDuration, _fadeOutDuration, () => ReleaseLogItem(logItem));

            while (_activeLogs.Count > _maxVisibleCount)
            {
                int oldestIndex = _activeLogs.Count - 1;
                CurrencyGainLogItemView oldestLog = _activeLogs[oldestIndex];
                _activeLogs.RemoveAt(oldestIndex);
                oldestLog.FadeOut(_overflowFadeOutDuration, () => ReleaseLogItem(oldestLog));
            }

            RefreshLogPositions();
        }

        private void PreloadLogs()
        {
            for (int i = 0; i < _preloadCount; i++)
                _inactiveLogs.Push(CreateLogItem());
        }

        private CurrencyGainLogItemView GetLogItem()
        {
            CurrencyGainLogItemView logItem = _inactiveLogs.Count > 0
                ? _inactiveLogs.Pop()
                : CreateLogItem();

            logItem.gameObject.SetActive(true);
            return logItem;
        }

        private CurrencyGainLogItemView CreateLogItem()
        {
            CurrencyGainLogItemView logItem = Instantiate(_logItemPrefab, _logRoot);
            logItem.gameObject.SetActive(false);
            return logItem;
        }

        private Sprite GetCurrencyIcon(CurrencyType currencyType)
        {
            if (_currencyHudPresenter != null && _currencyHudPresenter.TryGetCurrencyIcon(currencyType, out Sprite icon))
                return icon;

            return null;
        }

        private void ReleaseLogItem(CurrencyGainLogItemView logItem)
        {
            if (logItem == null || _inactiveLogs.Contains(logItem))
                return;

            _activeLogs.Remove(logItem);
            logItem.gameObject.SetActive(false);
            _inactiveLogs.Push(logItem);
            RefreshLogPositions();
        }

        private void RefreshLogPositions()
        {
            for (int i = 0; i < _activeLogs.Count; i++)
            {
                RectTransform logRectTransform = _activeLogs[i].transform as RectTransform;
                float itemHeight = logRectTransform.rect.height * logRectTransform.localScale.y;
                float y = i * (itemHeight + _itemSpacing);
                _activeLogs[i].MoveTo(new Vector2(0f, y), _moveDuration);
            }
        }
    }
}
