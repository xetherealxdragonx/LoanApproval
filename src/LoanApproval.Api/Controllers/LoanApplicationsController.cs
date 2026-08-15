using LoanApproval.Application.DTOs;
using LoanApproval.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanApproval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanApplicationsController(ILoanApplicationService loanApplicationService) : ControllerBase
{
    /// <summary>
    /// Submits a new small-dollar loan application for evaluation and,
    /// if approved, initiates funding - mirroring a Q-Cash style
    /// apply-to-funded flow in a single call.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LoanDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanDecisionResponse>> Submit([FromBody] LoanApplicationRequest request)
    {
        try
        {
            var response = await loanApplicationService.SubmitAsync(request);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
