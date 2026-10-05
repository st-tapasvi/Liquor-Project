namespace ST.LiquorTNT.Business.Common;

/// <summary>Time, injected so it can be frozen in tests.</summary>
public interface IClock
{
    /// <summary>Current UTC time. Used for infrastructure concerns such as JWT expiry.</summary>
    DateTime UtcNow { get; }

    /// <summary>Current time in India Standard Time. The User module operates in IST; "today" is its date part.</summary>
    DateTime IndiaNow { get; }
}
