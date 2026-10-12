namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>PAGE_ACTION_EXCISES</c>: the action exists in this excise. An action without rows exists wherever its
/// page does; with rows, only in those excises (always some of its page's).
/// </summary>
public class PAGE_ACTION_EXCISES
{
    private PAGE_ACTION_EXCISES()
    {
    }

    public int Id { get; private set; }
    public int PageActionId { get; private set; }
    public int ExciseId { get; private set; }
}
