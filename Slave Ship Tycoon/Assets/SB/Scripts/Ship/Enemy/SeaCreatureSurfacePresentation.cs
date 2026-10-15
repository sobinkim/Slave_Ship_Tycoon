using UnityEngine;

namespace SB.Scripts
{
    /// <summary>Explicitly driven creature rig and water contact. Does not move the gameplay root.</summary>
    [DisallowMultipleComponent]
    public sealed class SeaCreatureSurfacePresentation : MonoBehaviour
    {
        [Header("Explicit body references")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField, Tooltip("Optional articulated parts, with their pivots placed at the joint.")]
        private Transform tailPivot;
        [SerializeField] private Transform finPivot;
        [SerializeField] private Transform jawPivot;
        [SerializeField, Tooltip("Optional additional body renderers to fade and submerge with the body.")]
        private SpriteRenderer[] bodyParts = new SpriteRenderer[0];

        [Header("Explicit water references")]
        [SerializeField, Tooltip("Keep surface renderers beside Visual, so the surface does not bob with the body.")]
        private SpriteRenderer waterlineRenderer;
        [SerializeField] private SpriteRenderer contactShadow;
        [SerializeField] private SpriteRenderer bowFoam;
        [SerializeField] private SpriteRenderer[] trailingFoam = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer rippleRenderer;

        [Header("Swimming")]
        [SerializeField] private bool artFacesRight;
        [SerializeField, Min(0.01f)] private float idleCyclesPerSecond = 0.75f;
        [SerializeField, Min(0.01f)] private float swimCyclesPerSecond = 2.2f;
        [SerializeField, Min(0.01f)] private float fullStrokeSpeed = 1.1f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.018f;
        [SerializeField, Range(0f, 0.1f)] private float breathingScale = 0.018f;
        [SerializeField, Range(0f, 20f)] private float bodyPitchDegrees = 3.5f;
        [SerializeField, Range(0f, 50f)] private float tailSwingDegrees = 18f;
        [SerializeField, Range(0f, 40f)] private float finSwingDegrees = 8f;
        [SerializeField, Min(0.01f)] private float turnSeconds = 0.14f;
        [SerializeField, Range(0f, 0.05f)] private float idleBendStrength = 0.003f;
        [SerializeField, Range(0f, 0.05f)] private float swimBendStrength = 0.012f;
        [SerializeField, Range(0f, 0.05f)] private float chargeBendStrength = 0.022f;

        [Header("Attack and recoil")]
        [SerializeField, Min(0f)] private float biteLungeDistance = 0.11f;
        [SerializeField, Min(0f)] private float windupPullbackDistance = 0.06f;
        [SerializeField, Range(0f, 45f)] private float jawOpenDegrees = 18f;
        [SerializeField, Min(0f)] private float hitRecoilDistance = 0.05f;
        [SerializeField, Min(0.01f)] private float hitRecoilSeconds = 0.14f;

        [Header("Water surface")]
        [SerializeField, Tooltip("Water height in this creature root's local coordinates. Tune to the submerged edge of the art.")]
        private float waterlineLocalY = -0.13f;
        [SerializeField] private Color waterTint = new Color(0.15f, 0.56f, 0.6f, 0.65f);
        [SerializeField, Min(0.01f)] private float foamFadeSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float wakeCycleSeconds = 0.6f;
        [SerializeField, Min(0f)] private float wakeTravelDistance = 0.5f;
        [SerializeField, Min(0f)] private float wakeSpread = 0.07f;
        [SerializeField, Range(0f, 1f)] private float idleWaterlineOpacity = 0.8f;
        [SerializeField, Min(0.01f)] private float rippleSeconds = 0.42f;
        [SerializeField, Min(0f)] private float rippleExpansion = 0.45f;
        [SerializeField, Min(0.01f)] private float sinkDistance = 0.4f;

        private static readonly int SwimPhaseId = Shader.PropertyToID("_SwimPhase");
        private static readonly int SwimStrengthId = Shader.PropertyToID("_SwimStrength");
        private static readonly int SinkProgressId = Shader.PropertyToID("_SinkProgress");
        private static readonly int SinkDistanceId = Shader.PropertyToID("_SinkDistance");
        private static readonly int WaterlineId = Shader.PropertyToID("_Waterline");
        private static readonly int WaterTintId = Shader.PropertyToID("_WaterTint");

        private PoseCache visualPose;
        private PoseCache tailPose;
        private PoseCache finPose;
        private PoseCache jawPose;
        private SurfaceCache waterline;
        private SurfaceCache shadow;
        private SurfaceCache bow;
        private SurfaceCache ripple;
        private SurfaceCache[] foam;
        private BodyCache body;
        private BodyCache[] parts;
        private MaterialPropertyBlock properties;
        private SeaCreatureState previousState;
        private Vector2 facing = Vector2.left;
        private float animationTime;
        private float strokePhase;
        private float heading;
        private float headingVelocity;
        private float foamVisibility;
        private float hitStrength;
        private float rippleAge;
        private bool initialized;

        private readonly struct PoseCache
        {
            public readonly Transform Transform;
            public readonly Vector3 Position;
            public readonly Vector3 Scale;
            public readonly Quaternion Rotation;

            public PoseCache(Transform target)
            {
                Transform = target;
                Position = target != null ? target.localPosition : Vector3.zero;
                Scale = target != null ? target.localScale : Vector3.one;
                Rotation = target != null ? target.localRotation : Quaternion.identity;
            }

            public void Restore()
            {
                if (Transform == null) return;
                Transform.localPosition = Position;
                Transform.localScale = Scale;
                Transform.localRotation = Rotation;
            }
        }

        private readonly struct SurfaceCache
        {
            public readonly SpriteRenderer Renderer;
            public readonly PoseCache Pose;
            public readonly Color Tint;

            public SurfaceCache(SpriteRenderer renderer)
            {
                Renderer = renderer;
                Pose = new PoseCache(renderer != null ? renderer.transform : null);
                Tint = renderer != null ? renderer.color : Color.white;
            }

            public void Restore(bool visible)
            {
                Pose.Restore();
                if (Renderer == null) return;
                Renderer.color = Tint;
                Renderer.enabled = visible;
            }

            public void SetOpacity(float opacity)
            {
                if (Renderer == null) return;
                Color tint = Tint;
                tint.a *= Mathf.Clamp01(opacity);
                Renderer.color = tint;
                Renderer.enabled = tint.a > 0.001f;
            }
        }

        private readonly struct BodyCache
        {
            public readonly SpriteRenderer Renderer;
            public readonly Color Tint;
            public readonly bool HasSwim;
            public readonly bool HasSink;
            public readonly bool HasWaterTint;

            public BodyCache(SpriteRenderer renderer)
            {
                Renderer = renderer;
                Tint = renderer != null ? renderer.color : Color.white;
                Material material = renderer != null ? renderer.sharedMaterial : null;
                HasSwim = material != null && material.HasProperty(SwimPhaseId) && material.HasProperty(SwimStrengthId);
                HasSink = material != null && material.HasProperty(SinkProgressId) && material.HasProperty(SinkDistanceId)
                    && material.HasProperty(WaterlineId);
                HasWaterTint = material != null && material.HasProperty(WaterTintId);
            }
        }

        private void Awake() => CacheReferences();
        private void OnEnable() => ResetPresentation(Vector2.left);

        private bool CacheReferences()
        {
            if (initialized) return true;
            if (visualRoot == null || bodyRenderer == null) return false;
            visualPose = new PoseCache(visualRoot);
            tailPose = new PoseCache(tailPivot);
            finPose = new PoseCache(finPivot);
            jawPose = new PoseCache(jawPivot);
            body = new BodyCache(bodyRenderer);
            parts = new BodyCache[bodyParts.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = new BodyCache(bodyParts[i]);
            waterline = new SurfaceCache(waterlineRenderer);
            shadow = new SurfaceCache(contactShadow);
            bow = new SurfaceCache(bowFoam);
            ripple = new SurfaceCache(rippleRenderer);
            foam = new SurfaceCache[trailingFoam.Length];
            for (int i = 0; i < foam.Length; i++) foam[i] = new SurfaceCache(trailingFoam[i]);
            properties = new MaterialPropertyBlock();
            initialized = true;
            return true;
        }

        /// <summary>Call from the owner's pool spawn. Facing is explicit; the initial pose never defaults to the right.</summary>
        public void ResetPresentation(Vector2 facingDirection)
        {
            if (!CacheReferences()) return;
            RestoreTransforms();
            facing = facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : Vector2.left;
            heading = FacingSign(facing);
            headingVelocity = 0f;
            animationTime = 0f;
            strokePhase = 0f;
            foamVisibility = 0f;
            hitStrength = 0f;
            rippleAge = rippleSeconds;
            previousState = SeaCreatureState.Cruising;
            ApplyBodyMaterial(body, 0f, idleBendStrength, 0f, true);
            foreach (BodyCache part in parts) ApplyBodyMaterial(part, 0f, idleBendStrength, 0f, true);
            visualRoot.localScale = Vector3.Scale(visualPose.Scale, new Vector3(heading, 1f, 1f));
            waterline.Restore(true);
            shadow.Restore(true);
            bow.Restore(false);
            ripple.Restore(false);
            foreach (SurfaceCache fleck in foam) fleck.Restore(false);
        }

        /// <summary>
        /// Owner calls once after movement. Water velocity is actual world velocity, stateProgress is 0..1,
        /// phaseRadians is a per-creature phase offset, and hitPulse is 1 only on a new hit.
        /// </summary>
        public void TickPresentation(SeaCreatureState state, float deltaTime, float phaseRadians,
            Vector2 waterVelocity, Vector2 facingDirection, float stateProgress, float hitPulse = 0f)
        {
            if (!CacheReferences()) return;
            float delta = Mathf.Max(0f, deltaTime);
            if (delta <= 0f) return;
            float progress = Mathf.Clamp01(stateProgress);
            if (facingDirection.sqrMagnitude > 0.001f) facing = facingDirection.normalized;
            bool dying = state == SeaCreatureState.Dying;
            bool charging = state == SeaCreatureState.Charging;
            float speed = Mathf.Clamp01(waterVelocity.magnitude / fullStrokeSpeed);
            float activity = charging ? 1f : speed;
            float sink = dying ? Mathf.SmoothStep(0f, 1f, progress) : 0f;
            animationTime += delta;
            float frequency = Mathf.Lerp(idleCyclesPerSecond, swimCyclesPerSecond, activity);
            if (charging) frequency *= 1.65f;
            strokePhase += delta * frequency * Mathf.PI * 2f;
            float phase = strokePhase + phaseRadians;
            hitStrength = Mathf.Max(hitStrength, Mathf.Clamp01(hitPulse));
            float hit = hitStrength;
            hitStrength = Mathf.MoveTowards(hitStrength, 0f, delta / hitRecoilSeconds);

            if (state != previousState)
            {
                if (state == SeaCreatureState.Biting || charging || dying) rippleAge = 0f;
                previousState = state;
            }
            rippleAge += delta;
            heading = Mathf.SmoothDamp(heading, FacingSign(facing), ref headingVelocity, turnSeconds,
                Mathf.Infinity, delta);
            float mirror = Mathf.Sign(heading) * Mathf.Max(0.14f, Mathf.Abs(heading));
            float stroke = Mathf.Sin(phase);
            float breath = Mathf.Sin(animationTime * idleCyclesPerSecond * Mathf.PI * 2f + phaseRadians);
            Vector2 localFacing = transform.InverseTransformDirection(facing);
            Vector3 offset = Vector3.up * (breath * bobAmplitude * (1f - sink));
            Vector2 scale = new Vector2(1f + breath * breathingScale, 1f - breath * breathingScale * 0.6f);
            float pitch = facing.y * Mathf.Sign(facing.x) * bodyPitchDegrees;
            pitch += stroke * bodyPitchDegrees * 0.35f * (1f - sink);
            float jaw = 0f;

            if (state == SeaCreatureState.WindingUp)
            {
                float anticipation = Mathf.SmoothStep(0f, 1f, progress);
                offset -= (Vector3)localFacing * (windupPullbackDistance * anticipation);
                scale += new Vector2(-0.1f, 0.08f) * anticipation;
                pitch += Mathf.Sin(phase * 2f) * 1.2f * anticipation;
                jaw = anticipation * 0.35f;
            }
            else if (state == SeaCreatureState.Biting)
            {
                float lunge = Mathf.Sin(Mathf.Clamp01((progress - 0.12f) / 0.88f) * Mathf.PI);
                float anticipation = Mathf.Sin(Mathf.Clamp01(progress / 0.18f) * Mathf.PI);
                offset += (Vector3)localFacing * (lunge * biteLungeDistance - anticipation * windupPullbackDistance * 0.4f);
                scale += new Vector2(lunge * 0.07f, -lunge * 0.06f);
                jaw = Mathf.Sin(Mathf.Clamp01(progress / 0.7f) * Mathf.PI);
            }
            else if (charging)
            {
                scale += new Vector2(0.08f, -0.08f);
                offset += (Vector3)localFacing * (biteLungeDistance * 0.25f);
                jaw = 0.25f;
            }
            else if (state == SeaCreatureState.Recovering)
            {
                float settle = Mathf.Sin(progress * Mathf.PI * 2f) * (1f - progress);
                offset -= (Vector3)localFacing * (settle * windupPullbackDistance * 0.4f);
                scale += new Vector2(-0.04f, 0.03f) * settle;
            }
            if (dying)
            {
                // Materials with the ship waterline convention sink their own vertices; other art uses the rig fallback.
                if (!body.HasSink) offset.y -= sink * sinkDistance;
                pitch -= sink * 8f;
                scale *= 1f - sink * 0.04f;
            }
            offset -= (Vector3)localFacing * (hitRecoilDistance * hit);
            pitch += Mathf.Sin(hit * Mathf.PI) * bodyPitchDegrees * 1.5f;
            visualRoot.localPosition = visualPose.Position + offset;
            visualRoot.localRotation = visualPose.Rotation * Quaternion.Euler(0f, 0f, pitch);
            visualRoot.localScale = Vector3.Scale(visualPose.Scale, new Vector3(scale.x * mirror, scale.y, 1f));
            AnimatePart(tailPose, stroke * tailSwingDegrees * Mathf.Lerp(0.35f, 1f, activity) * (1f - sink));
            AnimatePart(finPose, Mathf.Sin(phase - 1.1f) * finSwingDegrees * (1f - sink));
            AnimatePart(jawPose, jaw * jawOpenDegrees * (1f - sink));

            float bend = charging ? chargeBendStrength : Mathf.Lerp(idleBendStrength, swimBendStrength, activity);
            ApplyBodyMaterial(body, phase, bend * (1f - sink), sink, false);
            foreach (BodyCache part in parts) ApplyBodyMaterial(part, phase, bend * (1f - sink), sink, false);
            UpdateSurface(state, delta, phase, speed, localFacing, sink, hit);
        }

        private float FacingSign(Vector2 direction) => artFacesRight == (direction.x >= 0f) ? 1f : -1f;

        private static void AnimatePart(PoseCache pose, float degrees)
        {
            if (pose.Transform != null) pose.Transform.localRotation = pose.Rotation * Quaternion.Euler(0f, 0f, degrees);
        }

        private void ApplyBodyMaterial(BodyCache cached, float phase, float bend, float sink, bool reset)
        {
            SpriteRenderer renderer = cached.Renderer;
            if (renderer == null) return;
            if (reset) renderer.color = cached.Tint;
            else if (sink > 0f)
            {
                Color tint = renderer.color;
                tint.a = cached.Tint.a * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, sink)));
                renderer.color = tint;
            }
            if (!cached.HasSwim && !cached.HasSink && !cached.HasWaterTint) return;
            // Preserve unrelated per-renderer overrides and the normal hit tint from ShipHullFeedback.
            renderer.GetPropertyBlock(properties);
            if (cached.HasSwim)
            {
                properties.SetFloat(SwimPhaseId, phase);
                properties.SetFloat(SwimStrengthId, bend);
            }
            if (cached.HasSink)
            {
                Vector3 surface = transform.TransformPoint(new Vector3(0f, waterlineLocalY, 0f));
                properties.SetFloat(WaterlineId, renderer.transform.InverseTransformPoint(surface).y);
                properties.SetFloat(SinkProgressId, sink);
                properties.SetFloat(SinkDistanceId, sinkDistance);
            }
            if (cached.HasWaterTint) properties.SetColor(WaterTintId, waterTint);
            renderer.SetPropertyBlock(properties);
        }

        private void UpdateSurface(SeaCreatureState state, float delta, float phase, float speed,
            Vector2 direction, float sink, float hit)
        {
            float visible = 1f - sink;
            float agitation = state == SeaCreatureState.Charging ? 1f
                : state == SeaCreatureState.Biting ? 0.85f : state == SeaCreatureState.WindingUp ? 0.25f : speed;
            agitation = Mathf.Max(agitation, hit * 0.65f);
            foamVisibility = Mathf.MoveTowards(foamVisibility, agitation * visible, delta / foamFadeSeconds);
            float surfaceBreath = Mathf.Sin(phase * 0.65f);
            if (waterline.Renderer != null)
            {
                waterline.Pose.Transform.localScale = Vector3.Scale(waterline.Pose.Scale,
                    new Vector3(1f + surfaceBreath * 0.025f + sink * 0.12f, 1f + agitation * 0.12f, 1f));
                waterline.SetOpacity(Mathf.Lerp(idleWaterlineOpacity, 1f, agitation) * visible);
            }
            if (shadow.Renderer != null)
            {
                shadow.Pose.Transform.localScale = Vector3.Scale(shadow.Pose.Scale,
                    new Vector3(1f + surfaceBreath * 0.018f + sink * 0.16f, 1f + sink * 0.3f, 1f));
                shadow.SetOpacity(visible * visible);
            }
            if (bow.Renderer != null)
            {
                Vector3 position = bow.Pose.Position;
                position.x *= FacingSign(direction);
                bow.Pose.Transform.localPosition = position;
                bow.Pose.Transform.localScale = Vector3.Scale(bow.Pose.Scale,
                    new Vector3(1f + agitation * 0.2f, 1f + Mathf.Sin(phase * 1.4f) * 0.08f, 1f));
                bow.SetOpacity(foamVisibility * (0.7f + Mathf.Sin(phase) * 0.15f));
            }
            for (int i = 0; i < foam.Length; i++)
            {
                SurfaceCache fleck = foam[i];
                if (fleck.Renderer == null) continue;
                float age = Mathf.Repeat(animationTime / wakeCycleSeconds + (float)i / foam.Length, 1f);
                Vector2 spread = new Vector2(-direction.y, direction.x) * ((i % 2 == 0 ? -1f : 1f) * wakeSpread * age);
                Vector3 origin = fleck.Pose.Position;
                origin.x *= FacingSign(direction);
                fleck.Pose.Transform.localPosition = origin - (Vector3)direction * (wakeTravelDistance * age) + (Vector3)spread;
                float scale = Mathf.Lerp(0.7f, 1.2f, age);
                fleck.Pose.Transform.localScale = Vector3.Scale(fleck.Pose.Scale, new Vector3(scale, 1f - age * 0.3f, 1f));
                fleck.SetOpacity(foamVisibility * Mathf.Sin(age * Mathf.PI) * 0.75f);
            }
            if (ripple.Renderer != null)
            {
                float progress = Mathf.Clamp01(rippleAge / rippleSeconds);
                float scale = 1f + progress * rippleExpansion;
                ripple.Pose.Transform.localScale = Vector3.Scale(ripple.Pose.Scale, new Vector3(scale, scale, 1f));
                ripple.SetOpacity(Mathf.Sin(progress * Mathf.PI) * (1f - progress));
            }
        }

        private void RestoreTransforms()
        {
            visualPose.Restore();
            tailPose.Restore();
            finPose.Restore();
            jawPose.Restore();
        }

        private void OnDisable()
        {
            if (!initialized) return;
            RestoreTransforms();
            ApplyBodyMaterial(body, 0f, idleBendStrength, 0f, true);
            foreach (BodyCache part in parts) ApplyBodyMaterial(part, 0f, idleBendStrength, 0f, true);
            waterline.Restore(false);
            shadow.Restore(false);
            bow.Restore(false);
            ripple.Restore(false);
            foreach (SurfaceCache fleck in foam) fleck.Restore(false);
        }
    }
}
