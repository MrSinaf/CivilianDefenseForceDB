using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using DataBaseCDF.Models;
using DataBaseCDF.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

[Route("olds")]
public class OldHomeController(JwtTokenService service, MySqlDataSource db) : Controller
{
    private const string COOKIE_NAME = "jwt";

    [Route("login"), HttpGet]
    public IActionResult Login()
    {
        return View(new LoginModel());
    }

    [Route("login"), HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await using var connection = await db.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT id, password, admin, version FROM members WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("id", Regex.Replace(model.id, @"\D", ""));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var member = new Member(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2), reader.GetInt32(3));
            if (BCrypt.Net.BCrypt.Verify(model.password, member.password))
            {
                var token = service.GenerateToken(member);
                HttpContext.Response.Cookies.Append(COOKIE_NAME, token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
                return RedirectToAction("Index", "OldUsers");
            }
        }

        return View(model);
    }

    [Route("RGPD")]
    public IActionResult About()
    {
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Response.Cookies.Delete(COOKIE_NAME);
        return RedirectToAction("Login", "OldHome");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}