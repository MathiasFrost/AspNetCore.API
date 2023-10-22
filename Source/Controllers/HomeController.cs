using AspNetCore.API.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]")]
public sealed class HomeController : Controller
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ViewResult> Index(CancellationToken token) => View(await _mediator.Send(new GetWorldsRequest(), token));

    [HttpPost("[action]")]
    public async Task<RedirectToActionResult> Generate([FromForm] int worldsToGenerate)
    {
        if (ModelState.IsValid) await _mediator.Publish(new GenerateWorldsRequest { WorldsToGenerate = worldsToGenerate });
        return RedirectToAction("Index");
    }
}