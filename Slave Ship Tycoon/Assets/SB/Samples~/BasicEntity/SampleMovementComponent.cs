using SB.Core;
using UnityEngine;

namespace SB.Core.Samples
{
    public sealed class SampleMovementComponent : EntityComponent
    {
        [SerializeField] private float speed = 5f;

        private void Update()
        {
            if (Owner == null)
                return;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            Owner.transform.position += direction * (speed * Time.deltaTime);
        }
    }
}
