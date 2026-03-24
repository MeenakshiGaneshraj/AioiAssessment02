using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Auth;

namespace WebApp.Pages;

public class AddressCheckerModel : PageModel
{
    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString(SessionKeys.IsAuthenticated) != "true")
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }
}
