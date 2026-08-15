using LoanApproval.Application.DTOs;
using LoanApproval.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanApproval.Api.Controllers;

/// <summary>
/// Read-only endpoints backing the member browser UI. Routed to
/// api/Applicants by the [controller] token.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApplicantsController(IApplicantService applicantService) : ControllerBase
{
    /// <summary>
    /// Lists every member, with a count of the applications each has submitted.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicantSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApplicantSummaryResponse>>> GetAll()
    {
        return Ok(await applicantService.GetApplicantsAsync());
    }

    /// <summary>
    /// Returns a single member together with their submitted applications and
    /// the decision recorded against each.
    /// </summary>
    [HttpGet("{memberNumber}")]
    [ProducesResponseType(typeof(ApplicantDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicantDetailResponse>> GetByMemberNumber(string memberNumber)
    {
        var applicant = await applicantService.GetApplicantDetailAsync(memberNumber);

        return applicant is null
            ? NotFound(new { error = $"No applicant found for member number '{memberNumber}'." })
            : Ok(applicant);
    }
}
