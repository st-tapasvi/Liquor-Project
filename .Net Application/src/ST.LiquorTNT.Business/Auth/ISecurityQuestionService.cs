using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Business.Auth;

public interface ISecurityQuestionService
{
    Task<IReadOnlyList<SecurityQuestionResponse>> GetQuestionsAsync(CancellationToken ct);

    /// <summary>Sets the caller's question and answer. Requires the current password so a hijacked session cannot plant a back door.</summary>
    Task<MessageResponse> SetMyQuestionAsync(SetSecurityQuestionRequest request, CancellationToken ct);
}
