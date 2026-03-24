using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApp.Auth;

namespace WebApp.Pages;

[IgnoreAntiforgeryToken]
public class LoginModel(IOptions<LoginOptions> options) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? UserMessage { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            UserMessage = "Username and password are required.";
            return Page();
        }

        if (Input.Username == options.Value.Username && Input.Password == options.Value.Password)
        {
            HttpContext.Session.SetString(SessionKeys.IsAuthenticated, "true");
            return RedirectToPage("/AddressChecker");
        }

        UserMessage = "Invalid username or password.";
        return Page();
    }

    public sealed class InputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
