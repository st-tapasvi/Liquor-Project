namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>PAGE_EXCISES</c>: the page exists in this excise (state). A page without rows exists in every excise.
/// Seeded by developers with SQL, like the pages themselves.
/// </summary>
public class PAGE_EXCISES
{
    private PAGE_EXCISES()
    {
    }

    public int Id { get; private set; }
    public int PageId { get; private set; }
    public int ExciseId { get; private set; }
}
