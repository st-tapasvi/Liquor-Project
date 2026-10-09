namespace ST.LiquorTNT.Domain.Rules;

/// <summary>
/// The application a page belongs to. Stored as text in PAGES.APPLICATION_TYPE. Roles and rights are shared, so one
/// role may hold rights of both applications.
/// </summary>
public enum ApplicationType
{
    /// <summary>The web application on the server (users, roles, settings, masters …).</summary>
    WEB,

    /// <summary>The desktop application at the production line.</summary>
    LINE,
}
