using System.Data;
using DataBaseCDF.Models;
using DataBaseCDF.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

[Authorize]
public class UsersController(MySqlDataSource db) : Controller
{
    private const int USER_PER_PAGE = 30;

    [AdminAuthorize, Route("/create"), HttpGet]
    public IActionResult Create()
    {
        return View("Edit", new CreateUserModel());
    }

    [AdminAuthorize, Route("/create"), HttpPost]
    public async Task<IActionResult> Create(CreateUserModel model)
    {
        if (!ModelState.IsValid)
            return View("Edit", model);
        
        await using var connection = await db.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT TRUE FROM users WHERE name = @name;", connection);
        cmd.Parameters.AddWithValue("name", model.name);
        if ((int)(await cmd.ExecuteScalarAsync() ?? 0) == 1)
        {
            ViewBag.error = $"L'utilisateur '{model.name}' existe déjà !";
            return View("Edit", model);
        }

        cmd.CommandText = "SELECT COUNT(*) FROM users WHERE id = @id";
        cmd.Parameters.Add("id", DbType.Int32);
        int id;
        do
        {
            cmd.Parameters["id"].Value = id = Random.Shared.Next(1000000, 9999999);
        } while ((long)(await cmd.ExecuteScalarAsync())! > 0);

        cmd.CommandText = "INSERT INTO users (id, name, wanted, wanted_score, state, corporation, divers, agent) VALUES "
                          + "(@id, @name, @wanted, @wanted_score, @state, @corporation, @divers, @agent);";
        cmd.Parameters.AddWithValue("wanted", model.wanted);
        cmd.Parameters.AddWithValue("wanted_score", model.wantedScore);
        cmd.Parameters.AddWithValue("state", model.state);
        cmd.Parameters.AddWithValue("corporation", model.corporation);
        cmd.Parameters.AddWithValue("divers", model.divers ?? "");
        cmd.Parameters.AddWithValue("agent", User.GetId());
        await cmd.ExecuteNonQueryAsync();

        return Redirect($"/{id}");
    }
    
    
    [Route("/users")]
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
    
    [Route("/wanted")]
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

    [Route("/{id:int}")]
    public async Task<IActionResult> Folder(int id)
    {
        await using var connection = await db.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT id FROM users WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("id", id);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return View(new UserModel
            {
                id = reader.GetInt32(0)
            });
        }
        
        return View();
    }
}