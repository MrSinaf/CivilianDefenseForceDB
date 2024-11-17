using System.Text.RegularExpressions;
using DataBaseCDF.Models;
using DataBaseCDF.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

[Route("members")]
public class MembersController(JwtTokenService service, MySqlDataSource db) : Controller
{
	private const string COOKIE_NAME = "jwt";

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

	[AdminAuthorize, Route("/{id:int}/member"), HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("SELECT admin FROM members WHERE id = @id;", connection);
		cmd.Parameters.AddWithValue("id", id);
		var result = await cmd.ExecuteScalarAsync();
		ViewBag.exist = result != null;
		
		return View(new MemberModel { id = id, isAdmin = (bool)(result ?? false)});
	}

	[AdminAuthorize, Route("/{id:int}/member"), HttpPost]
	public async Task<IActionResult> Edit(MemberModel model)
	{
		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("SELECT TRUE FROM members WHERE id = @id;", connection);
		cmd.Parameters.AddWithValue("id", model.id);
		cmd.Parameters.AddWithValue("admin", model.isAdmin);

		var exist = (int)(await cmd.ExecuteScalarAsync() ?? 0) == 1;
		ViewBag.exist = exist;
		if (!exist)	// Si c'est un nouveau membre.
		{
			if (!ModelState.IsValid)
				return View(model);
			
			cmd.CommandText = "INSERT INTO members (id, admin, password) VALUES (@id, @admin, @password);";
			cmd.Parameters.AddWithValue("password", BCrypt.Net.BCrypt.HashPassword(model.password));
			await cmd.ExecuteNonQueryAsync();
			return Redirect($"/{model.id}");
			
		}

		cmd.CommandText = "UPDATE members SET admin = @admin, version = version + 1";
		if (!model.password.IsNullOrEmpty())	// Vérifie si le mot est à mettre à jour.
		{
			if (!ModelState.IsValid)
				return View(model);
			
			cmd.CommandText += ", password = @password";
			cmd.Parameters.AddWithValue("password", BCrypt.Net.BCrypt.HashPassword(model.password));
		}
		cmd.CommandText += " WHERE id = @id;";
		await cmd.ExecuteNonQueryAsync();
		
		return Redirect($"/{model.id}");
	}

	[AdminAuthorize, Route("/{id:int}/member/remove")]
	public async Task<IActionResult> Remove(int id)
	{
		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("DELETE FROM members WHERE id = @id;", connection);
		cmd.Parameters.AddWithValue("id", id);
		await cmd.ExecuteNonQueryAsync();
		
		return Redirect($"/{id}");
	}

	[Route("/logout")]
	public IActionResult Logout()
	{
		HttpContext.Response.Cookies.Delete(COOKIE_NAME);
		ViewBag.error = "Vous venez d'être déconnecté !";
		return RedirectToAction("Login");
	}
}