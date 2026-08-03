using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class ParallaxAlphaController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] spriteRenderers;
        [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;
        [SerializeField, Min(0f)] private float fadeInSpeed = 8f;
        [SerializeField, Min(0f)] private float fadeOutSpeed = 3f;
        [SerializeField] private bool hideOnAwake = true;

        private float _currentAlpha;
        private float _targetAlpha;

        private void Awake()
        {
            CacheSpriteRenderersIfNeeded();

            _currentAlpha = hideOnAwake ? hiddenAlpha : visibleAlpha;
            _targetAlpha = _currentAlpha;
            ApplyAlpha(_currentAlpha);
        }

        private void OnEnable()
        {
            Bus<StartEncounterSequence>.OnEvent += HandleStartEncounterSequence;
            Bus<StopEncounterSequence>.OnEvent += HandleStopEncounterSequence;
        }

        private void OnDisable()
        {
            Bus<StartEncounterSequence>.OnEvent -= HandleStartEncounterSequence;
            Bus<StopEncounterSequence>.OnEvent -= HandleStopEncounterSequence;
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentAlpha, _targetAlpha))
                return;

            float speed = _targetAlpha > _currentAlpha ? fadeInSpeed : fadeOutSpeed;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, speed * Time.deltaTime);
            ApplyAlpha(_currentAlpha);
        }

        private void HandleStartEncounterSequence(StartEncounterSequence evt)
        {
            _targetAlpha = visibleAlpha;
        }

        private void HandleStopEncounterSequence(StopEncounterSequence evt)
        {
            _targetAlpha = hiddenAlpha;
        }

        private void CacheSpriteRenderersIfNeeded()
        {
            if (spriteRenderers != null && spriteRenderers.Length > 0)
                return;

            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void ApplyAlpha(float alpha)
        {
            if (spriteRenderers == null)
                return;

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                    continue;

                Color color = spriteRenderers[i].color;
                color.a = alpha;
                spriteRenderers[i].color = color;
            }
        }
    }
}
