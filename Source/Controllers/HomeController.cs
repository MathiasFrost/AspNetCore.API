using AspNetCore.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.Controllers;

[ApiController, Route("[controller]")]
public class HomeController : Controller
{
    [HttpGet]
    public ViewResult Index() => View(new ContactForm());

    [HttpPost]
    public IActionResult SubmitForm([FromForm] ContactForm form)
    {
        if (ModelState.IsValid)
            // Process the form, e.g., save to a database or send an email.
            return RedirectToAction("Index");
        else
            // If model validation fails, show the form again with validation messages.
            return View("Index", form);
    }
}