using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    public sealed partial class InGameToolkitScreen
    {
        [Header("Boss button transport clouds")]
        [SerializeField, Min(1f), Tooltip("운송 중 각 먹구름 입자가 나타나고 사라지는 주기입니다. 일시정지 중에는 연출도 멈춥니다.")]
        private float bossCloudCycleSeconds = 4.8f;
        [SerializeField, Min(0f), Tooltip("먹구름 입자가 수명 동안 좌우로 흐르는 UI Toolkit 논리 픽셀 거리입니다.")]
        private float bossCloudDriftPixels = 8f;
        [SerializeField, Range(0f, 1f), Tooltip("먹구름 입자의 최대 불투명도입니다. 크라켄이 구름 사이로 비치도록 조절합니다.")]
        private float bossCloudDensity = 0.7f;

        private Button bossTransportButton;
        private VisualElement bossTransportOverlay;
        private VisualElement bossCloudBack;
        private VisualElement bossCloudFront;
        private VisualElement bossCloudWisp;
        private bool bossTransportPresentationActive;
        private float bossCloudCycleElapsed;

        private void BindBossTransportPresentation()
        {
            ClearBossTransportPresentation();
            if (root == null) return;
            bossTransportButton = root.Q<Button>("bossButton");
            bossTransportOverlay = root.Q("bossTransportOverlay");
            bossCloudBack = root.Q("bossCloudBack");
            bossCloudFront = root.Q("bossCloudFront");
            bossCloudWisp = root.Q("bossCloudWisp");
            ResetBossTransportPose();
        }

        private void RefreshBossTransportPresentation(bool isTransport)
        {
            bool show = isTransport && bossTransportButton != null && bossTransportOverlay != null &&
                bossCloudBack != null && bossCloudFront != null && bossCloudWisp != null;
            if (show == bossTransportPresentationActive) return;
            bossTransportPresentationActive = show;
            bossCloudCycleElapsed = 0f;
            bossTransportButton?.EnableInClassList("is-transport-blocked", show);
            bossTransportOverlay?.EnableInClassList("hidden", !show);
            ResetBossTransportPose();
        }

        private void UpdateBossTransportPresentation()
        {
            if (!bossTransportPresentationActive || bossTransportOverlay == null || bossTransportOverlay.panel == null ||
                Time.timeScale <= 0f) return;
            for (VisualElement element = bossTransportOverlay; element != null; element = element.parent)
            {
                if (element.resolvedStyle.display == DisplayStyle.None ||
                    element.resolvedStyle.visibility != Visibility.Visible || element.resolvedStyle.opacity <= 0f) return;
            }

            float cycleSeconds = Mathf.Max(1f, bossCloudCycleSeconds);
            bossCloudCycleElapsed = Mathf.Repeat(bossCloudCycleElapsed + Time.unscaledDeltaTime, cycleSeconds);
            float phase = bossCloudCycleElapsed / cycleSeconds;
            float driftPixels = Mathf.Max(0f, bossCloudDriftPixels);
            float density = Mathf.Clamp01(bossCloudDensity);
            ApplyBossCloudParticle(bossCloudBack, phase, driftPixels, density * 0.8f);
            ApplyBossCloudParticle(bossCloudFront, Mathf.Repeat(phase + 1f / 3f, 1f), -driftPixels, density);
            ApplyBossCloudParticle(bossCloudWisp, Mathf.Repeat(phase + 2f / 3f, 1f), driftPixels * 0.7f, density * 0.65f);
        }

        private static void ApplyBossCloudParticle(VisualElement cloud, float lifeProgress, float travelPixels, float peakOpacity)
        {
            float opacityEnvelope = Mathf.Sin(lifeProgress * Mathf.PI);
            float scale = Mathf.Lerp(0.92f, 1.08f, lifeProgress);
            cloud.style.opacity = opacityEnvelope * peakOpacity;
            cloud.style.translate = new Translate((lifeProgress - 0.5f) * travelPixels, -lifeProgress * 4f, 0f);
            cloud.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private void ResetBossTransportPose()
        {
            ResetBossCloudParticle(bossCloudBack);
            ResetBossCloudParticle(bossCloudFront);
            ResetBossCloudParticle(bossCloudWisp);
        }

        private static void ResetBossCloudParticle(VisualElement cloud)
        {
            if (cloud == null) return;
            cloud.style.translate = new Translate(0f, 0f, 0f);
            cloud.style.scale = new Scale(Vector3.one);
            cloud.style.opacity = 0f;
        }

        private void ClearBossTransportPresentation()
        {
            bossTransportPresentationActive = false;
            bossCloudCycleElapsed = 0f;
            bossTransportButton?.RemoveFromClassList("is-transport-blocked");
            bossTransportOverlay?.AddToClassList("hidden");
            ResetBossTransportPose();
            bossTransportButton = null;
            bossTransportOverlay = null;
            bossCloudBack = null;
            bossCloudFront = null;
            bossCloudWisp = null;
        }
    }
}
