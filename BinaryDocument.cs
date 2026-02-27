using MemoryPack;

namespace KeyValueStoreServer
{
    /// <summary>
    /// Represents a binary document that encapsulates an entity together with associated labels and tags for efficient
    /// serialization and deserialization.
    /// </summary>
    /// <remarks>BinaryDocument facilitates the storage and retrieval of entities along with their metadata in
    /// a compact binary format. This is useful for scenarios where performance and space efficiency are important, such
    /// as caching or transmitting data between systems.</remarks>
    /// <typeparam name="TEntity">The type of the entity contained within the binary document.</typeparam>
    [MemoryPackable]
    public partial class BinaryDocument<TEntity>
    {
        /// <summary>
        /// Gets or sets the entity associated with this instance.
        /// </summary>
        /// <remarks>The entity can be null, indicating that no entity is currently associated. Ensure to
        /// check for null before accessing properties of the entity.</remarks>
        [MemoryPackOrder(0)] public TEntity? Entity { get; set; } = default;

        /// <summary>
        /// Gets or sets the labels associated with the object, which can be used for categorization or filtering.
        /// </summary>
        /// <remarks>The labels are represented as an array of strings. If no labels are assigned, the
        /// property will be null.</remarks>
        [MemoryPackOrder(1)] public string[]? Labels { get; set; } = default;

        /// <summary>
        /// Gets or sets the tags associated with the item, which can be used for categorization or filtering.
        /// </summary>
        /// <remarks>The tags are represented as an array of strings. This property can be null,
        /// indicating that no tags are assigned.</remarks>
        [MemoryPackOrder(2)] public string[]? Tags { get; set; } = default;
    }
}
