namespace ST.LiquorTNT.Business.Common.Abstractions;

/// <summary>Used by Auth today and by the Users module later, so it is cross-cutting.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string storedHash);
}
