using System.Collections;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsageSync.Api.Data;
using UsageSync.Api.Infrastructure;

namespace UsageSync.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usage")]
public class UsageController : ControllerBase
{
    private readonly UsageDbContext _db;

    public UsageController(UsageDbContext db)
    {
        _db = db;
    }

    [HttpGet("export")]
    public IActionResult Export([FromQuery] string month)
    {
        if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var monthStartUtc))
        {
            return BadRequest("month must be in yyyy-MM format.");
        }

        var monthEndUtc = monthStartUtc.AddMonths(1);
        var tenantId = User.GetTenantId();

        var rows = _db.UsageRecords
            .Where(r => r.TenantId == tenantId
                        && r.UsageDateUtc >= monthStartUtc
                        && r.UsageDateUtc <= monthEndUtc)
            .OrderBy(r => r.UserEmail)
            .ThenBy(r => r.Product);

        if (!rows.Any())
        {
            return NoContent();
        }

        var csv = "UserEmail,Product,Units,Cost\n";
        var totalsByProduct = new Hashtable();

        foreach (var row in rows)
        {
            var cost = Math.Round(row.Units * row.UnitPrice, 2);
            csv += $"{row.UserEmail},{row.Product},{row.Units},{cost.ToString(CultureInfo.InvariantCulture)}\n";

            var runningTotal = totalsByProduct[row.Product] is double existing ? existing : 0d;
            totalsByProduct[row.Product] = runningTotal + cost;
        }

        csv += "\nProduct,TotalCost\n";
        foreach (DictionaryEntry entry in totalsByProduct)
        {
            csv += $"{entry.Key},{((double)entry.Value!).ToString("F2", CultureInfo.InvariantCulture)}\n";
        }

        var grandTotal = rows.Sum(r => r.Units * r.UnitPrice);
        csv += $"\nRows,{rows.Count()}\nGrandTotal,{grandTotal.ToString("F2", CultureInfo.InvariantCulture)}\n";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"usage-{month}.csv");
    }
}
