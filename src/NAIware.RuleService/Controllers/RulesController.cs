using Microsoft.AspNetCore.Mvc;
using NAIware.RuleService.Contracts;
using NAIware.RuleService.Services;

namespace NAIware.RuleService.Controllers;

/// <summary>
/// Evaluates serialized models (JSON or XML) against a rules library and returns structured results.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RulesController : ControllerBase
{
    private readonly RuleEvaluationService _evaluationService;
    private readonly RuleValidationApiService _validationService;
    private readonly ILogger<RulesController> _logger;

    /// <summary>Creates a new rules controller.</summary>
    public RulesController(
        RuleEvaluationService evaluationService,
        RuleValidationApiService validationService,
        ILogger<RulesController> logger)
    {
        _evaluationService = evaluationService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates a serialized model against the supplied rules library.
    /// </summary>
    /// <param name="request">The evaluation request carrying the model payload and rules library.</param>
    /// <returns>The structured evaluation result.</returns>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(EvaluateModelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<EvaluateModelResponse> Evaluate([FromBody] EvaluateModelRequest request)
    {
        try
        {
            EvaluateModelResponse response = _evaluationService.Evaluate(request);
            return Ok(response);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            // Client-correctable input problems (bad paths, unresolved types, missing payload, etc.).
            _logger.LogWarning(ex, "Rule evaluation rejected: {Message}", ex.Message);
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Evaluation failed");
        }
    }

    /// <summary>
    /// Validates a single draft rule expression against a model type before it is saved.
    /// </summary>
    /// <param name="request">The draft expression and the model type to validate it against.</param>
    /// <returns>The structured validation result, including any errors and warnings.</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ValidationResponse> ValidateExpression([FromBody] ValidateExpressionRequest request)
    {
        try
        {
            ValidationResponse response = _validationService.ValidateExpression(request);
            return Ok(response);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            _logger.LogWarning(ex, "Expression validation rejected: {Message}", ex.Message);
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");
        }
    }

    /// <summary>
    /// Validates an entire rules library document.
    /// </summary>
    /// <param name="request">The library to validate, supplied inline or by path.</param>
    /// <returns>The structured validation result, including any errors and warnings.</returns>
    [HttpPost("validate-library")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ValidationResponse> ValidateLibrary([FromBody] ValidateLibraryRequest request)
    {
        try
        {
            ValidationResponse response = _validationService.ValidateLibrary(request);
            return Ok(response);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            _logger.LogWarning(ex, "Library validation rejected: {Message}", ex.Message);
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");
        }
    }

    /// <summary>Simple liveness probe.</summary>
    /// <returns>An OK result when the service is responsive.</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "healthy" });
}
