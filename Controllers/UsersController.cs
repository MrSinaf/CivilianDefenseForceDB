using DataBaseCDF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

[Authorize, Route("/users")]
public class UsersController(MySqlDataSource db) : Controller
{
    private const int USER_PER_PAGE = 30;
    
    public async Task<IActionResult> Index(string? name, string? corporation, int? state, int page = 1)
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

    [Route("{id:int}")]
    public async Task<IActionResult> User(int id)
    {
        return View();
    }
}