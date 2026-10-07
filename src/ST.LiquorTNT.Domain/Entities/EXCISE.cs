namespace ST.LiquorTNT.Domain.Entities;

/// <summary>Entity for <c>EXCISE</c>: the 11 state excise departments (RJ, UP ...). Seeded by SQL; read-only here.</summary>
public class EXCISE
{
    private EXCISE()
    {
        ExciseCode = string.Empty;
        ExciseName = string.Empty;
    }

    public int Id { get; private set; }               // EXCISE_ID
    public string ExciseCode { get; private set; }    // RJ
    public string ExciseName { get; private set; }    // Rajasthan
    public bool IsActive { get; private set; }
}
