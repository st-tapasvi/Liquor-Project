namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>USER_SECURITY_QUESTION</c>: the question a user chose and the HASH of their answer.
/// The answer is normalised (trimmed, lower-cased) before hashing so "Delhi" and " delhi " match.
/// A user has one active row; choosing a new question deactivates the old one.
/// </summary>
public class USER_SECURITY_QUESTION
{
    private USER_SECURITY_QUESTION()
    {
        AnswerHash = string.Empty;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int QuestionId { get; private set; }
    public string AnswerHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static USER_SECURITY_QUESTION Create(int userId, int questionId, string answerHash, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(answerHash))
        {
            throw new ArgumentException("Answer hash is required.", nameof(answerHash));
        }

        return new USER_SECURITY_QUESTION
        {
            UserId = userId,
            QuestionId = questionId,
            AnswerHash = answerHash,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>The user picked this question again (or changed the answer): update in place, it becomes the active one.</summary>
    public void Replace(string answerHash, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(answerHash))
        {
            throw new ArgumentException("Answer hash is required.", nameof(answerHash));
        }

        AnswerHash = answerHash;
        IsActive = true;
        UpdatedAt = now;
    }
}
