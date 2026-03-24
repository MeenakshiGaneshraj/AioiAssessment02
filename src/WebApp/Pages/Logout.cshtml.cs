using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Auth;

namespace WebApp.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnPost()
    {
        HttpContext.Session.Remove(SessionKeys.IsAuthenticated);
        return RedirectToPage("/Login");
    }
}
