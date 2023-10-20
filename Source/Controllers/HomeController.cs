using AspNetCore.API.Handlers;
using AspNetCore.API.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]")]
public class HomeController : Controller
{
    private static readonly ICollection<WeatherForecast> Forecasts = new List<WeatherForecast>();

    private readonly IMediator _mediator;

    public HomeController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ViewResult> Index(CancellationToken token) => View((await _mediator.Send(new WeatherForecastRequest(), token)).Concat(Forecasts));

    [HttpPost("[action]")]
    public IActionResult Add([FromForm] WeatherForecast form)
    {
        if (ModelState.IsValid) Forecasts.Add(form);
        return RedirectToAction("Index");
    }
}