using System.Text.RegularExpressions;
using DataBaseCDF.Models;
using DataBaseCDF.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

[Route("members")]
public class MembersController(JwtTokenService service, MySqlDataSource db) : Controller
{
    private const string COOKIE_NAME = "jwt";
    private const int USER_PER_PAGE = 42;
    
    [AdminAuthorize]
    public async Task<IActionResult> Index(string? name, string? corporation, int? admin, int page = 1)
    {
        page--;
        await using var connection = await db.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM members m INNER JOIN users u ON m.id = u.id WHERE name LIKE @name AND corporation LIKE @corporation AND "
                                               + "(@admin > 2 OR @admin < 0 OR admin = @admin);", connection);
        cmd.Parameters.AddWithValue("name", $"%{name ?? ""}%");
        cmd.Parameters.AddWithValue("corporation", $"%{corporation ?? ""}%");
        cmd.Parameters.AddWithValue("admin", admin ?? -1);
        var nUsers = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        var nPages = (int)MathF.Ceiling(1f * nUsers / USER_PER_PAGE);
        page = page < 0 ? 0 : page > nPages ? nPages : page;
        
        cmd.CommandText= "SELECT m.id, name, admin, corporation FROM members m INNER JOIN users u ON m.id = u.id WHERE name LIKE @name AND corporation LIKE @corporation " +
                         $"AND (@admin > 2 OR @admin < 0 OR admin = @admin) ORDER BY last_update DESC LIMIT {USER_PER_PAGE} OFFSET {page * USER_PER_PAGE};";
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var list = new List<UserModel>();
        while (await reader.ReadAsync())
        {
            list.Add(new UserModel
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                isAdmin = reader.GetBoolean(2),
                corporation = reader.GetString(3),
            });
        }
        
        ViewBag.name = name;
        ViewBag.admin = admin;
        ViewBag.corporation = corporation;
        ViewBag.page = page + 1;
        ViewBag.nPages = nPages;
        ViewBag.nUsers = nUsers;
        return View(list);
    }
    
    [Route("/login"), HttpGet]
    public IActionResult Login() => View(new LoginModel());

    [Route("/login"), HttpPost]
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
                return RedirectToAction("Index", "Home");
            }
        }
        
        ViewBag.error = "L'identifiant ou le mot de passe est invalide !";
        return View(model);
    }
    
    [Route("/logout")]
    public IActionResult Logout()
    {
        HttpContext.Response.Cookies.Delete(COOKIE_NAME);
        ViewBag.error = "Vous venez d'être déconnecté !";
        return RedirectToAction("Login");
    }
}