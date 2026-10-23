using UnityEngine;

namespace SB.Scripts
{
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform[] sprites;
        [SerializeField, Tooltip("조우 이동 중 초당 이동하는 월드 거리입니다.")]
        private float scrollSpeed = 2f;
        [SerializeField, Tooltip("월드 좌표로 그리는 바다 물결에도 이동 거리를 전달합니다. 별도 항해 연출이 제어하는 밤 바다에서는 끕니다.")]
        private bool scrollShaderWaves;
        [SerializeField, Min(0f), Tooltip("조우 이동이 끝난 뒤 초당 이동하는 월드 거리입니다. 항상 스크롤하는 레이어에만 적용됩니다.")]
        private float idleScrollSpeed;
        [SerializeField, Min(0f), Tooltip("최대 설정 속도를 기준으로 속도를 바꾸는 시간입니다. 0이면 즉시 바뀝니다.")]
        private float speedChangeSeconds;
        [SerializeField] private float spriteWidth = 20f;
        [SerializeField] private float leftResetX = -20f;
        [SerializeField, Tooltip("이동 카메라를 쓰는 테스트 항해에서 재배치 경계의 기준이 되는 배경 루트입니다. 미지정 시 기존 월드 경계를 사용합니다.")]
        private Transform scrollReference;
        [SerializeField, Tooltip("조우 이동이 끝난 뒤에도 대기 속도로 스크롤합니다.")]
        private bool dontCare = false;
        [SerializeField, Tooltip("같은 이미지의 좌우 반전 타일을 번갈아 연결합니다. 초기 타일도 정방향/반전 순으로 배치하세요.")]
        private bool alternateMirroredTiles;
        private SpriteRenderer[] tileRenderers;
        private bool[] shaderWaveTiles;
        private MaterialPropertyBlock waveProperties;
        private float waveTravelDistance;
        private float previousReferenceX;
        private static readonly int TravelOffsetId = Shader.PropertyToID("_TravelOffset");
        private float currentScrollSpeed;
        private bool _isScrolling;
        private bool hasTravelStrength;
        private float travelStrength;

        public float CurrentScrollSpeed => currentScrollSpeed;

        private void Awake()
        {
            currentScrollSpeed = dontCare ? idleScrollSpeed : 0f;
            previousReferenceX = scrollReference != null ? scrollReference.position.x : 0f;
            waveProperties = new MaterialPropertyBlock();
            tileRenderers = new SpriteRenderer[sprites.Length];
            shaderWaveTiles = new bool[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null)
                {
                    tileRenderers[i] = sprites[i].GetComponent<SpriteRenderer>();
                    SpriteRenderer tile = tileRenderers[i];
                    shaderWaveTiles[i] = tile != null && tile.sharedMaterial != null
                        && tile.sharedMaterial.HasProperty(TravelOffsetId);
                }
        }

        public void StartScroll()
        {
            hasTravelStrength = false;
            _isScrolling = true;
        }

        public void StopScroll()
        {
            hasTravelStrength = false;
            _isScrolling = false;
        }

        public void SetTravelStrength(float strength)
        {
            hasTravelStrength = true;
            travelStrength = Mathf.Clamp01(strength);
            currentScrollSpeed = Mathf.Lerp(dontCare ? idleScrollSpeed : 0f, scrollSpeed, travelStrength);
        }

        private void Update()
        {
            float targetSpeed = _isScrolling ? scrollSpeed : (dontCare ? idleScrollSpeed : 0f);
            float maxScrollSpeed = Mathf.Max(scrollSpeed, idleScrollSpeed);
            if (hasTravelStrength)
                currentScrollSpeed = Mathf.Lerp(dontCare ? idleScrollSpeed : 0f, scrollSpeed, travelStrength);
            else if (speedChangeSeconds <= 0f || maxScrollSpeed <= 0f)
                currentScrollSpeed = targetSpeed;
            else
                currentScrollSpeed = Mathf.MoveTowards(currentScrollSpeed, targetSpeed,
                    maxScrollSpeed * Time.deltaTime / speedChangeSeconds);

            if (currentScrollSpeed <= 0f)
                return;

            MoveSprites(currentScrollSpeed);
            RepositionOutOfBoundsSprites();
        }

        private void LateUpdate()
        {
            float referenceX = scrollReference != null ? scrollReference.position.x : 0f;
            float referenceTravel = Mathf.Max(0f, referenceX - previousReferenceX);
            previousReferenceX = referenceX;
            if (!scrollShaderWaves || currentScrollSpeed <= 0f)
                return;

            // 크리쳐 항해에서는 배경과 카메라 자체가 전진하므로 이미 흐른 거리는 중복해서 더하지 않습니다.
            float additionalTravel = Mathf.Max(0f, currentScrollSpeed * Time.deltaTime - referenceTravel);
            if (additionalTravel <= 0f)
                return;

            waveTravelDistance += additionalTravel;
            for (int i = 0; i < tileRenderers.Length; i++)
            {
                if (!shaderWaveTiles[i] || tileRenderers[i] == null)
                    continue;

                tileRenderers[i].GetPropertyBlock(waveProperties);
                waveProperties.SetFloat(TravelOffsetId, waveTravelDistance);
                tileRenderers[i].SetPropertyBlock(waveProperties);
            }
        }

        private void MoveSprites(float speed)
        {
            float moveAmount = speed * Time.deltaTime;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null)
                    continue;

                sprites[i].position += Vector3.left * moveAmount;
            }
        }

        private void RepositionOutOfBoundsSprites()
        {
            float boundary = leftResetX + (scrollReference != null ? scrollReference.position.x : 0f);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null || sprites[i].position.x > boundary)
                    continue;

                MoveSpriteToRightEnd(sprites[i]);
            }
        }

        private void MoveSpriteToRightEnd(Transform targetSprite)
        {
            float rightMostX = GetRightMostX();

            // 홀수 개의 타일도 순환할 수 있도록 현재 맨 오른쪽 타일의 반대 방향으로 연결합니다.
            if (alternateMirroredTiles)
            {
                SpriteRenderer rightRenderer = null;
                SpriteRenderer targetRenderer = null;
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == targetSprite) targetRenderer = tileRenderers[i];
                    if (sprites[i] != null && sprites[i].position.x == rightMostX)
                        rightRenderer = tileRenderers[i];
                }
                if (targetRenderer != null && rightRenderer != null)
                    targetRenderer.flipX = !rightRenderer.flipX;
            }

            Vector3 position = targetSprite.position;
            position.x = rightMostX + spriteWidth;
            targetSprite.position = position;
        }

        private float GetRightMostX()
        {
            float rightMostX = float.MinValue;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null)
                    continue;

                if (sprites[i].position.x > rightMostX)
                    rightMostX = sprites[i].position.x;
            }

            return rightMostX;
        }
    }
}
