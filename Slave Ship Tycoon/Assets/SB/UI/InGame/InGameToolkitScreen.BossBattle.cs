using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    public sealed partial class InGameToolkitScreen
    {
        private const float BossSwordRestAngle = 42f;
        private const float BossFirstStrikePhase = 0.19f;
        private const float BossSecondStrikePhase = 0.415f;

        [Header("Boss button battle animation")]
        [SerializeField, Min(1.5f), Tooltip("예비 동작, 두 번의 타격, 반동과 대기를 포함한 한 주기입니다. 일시정지 중에는 연출도 멈춥니다.")]
        private float bossSwordCycleSeconds = 2.6f;
        [SerializeField, Range(0f, 20f), Tooltip("교차한 칼이 맞부딪히고 튕길 때 움직이는 각도입니다.")]
        private float bossSwordMotionDegrees = 10f;
        [SerializeField, Min(0f), Tooltip("칼이 맞부딪힐 때 이동하는 UI Toolkit 논리 픽셀 거리입니다.")]
        private float bossSwordRecoilPixels = 4f;
        [SerializeField, Range(0.03f, 0.1f), Tooltip("타격 직후 칼을 잠깐 멈춰 무게감을 주는 시간입니다.")]
        private float bossSwordContactHoldSeconds = 0.07f;
        [SerializeField, Min(0.03f), Tooltip("각 맞부딪힘에서 교차점의 작은 불꽃이 보이는 시간입니다.")]
        private float bossSwordImpactSeconds = 0.085f;

        private Button bossBattleButton;
        private VisualElement bossBattleOverlay;
        private VisualElement bossSwordLeft;
        private VisualElement bossSwordRight;
        private VisualElement bossSwordImpact;
        private bool bossBattlePresentationActive;
        private float bossSwordCycleElapsed;

        private void BindBossBattlePresentation()
        {
            ClearBossBattlePresentation();
            if (root == null) return;
            bossBattleButton = root.Q<Button>("bossButton");
            bossBattleOverlay = root.Q("bossBattleOverlay");
            bossSwordLeft = root.Q("bossSwordLeft");
            bossSwordRight = root.Q("bossSwordRight");
            bossSwordImpact = root.Q("bossSwordImpact");
            ResetBossBattlePose();
        }

        private void RefreshBossBattlePresentation(bool isBossBattle)
        {
            bool show = isBossBattle && bossBattleButton != null && bossBattleOverlay != null &&
                bossSwordLeft != null && bossSwordRight != null && bossSwordImpact != null;
            if (show == bossBattlePresentationActive) return;
            bossBattlePresentationActive = show;
            bossSwordCycleElapsed = 0f;
            bossBattleButton?.EnableInClassList("is-boss-battle", show);
            bossBattleOverlay?.EnableInClassList("hidden", !show);
            ResetBossBattlePose();
        }

        private void UpdateBossBattlePresentation()
        {
            if (!bossBattlePresentationActive || bossBattleOverlay == null || bossBattleOverlay.panel == null ||
                Time.timeScale <= 0f) return;
            for (VisualElement element = bossBattleOverlay; element != null; element = element.parent)
            {
                if (element.resolvedStyle.display == DisplayStyle.None ||
                    element.resolvedStyle.visibility != Visibility.Visible || element.resolvedStyle.opacity <= 0f) return;
            }

            float cycleSeconds = Mathf.Max(1.5f, bossSwordCycleSeconds);
            bossSwordCycleElapsed = Mathf.Repeat(bossSwordCycleElapsed + Time.unscaledDeltaTime, cycleSeconds);
            float phase = bossSwordCycleElapsed / cycleSeconds;
            float contactHoldPhase = Mathf.Clamp(bossSwordContactHoldSeconds / cycleSeconds, 0f, 0.07f);
            float motion = EvaluateBossSwordMotion(phase, contactHoldPhase);
            float angle = BossSwordRestAngle + motion * bossSwordMotionDegrees;
            float recoil = motion * bossSwordRecoilPixels;
            bossSwordLeft.style.rotate = new Rotate(new Angle(-angle, AngleUnit.Degree));
            bossSwordRight.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            bossSwordLeft.style.translate = new Translate(-recoil, -recoil * 0.5f, 0f);
            bossSwordRight.style.translate = new Translate(recoil, -recoil * 0.5f, 0f);

            float firstImpactAge = bossSwordCycleElapsed - cycleSeconds * BossFirstStrikePhase;
            float secondImpactAge = bossSwordCycleElapsed - cycleSeconds * BossSecondStrikePhase;
            float impactAge = secondImpactAge >= 0f ? secondImpactAge : firstImpactAge;
            float impactSeconds = Mathf.Max(0.03f, bossSwordImpactSeconds);
            bool hasImpact = impactAge >= 0f && impactAge < impactSeconds;
            float impactProgress = hasImpact ? impactAge / impactSeconds : 0f;
            float impactStrength = secondImpactAge >= 0f ? 0.7f : 1f;
            float impactFade = 1f - impactProgress;
            bossSwordImpact.style.opacity = hasImpact ? impactStrength * impactFade * impactFade : 0f;
            float impactScale = hasImpact ? Mathf.Lerp(1.4f, 0.6f, impactProgress) * impactStrength : 1f;
            bossSwordImpact.style.scale = new Scale(new Vector3(impactScale, impactScale, 1f));
        }

        private static float EvaluateBossSwordMotion(float phase, float contactHoldPhase)
        {
            if (phase < 0.16f) return SmoothBossSwordMotion(0f, 1f, phase / 0.16f);
            if (phase < BossFirstStrikePhase) return AccelerateBossSwordMotion(1f, -0.8f,
                (phase - 0.16f) / (BossFirstStrikePhase - 0.16f));
            float firstHoldEnd = BossFirstStrikePhase + contactHoldPhase;
            if (phase < firstHoldEnd) return -0.8f;
            if (phase < 0.29f) return RecoilBossSwordMotion(-0.8f, 0.62f,
                (phase - firstHoldEnd) / (0.29f - firstHoldEnd));
            if (phase < 0.39f) return SmoothBossSwordMotion(0.62f, 0.85f, (phase - 0.29f) / 0.10f);
            if (phase < BossSecondStrikePhase) return AccelerateBossSwordMotion(0.85f, -0.45f,
                (phase - 0.39f) / (BossSecondStrikePhase - 0.39f));
            float secondHoldEnd = BossSecondStrikePhase + contactHoldPhase;
            if (phase < secondHoldEnd) return -0.45f;
            if (phase < 0.50f) return RecoilBossSwordMotion(-0.45f, 0.32f,
                (phase - secondHoldEnd) / (0.50f - secondHoldEnd));
            if (phase < 0.57f) return SmoothBossSwordMotion(0.32f, -0.1f, (phase - 0.50f) / 0.07f);
            if (phase < 0.67f) return SmoothBossSwordMotion(-0.1f, 0f, (phase - 0.57f) / 0.10f);
            return 0f;
        }

        private static float AccelerateBossSwordMotion(float from, float to, float progress)
        {
            return Mathf.Lerp(from, to, progress * progress * progress);
        }

        private static float RecoilBossSwordMotion(float from, float to, float progress)
        {
            float remaining = 1f - Mathf.Clamp01(progress);
            return Mathf.Lerp(from, to, 1f - remaining * remaining * remaining);
        }

        private static float SmoothBossSwordMotion(float from, float to, float progress)
        {
            return Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
        }

        private void ResetBossBattlePose()
        {
            if (bossSwordLeft != null)
            {
                bossSwordLeft.style.rotate = new Rotate(new Angle(-BossSwordRestAngle, AngleUnit.Degree));
                bossSwordLeft.style.translate = new Translate(0f, 0f, 0f);
            }
            if (bossSwordRight != null)
            {
                bossSwordRight.style.rotate = new Rotate(new Angle(BossSwordRestAngle, AngleUnit.Degree));
                bossSwordRight.style.translate = new Translate(0f, 0f, 0f);
            }
            if (bossSwordImpact != null)
            {
                bossSwordImpact.style.opacity = 0f;
                bossSwordImpact.style.scale = new Scale(Vector3.one);
            }
        }

        private void ClearBossBattlePresentation()
        {
            bossBattlePresentationActive = false;
            bossSwordCycleElapsed = 0f;
            bossBattleButton?.RemoveFromClassList("is-boss-battle");
            bossBattleOverlay?.AddToClassList("hidden");
            ResetBossBattlePose();
            bossBattleButton = null;
            bossBattleOverlay = null;
            bossSwordLeft = null;
            bossSwordRight = null;
            bossSwordImpact = null;
        }
    }
}
