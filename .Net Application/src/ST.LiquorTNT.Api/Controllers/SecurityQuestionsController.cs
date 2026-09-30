using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Api.Controllers;

[ApiController]
[Route("api/securityquestions")]
public sealed class SecurityQuestionsController : ControllerBase
{
    private readonly ISecurityQuestionService _questions;

    public SecurityQuestionsController(ISecurityQuestionService questions) => _questions = questions;

    /// <summary>Anonymous: the list is shown on the profile screen and during recovery.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SecurityQuestionResponse>>> GetAsync(CancellationToken ct)
        => Ok(await _questions.GetQuestionsAsync(ct));

    [HttpPut("mine")]
    [Authorize]
    public async Task<ActionResult<MessageResponse>> SetMineAsync(SetSecurityQuestionRequest request, CancellationToken ct)
        => Ok(await _questions.SetMyQuestionAsync(request, ct));
}
