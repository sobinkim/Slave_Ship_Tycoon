using SB.Core;

namespace SB.Scripts.AttackCompo
{
    public class Test_ShipAttackCompo : Base_ShipAttackCompo
    {
        protected override void AttackStart()
        {
            base.AttackStart();

            if (CurrentTarget == null)
                return;

            print($"{Owner.name} -> {CurrentTarget.name} Damage 10");
            CurrentTarget.GetCompo<EntityHealth>()?.ApplyDamage(10);
        }
    }
}
