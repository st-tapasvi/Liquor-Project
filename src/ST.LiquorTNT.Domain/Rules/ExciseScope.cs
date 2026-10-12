namespace ST.LiquorTNT.Domain.Rules;

/// <summary>
/// Which excises (states) a page or a page action belongs to: the excise codes ("RJ", "JK") of its PAGE_EXCISES /
/// PAGE_ACTION_EXCISES rows, or null (no rows) for every excise. An action counts in a supplier code only when its page
/// AND the action cover that supplier code's excise.
/// </summary>
public static class ExciseScope
{
    /// <summary>True when <paramref name="codes"/> (null = every excise) includes <paramref name="exciseCode"/>.</summary>
    public static bool Covers(IReadOnlyCollection<string>? codes, string exciseCode) =>
        codes is null || codes.Contains(exciseCode, StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the page and the action both cover <paramref name="exciseCode"/>.</summary>
    public static bool ActionCovers(IReadOnlyCollection<string>? pageCodes, IReadOnlyCollection<string>? actionCodes, string exciseCode) =>
        Covers(pageCodes, exciseCode) && Covers(actionCodes, exciseCode);

    /// <summary>
    /// True when the page and the action cover at least one of <paramref name="exciseCodes"/> (a company-level role works in
    /// several excises). <paramref name="exciseCodes"/> null = no restriction (templates, Admin).
    /// </summary>
    public static bool ActionCoversAny(IReadOnlyCollection<string>? pageCodes, IReadOnlyCollection<string>? actionCodes,
        IReadOnlyCollection<string>? exciseCodes) =>
        exciseCodes is null || exciseCodes.Any(code => ActionCovers(pageCodes, actionCodes, code));
}
