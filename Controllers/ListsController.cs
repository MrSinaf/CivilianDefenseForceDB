using DataBaseCDF.Models;
using DataBaseCDF.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

[Authorize]
public class ListsController(MySqlDataSource db) : Controller
{
	private const int USER_PER_PAGE = 30;
	
	[Route("users")]
	public async Task<IActionResult> Users(string? name, string? corporation, int? state, int page = 1)
	{
		page--;
		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE name LIKE @name AND corporation LIKE @corporation AND "
											   + "(@state > 2 OR @state < 0 OR state = @state);", connection);
		cmd.Parameters.AddWithValue("name", $"%{name ?? ""}%");
		cmd.Parameters.AddWithValue("corporation", $"%{corporation ?? ""}%");
		cmd.Parameters.AddWithValue("state", state ?? -1);
		var nUsers = (long)(await cmd.ExecuteScalarAsync() ?? 0);
		var nPages = (int)MathF.Ceiling(1f * nUsers / USER_PER_PAGE);
		page = page < 0 ? 0 : page > nPages ? nPages : page;
        
		cmd.CommandText= "SELECT id, name, state, corporation, wanted FROM users WHERE name LIKE @name AND corporation LIKE @corporation AND " + 
						 $"(@state > 2 OR @state < 0 OR state = @state) ORDER BY last_update DESC LIMIT {USER_PER_PAGE} OFFSET {page * USER_PER_PAGE};";
		await using var reader = await cmd.ExecuteReaderAsync();
        
		var list = new List<UserModel>();
		while (await reader.ReadAsync())
		{
			list.Add(new UserModel
			{
				id = reader.GetInt32(0),
				name = reader.GetString(1),
				state = (UserModel.State)reader.GetInt16(2),
				corporation = reader.GetString(3),
				isWanted = reader.GetBoolean(4)
			});
		}
        
		ViewBag.name = name;
		ViewBag.state = state;
		ViewBag.corporation = corporation;
		ViewBag.page = page + 1;
		ViewBag.nPages = nPages;
		ViewBag.nUsers = nUsers;
		return View(list);
	}
	
	[Route("wanted")]
	public async Task<IActionResult> Wanted(string? name, string? corporation, int page = 1)
	{
		page--;
		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE name LIKE @name AND corporation LIKE @corporation AND wanted = TRUE;", connection);
		cmd.Parameters.AddWithValue("name", $"%{name ?? ""}%");
		cmd.Parameters.AddWithValue("corporation", $"%{corporation ?? ""}%");
		var nUsers = (long)(await cmd.ExecuteScalarAsync() ?? 0);
		var nPages = (int)MathF.Ceiling(1f * nUsers / USER_PER_PAGE);
		page = page < 0 ? 0 : page > nPages ? nPages : page;
        
		cmd.CommandText= "SELECT id, name, corporation, divers, wanted_score FROM users WHERE name LIKE @name AND corporation LIKE @corporation AND wanted = TRUE" + 
						 $" ORDER BY wanted_score DESC LIMIT {USER_PER_PAGE} OFFSET {page * USER_PER_PAGE};";
		await using var reader = await cmd.ExecuteReaderAsync();
        
		var list = new List<UserModel>();
		while (await reader.ReadAsync())
		{
			list.Add(new UserModel
			{
				id = reader.GetInt32(0),
				name = reader.GetString(1),
				corporation = reader.GetString(2),
				divers = reader.GetString(3),
				wantedScore = reader.GetInt32(4)
			});
		}
        
		ViewBag.name = name;
		ViewBag.corporation = corporation;
		ViewBag.page = page + 1;
		ViewBag.nPages = nPages;
		ViewBag.nUsers = nUsers;
		return View(list);
	}
	
	[AdminAuthorize, Route("members")]
	public async Task<IActionResult> Members(string? name, string? corporation, int? admin, int page = 1)
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
}