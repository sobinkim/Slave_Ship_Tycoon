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
        private bool _isScrolling;

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
