using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace SB.UI.Shared
{
    [Serializable]
    public sealed class EntryTransitionPreset
    {
        [SerializeField] private string warningText;
        [SerializeField] private string caption;
        [SerializeField] private Color bandColor;
        [SerializeField, Tooltip("전용 아이콘이 없으면 비워 두세요. 아이콘과 여백을 함께 숨깁니다.")]
        private Texture2D icon;
        [SerializeField, Min(0f)] private float holdSeconds;

        public string WarningText => warningText;
        public string Caption => caption;
        public Color BandColor => bandColor;
        public Texture2D Icon => icon;
        public float HoldSeconds => Mathf.Max(0f, holdSeconds);

        public EntryTransitionPreset(string warningText, string caption, Color bandColor, float holdSeconds)
        {
            this.warningText = warningText;
            this.caption = caption;
            this.bandColor = bandColor;
            this.holdSeconds = holdSeconds;
        }
    }

    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class ScreenEntryTransition : MonoBehaviour
    {
        [Header("Explicit UI references")]
        [SerializeField] private UIDocument document;
        [SerializeField, Tooltip("연출 중 숨길 다른 화면입니다. 컴포넌트를 끄지 않아 화면 상태와 이벤트 구독을 유지합니다.")]
        private UIDocument[] hiddenDocuments = Array.Empty<UIDocument>();
        [SerializeField, Tooltip("연출 중 렌더링과 GraphicRaycaster 입력을 함께 끌 기존 Canvas입니다.")]
        private Canvas[] hiddenCanvases = Array.Empty<Canvas>();
        [Header("Motion (unscaled seconds)")]
        [SerializeField, Min(0.01f)] private float enterSeconds = 0.28f;
        [SerializeField, Min(0.01f)] private float exitSeconds = 0.28f;
        [SerializeField, Min(0.1f)] private float breathingSeconds = 0.65f;
        [SerializeField, Range(0f, 1f)] private float minimumBandAlpha = 0.42f;

        private readonly List<ElementState> hiddenElements = new List<ElementState>();
        private readonly List<BehaviourState> hiddenBehaviours = new List<BehaviourState>();
        private VisualElement overlay, topShutter, bottomShutter, band, entryIcon;
        private Label warningLabel, captionLabel;
        private Coroutine routine;
        public bool IsPlaying { get; private set; }

        private readonly struct ElementState
        {
            public readonly VisualElement Element;
            public readonly StyleEnum<DisplayStyle> Display;
            public readonly bool Enabled;
            public ElementState(VisualElement element)
            {
                Element = element;
                Display = element.style.display;
                Enabled = element.enabledSelf;
            }
        }

        private readonly struct BehaviourState
        {
            public readonly Behaviour Target;
            public readonly bool Enabled;
            public BehaviourState(Behaviour target) { Target = target; Enabled = target.enabled; }
        }

        private void OnEnable()
        {
            if (document == null) return;
            VisualElement root = document.rootVisualElement;
            overlay = root.Q("entryTransition");
            topShutter = root.Q("entryTopShutter");
            bottomShutter = root.Q("entryBottomShutter");
            band = root.Q("entryWarningBand");
            entryIcon = root.Q("entryIcon");
            warningLabel = root.Q<Label>("entryWarning");
            captionLabel = root.Q<Label>("entryCaption");
        }

        private void OnDisable() => Cancel();

        public bool Play(EntryTransitionPreset preset)
        {
            if (!isActiveAndEnabled || preset == null || overlay == null || topShutter == null ||
                bottomShutter == null || band == null || entryIcon == null || warningLabel == null || captionLabel == null)
                return false;

            Cancel();
            routine = StartCoroutine(Present(preset));
            return true;
        }

        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            Restore();
        }

        private IEnumerator Present(EntryTransitionPreset preset)
        {
            try
            {
                IsPlaying = true;
                HideOtherUi();
                warningLabel.text = preset.WarningText;
                captionLabel.text = preset.Caption;
                band.style.backgroundColor = preset.BandColor;
                band.style.opacity = 1f;
                entryIcon.style.backgroundImage = preset.Icon != null ? new StyleBackground(preset.Icon) : StyleKeyword.None;
                entryIcon.EnableInClassList("hidden", preset.Icon == null);
                overlay.RemoveFromClassList("hidden");
                overlay.BringToFront();
                yield return MoveShutters(true, enterSeconds);
                float elapsed = 0f;
                while (elapsed < preset.HoldSeconds)
                {
                    float breath = 0.5f + 0.5f * Mathf.Cos(elapsed * Mathf.PI * 2f / Mathf.Max(0.1f, breathingSeconds));
                    band.style.opacity = Mathf.Lerp(minimumBandAlpha, 1f, breath);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                RevealOtherUi();
                yield return MoveShutters(false, exitSeconds);
            }
            finally
            {
                routine = null;
                Restore();
            }
        }

        private IEnumerator MoveShutters(bool entering, float seconds)
        {
            float elapsed = 0f;
            SetShutterOffset(entering ? 1f : 0f);
            while (elapsed < seconds)
            {
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, seconds));
                SetShutterOffset(entering ? Mathf.Pow(1f - progress, 3f) : Mathf.SmoothStep(0f, 1f, progress));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetShutterOffset(entering ? 0f : 1f);
            // Render the final pose before hiding the input-blocking layer.
            yield return null;
        }

        private void SetShutterOffset(float offset)
        {
            topShutter.style.translate = new Translate(0, Length.Percent(-102f * offset), 0);
            bottomShutter.style.translate = new Translate(0, Length.Percent(102f * offset), 0);
        }

        private void HideOtherUi()
        {
            foreach (UIDocument target in hiddenDocuments)
                if (target != null && target != document) Hide(target.rootVisualElement);
            // This document also hosts settlement and stage feedback. Keep only the transition visible.
            foreach (VisualElement sibling in overlay.parent.Children())
                if (sibling != overlay) Hide(sibling);
            foreach (Canvas canvas in hiddenCanvases)
            {
                if (canvas == null) continue;
                Hide(canvas);
                foreach (GraphicRaycaster raycaster in canvas.GetComponents<GraphicRaycaster>()) Hide(raycaster);
            }
        }

        private void Hide(VisualElement element)
        {
            if (element == null) return;
            foreach (ElementState state in hiddenElements) if (state.Element == element) return;
            hiddenElements.Add(new ElementState(element));
            element.SetEnabled(false);
            element.style.display = DisplayStyle.None;
        }

        private void Hide(Behaviour target)
        {
            foreach (BehaviourState state in hiddenBehaviours) if (state.Target == target) return;
            hiddenBehaviours.Add(new BehaviourState(target));
            target.enabled = false;
        }

        private void RevealOtherUi()
        {
            foreach (ElementState state in hiddenElements)
            {
                state.Element.style.display = state.Display;
                state.Element.SetEnabled(state.Enabled);
            }
            foreach (BehaviourState state in hiddenBehaviours)
                if (state.Target is Canvas canvas && canvas != null) canvas.enabled = state.Enabled;
        }

        private void Restore()
        {
            if (overlay != null) overlay.AddToClassList("hidden");
            RevealOtherUi();
            hiddenElements.Clear();
            foreach (BehaviourState state in hiddenBehaviours)
                if (state.Target != null) state.Target.enabled = state.Enabled;
            hiddenBehaviours.Clear();
            IsPlaying = false;
        }
    }
}
