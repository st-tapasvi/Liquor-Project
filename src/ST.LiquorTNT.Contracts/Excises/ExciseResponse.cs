namespace ST.LiquorTNT.Contracts.Excises;

/// <summary>One state excise department (RJ, UP, JK ...), for dropdowns such as the supplier code form.</summary>
public sealed class ExciseResponse
{
    public int Id { get; set; }
    public string ExciseCode { get; set; } = string.Empty;
    public string ExciseName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
