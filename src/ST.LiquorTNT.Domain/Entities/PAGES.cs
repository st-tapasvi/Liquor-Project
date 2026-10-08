using ST.LiquorTNT.Domain.Rules;

namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>PAGES</c>: one screen of the web or the line application (User, Role ...). Seeded by developers
/// through SQL scripts, because API endpoints are bound to the page keys; administrators never create pages.
/// </summary>
public class PAGES
{
    private PAGES()
    {
        PageName = string.Empty;
    }

    public int Id { get; private set; }
    public string PageName { get; private set; }      // label for the rights grid: User
    public string? ModuleName { get; private set; }   // menu group: Administration
    public string? PageKey { get; private set; }      // first part of the permission key: user
    public ApplicationType ApplicationType { get; private set; }   // WEB application or LINE application
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
}
