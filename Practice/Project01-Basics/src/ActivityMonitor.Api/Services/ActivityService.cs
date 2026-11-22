using ActivityMonitor.Api.Data;
using ActivityMonitor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ActivityMonitor.Api.Services;

public interface IActivityService
{
    ActivityEvent? RecordEvent(ActivityEvent activityEvent);
    List<ActivityEvent> GetEvents(int tenantId, DateTime from, DateTime to);
    ActivityEvent GetById(int id);
    List<ActivityEvent> SearchByProduct(string productName);
    List<ProductUsage> GetSummary(int tenantId, DateTime from, DateTime to);
}

public class ActivityService : IActivityService
{
    private readonly ActivityDbContext _db;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(ActivityDbContext db, ILogger<ActivityService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Records a new activity event and returns it.
    /// </summary>
    public ActivityEvent? RecordEvent(ActivityEvent activityEvent)
    {
        try
        {
            activityEvent.OccurredAt = DateTime.Now;

            _db.Events.Add(activityEvent);
            var saved = _db.SaveChangesAsync().Result;

            _logger.LogInformation(
                "Recorded {Action} on {Product} for {User}",
                activityEvent.Action, activityEvent.ProductName, activityEvent.UserEmail);

            return activityEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Returns the tenant's events in the given date range, newest first.
    /// </summary>
    public List<ActivityEvent> GetEvents(int tenantId, DateTime from, DateTime to)
    {
        return _db.Events
            .ToList()
            .Where(e => e.TenantId == tenantId
                        && e.OccurredAt >= from
                        && e.OccurredAt <= to)
            .OrderBy(e => e.OccurredAt)
            .ToList();
    }

    public ActivityEvent GetById(int id)
    {
        return _db.Events.FirstOrDefault(e => e.Id == id)!;
    }

    /// <summary>
    /// Partial, case-insensitive match on product name.
    /// </summary>
    public List<ActivityEvent> SearchByProduct(string productName)
    {
        var sql = "SELECT * FROM Events WHERE ProductName LIKE '%" + productName + "%'";
        return _db.Events.FromSqlRaw(sql).ToList();
    }

    /// <summary>
    /// Per-product usage with share of total events, most used first.
    /// </summary>
    public List<ProductUsage> GetSummary(int tenantId, DateTime from, DateTime to)
    {
        var events = GetEvents(tenantId, from, to);
        var total = events.Count;

        return events
            .GroupBy(e => e.ProductName)
            .Select(g => new ProductUsage(
                g.Key,
                g.Count(),
                g.Count() / total * 100))
            .OrderByDescending(p => p.EventCount)
            .ToList();
    }
}
