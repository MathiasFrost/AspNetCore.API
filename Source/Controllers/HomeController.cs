using AspNetCore.API.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]")]
public sealed class HomeController : Controller
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator) => _mediator = mediator;

    [HttpPut("[action]/{str:datetime}")]
    public (string Str, int Num) Test([FromQuery] string test, [FromRoute] DateTime str, [FromForm] MyClass a) => ("haha" + str, (int)a.Class.Test2);
    
    [HttpPut("[action]")]
    public string Test2([FromBody] string test) => ("haha" + test);

    public sealed class MyClass
    {
        public ulong Test { get; init; }
        public MyOtherClass Class { get; init; }
    }

    public sealed class MyOtherClass
    {
        public decimal Test2 { get; init; }
    }

    [HttpGet]
    public async Task<ViewResult> Index(CancellationToken token) => View(await _mediator.Send(new GetWorldsRequest(), token));

    [HttpPost("[action]")]
    public async Task<RedirectToActionResult> Generate([FromForm] int worldsToGenerate)
    {
        if (ModelState.IsValid) await _mediator.Publish(new GenerateWorldsRequest { WorldsToGenerate = worldsToGenerate });
        return RedirectToAction("Index");
    }
}