namespace SB.Core
{
    /// <summary>
    /// Lets an entity-owned effect adjust damage at the one shared health entry point.
    /// Implementations must return a non-negative amount.
    /// </summary>
    public interface IIncomingDamageModifier
    {
        float ModifyIncomingDamage(float incomingDamage);
    }
}
