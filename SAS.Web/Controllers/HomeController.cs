using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SAS.Application.Contracts;
using SAS.Web.Models;

namespace SAS.Web.Controllers;

public class HomeController : Controller
{
    private readonly IQuoteStorage _quoteStorage;

    public HomeController(IQuoteStorage quoteStorage)
    {
        _quoteStorage = quoteStorage;
    }

    public IActionResult Index()
    {
        return View(_quoteStorage);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult DebugInfo()
    {
        return Ok(new
        {
            totalSaved = _quoteStorage.TotalSaved,
            totalDuplicates = _quoteStorage.TotalDuplicates,
            totalDropped = _quoteStorage.TotalDropped,
            lastBatchSize = _quoteStorage.LastBatchSize,
            lastBatchPause = _quoteStorage.LastBatchPause
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
