namespace SB.Core
{
    /// <summary>
    /// Lets an entity-owned effect adjust damage at the one shared health entry point.
    /// Implementations must return a non-negative amount without side effects:
    /// the same calculation is used by incoming-damage previews.
    /// </summary>
    public interface IIncomingDamageModifier
    {
        float ModifyIncomingDamage(float incomingDamage);
    }
}
