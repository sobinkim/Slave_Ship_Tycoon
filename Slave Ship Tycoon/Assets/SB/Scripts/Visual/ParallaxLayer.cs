using UnityEngine;

namespace SB.Scripts
{
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform[] sprites;
        [SerializeField] private float scrollSpeed = 2f;
        [SerializeField] private float spriteWidth = 20f;
        [SerializeField] private float leftResetX = -20f;
        [SerializeField] private bool dontCare = false;
        [SerializeField, Tooltip("같은 이미지의 좌우 반전 타일을 번갈아 연결합니다. 초기 타일도 정방향/반전 순으로 배치하세요.")]
        private bool alternateMirroredTiles;
        private SpriteRenderer[] tileRenderers;
        private bool _isScrolling;

        private void Awake()
        {
            tileRenderers = new SpriteRenderer[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null)
                    tileRenderers[i] = sprites[i].GetComponent<SpriteRenderer>();
        }

        public void StartScroll()
        {
            _isScrolling = true;
        }

        public void StopScroll()
        {
            if(dontCare)
                return;
            
            _isScrolling = false;
        }

        private void Update()
        {
            if (_isScrolling == false && !dontCare )
                return;

            MoveSprites();
            RepositionOutOfBoundsSprites();
        }

        private void MoveSprites()
        {
            float moveAmount = scrollSpeed * Time.deltaTime;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null)
                    continue;

                sprites[i].position += Vector3.left * moveAmount;
            }
        }

        private void RepositionOutOfBoundsSprites()
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null || sprites[i].position.x > leftResetX)
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
