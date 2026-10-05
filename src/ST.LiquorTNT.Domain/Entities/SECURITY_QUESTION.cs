namespace ST.LiquorTNT.Domain.Entities;

/// <summary>Entity for <c>SECURITY_QUESTION</c>: the master list of questions a user may choose from.</summary>
public class SECURITY_QUESTION
{
    private SECURITY_QUESTION()
    {
        QuestionText = string.Empty;
    }

    public int Id { get; private set; }
    public string QuestionText { get; private set; }
    public bool Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}
