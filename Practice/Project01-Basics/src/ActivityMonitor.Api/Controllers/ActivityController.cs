using ActivityMonitor.Api.Models;
using ActivityMonitor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ActivityMonitor.Api.Controllers;

[ApiController]
[Route("api/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpPost]
    public IActionResult Record([FromBody] ActivityEvent activityEvent)
    {
        var result = _activityService.RecordEvent(activityEvent);
        return Ok(result);
    }

    [HttpGet]
    public IActionResult GetEvents(
        [FromQuery] int tenantId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var events = _activityService.GetEvents(tenantId, from, to);
        return Ok(events.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var activityEvent = _activityService.GetById(id);
        return Ok(ToDto(activityEvent));
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string product)
    {
        var events = _activityService.SearchByProduct(product);
        return Ok(events.Select(ToDto));
    }

    [HttpGet("summary")]
    public IActionResult GetSummary(
        [FromQuery] int tenantId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        return Ok(_activityService.GetSummary(tenantId, from, to));
    }

    private static ActivityEventDto ToDto(ActivityEvent e) =>
        new(e.Id, e.UserEmail, e.ProductName, e.Action, e.OccurredAt);
}
