namespace ToDoList.Core.Models
{
    /// <summary>
    /// Result of a conflict-checked write under the newest-edit-wins rule.
    /// </summary>
    public enum WriteOutcome
    {
        /// <summary>The write was applied (its edit time was newer or equal).</summary>
        Applied,

        /// <summary>The row exists but the incoming edit was stale and was ignored.</summary>
        Stale,

        /// <summary>No such row exists.</summary>
        NotFound
    }
}
