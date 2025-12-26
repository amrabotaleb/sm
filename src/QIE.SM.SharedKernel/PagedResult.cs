namespace QIE.SM.SharedKernel;

/// <summary>
/// Represents a paged result set.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="totalCount">The total number of items.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    public PagedResult(IReadOnlyCollection<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the items for the page.
    /// </summary>
    public IReadOnlyCollection<T> Items { get; }

    /// <summary>
    /// Gets the total number of items.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; }
}
