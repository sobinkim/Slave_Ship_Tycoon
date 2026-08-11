using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Sprite _logBackgroundSprite;

        private readonly List<CurrencyGainLogItemView> _activeLogs = new();
        private readonly Stack<CurrencyGainLogItemView> _inactiveLogs = new();
        private readonly Queue<CurrencyAddedEvent> _pendingCurrencyEvents = new();

        private CurrencyHudPresenter _currencyHudPresenter;
        private Coroutine _spawnCoroutine;

        private void Awake()
        {
            _currencyHudPresenter = GetComponent<CurrencyHudPresenter>();

            if (_logRoot == null)
            {
                Debug.LogError($"{nameof(CurrencyGainLogController)} needs a log root.", this);
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
            if (_logRoot == null)
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
            GameObject itemObject = new GameObject(
                "CurrencyGainLogItem",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(CurrencyGainLogItemView)
            );

            RectTransform itemRectTransform = itemObject.GetComponent<RectTransform>();
            itemRectTransform.SetParent(_logRoot, false);
            itemRectTransform.anchorMin = new Vector2(0f, 0f);
            itemRectTransform.anchorMax = new Vector2(0f, 0f);
            itemRectTransform.pivot = new Vector2(0f, 0f);
            itemRectTransform.sizeDelta = new Vector2(280f, 54f);

            Image background = itemObject.GetComponent<Image>();
            background.sprite = _logBackgroundSprite;
            background.type = _logBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            Image icon = CreateIcon(itemRectTransform);
            TMP_Text amountText = CreateAmountText(itemRectTransform);
            CanvasGroup canvasGroup = itemObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            CurrencyGainLogItemView logItem = itemObject.GetComponent<CurrencyGainLogItemView>();
            logItem.SetReferences(icon, amountText, canvasGroup);
            itemObject.SetActive(false);
            return logItem;
        }

        private Image CreateIcon(RectTransform parent)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRectTransform = iconObject.GetComponent<RectTransform>();
            iconRectTransform.SetParent(parent, false);
            iconRectTransform.anchorMin = new Vector2(0f, 0.5f);
            iconRectTransform.anchorMax = new Vector2(0f, 0.5f);
            iconRectTransform.pivot = new Vector2(0f, 0.5f);
            iconRectTransform.anchoredPosition = new Vector2(12f, 0f);
            iconRectTransform.sizeDelta = new Vector2(32f, 32f);

            Image icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private TMP_Text CreateAmountText(RectTransform parent)
        {
            GameObject textObject = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
            textRectTransform.SetParent(parent, false);
            textRectTransform.anchorMin = new Vector2(0f, 0f);
            textRectTransform.anchorMax = new Vector2(1f, 1f);
            textRectTransform.offsetMin = new Vector2(56f, 0f);
            textRectTransform.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI amountText = textObject.GetComponent<TextMeshProUGUI>();
            amountText.font = TMP_Settings.defaultFontAsset;
            amountText.fontSize = 25f;
            amountText.color = Color.white;
            amountText.alignment = TextAlignmentOptions.MidlineLeft;
            amountText.raycastTarget = false;
            return amountText;
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
                float y = i * (logRectTransform.sizeDelta.y + _itemSpacing);
                _activeLogs[i].MoveTo(new Vector2(0f, y), _moveDuration);
            }
        }
    }
}
