using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Fleet.Effects
{
    [DisallowMultipleComponent]
    public sealed class FleetEffectStatusIcons : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FleetEffectReceiver receiver;
        [SerializeField] private SpriteRenderer iconTemplate;
        [Header("Layout in local units")]
        [SerializeField, Min(.01f), Tooltip("아이콘의 최대 가로/세로 크기")]
        private float iconSize = .1f;
        [SerializeField, Min(0f), Tooltip("아이콘 사이 간격")]
        private float iconSpacing = .025f;
        private readonly List<FleetEffectDefinition> effects = new List<FleetEffectDefinition>();
        private readonly List<SpriteRenderer> icons = new List<SpriteRenderer>();

        private void OnEnable()
        {
            if (receiver == null || iconTemplate == null)
            {
                Debug.LogError("FleetEffectStatusIcons requires receiver and icon template references.", this);
                return;
            }
            iconTemplate.gameObject.SetActive(false);
            receiver.EffectsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.EffectsChanged -= Refresh;
            foreach (SpriteRenderer icon in icons)
                if (icon != null) icon.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            receiver.CollectReceivedEffects(effects);
            int visibleCount = 0;
            foreach (FleetEffectDefinition effect in effects)
            {
                if (effect.StatusIcon == null) continue;
                if (visibleCount == icons.Count)
                {
                    SpriteRenderer newIcon = Instantiate(iconTemplate, transform);
                    newIcon.name = "EffectIcon";
                    icons.Add(newIcon);
                }
                SpriteRenderer icon = icons[visibleCount];
                icon.sprite = effect.StatusIcon;
                icon.color = effect.StatusIconColor;
                float spriteExtent = Mathf.Max(icon.sprite.bounds.size.x, icon.sprite.bounds.size.y);
                icon.transform.localScale = Vector3.one * (iconSize / Mathf.Max(.001f, spriteExtent));
                icon.transform.localPosition = new Vector3(visibleCount * (iconSize + iconSpacing), 0f, 0f);
                icon.gameObject.SetActive(true);
                visibleCount++;
            }
            for (int i = visibleCount; i < icons.Count; i++) icons[i].gameObject.SetActive(false);
        }
    }
}
