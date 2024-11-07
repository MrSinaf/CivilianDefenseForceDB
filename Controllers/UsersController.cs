using System.Data;
using Microsoft.AspNetCore.Mvc;
using DataBaseCDF.Models;
using DataBaseCDF.Services;
using Microsoft.AspNetCore.Authorization;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

[Authorize]
public class UsersController : Controller
{
    public async Task<IActionResult> Index(string name = "", string corporation = "", int state = -1, int page = 1)
    {
        page--;
        const int userPerPage = 30;
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE name LIKE @name AND corporation LIKE @corporation AND "
                                               + "(@state > 2 OR @state < 0 OR state = @state);", connection);
        cmd.Parameters.AddWithValue("name", $"%{name}%");
        cmd.Parameters.AddWithValue("corporation", $"%{corporation}%");
        cmd.Parameters.AddWithValue("state", state);
        var nUsers = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        var nPages = (int)MathF.Ceiling(1f * nUsers / userPerPage);
        page = page < 0 ? 0 : page > nPages ? nPages : page;
        
        cmd.CommandText = "SELECT id, name, state, corporation, wanted FROM users WHERE name LIKE @name AND corporation LIKE @corporation AND " + 
                          $"(@state > 2 OR @state < 0 OR state = @state) ORDER BY last_update DESC LIMIT {userPerPage} OFFSET {page * userPerPage};";
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

    [Route("{id}")]
    public async Task<IActionResult> Profil(int id)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT message, author FROM criminal_records WHERE target = @id ORDER BY id DESC;", connection);
        cmd.Parameters.AddWithValue("id", id);
        var folders = new List<Folder>();
        await using (var readerFolder = await cmd.ExecuteReaderAsync())
        {
            while (await readerFolder.ReadAsync())
                folders.Add(new Folder { message = readerFolder.GetString(0), authorId = readerFolder.GetInt32(1) });
        }

        var isMember = false;
        cmd.CommandText = "SELECT TRUE FROM members WHERE id = @id;";
        await using (var readerMember = await cmd.ExecuteReaderAsync())
        {
            if (await readerMember.ReadAsync())
                isMember = true;
        }

        cmd.CommandText = "SELECT id, name, state, corporation, divers, last_update, wanted, wanted_score, agent FROM users WHERE id = @id;";
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return View(new UserModel
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                state = (UserModel.State)reader.GetInt16(2),
                corporation = reader.GetString(3),
                divers = reader.GetString(4),
                lastUpdate = reader.GetDateTime(5),
                folders = folders.ToArray(),
                isWanted = reader.GetBoolean(6),
                wantedScore = reader.GetInt32(7),
                agentId = reader.GetInt32(8),
                isMember = isMember
            });
        }

        return NotFound();
    }

    [Route("{id}/report"), HttpGet]
    public IActionResult Report(string id)
    {
        ViewBag.targetId = id;
        return View();
    }

    [Route("{id}/report"), HttpPost]
    public async Task<IActionResult> Report(string id, string message)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("INSERT INTO criminal_records (target, message, author) VALUES (@target, @message, @author);", connection);
        cmd.Parameters.AddWithValue("target", id);
        cmd.Parameters.AddWithValue("author", User.GetId());
        cmd.Parameters.AddWithValue("message", message);
        await cmd.ExecuteNonQueryAsync();

        return Redirect($"/{id}");
    }

    [Route("wanted")]
    public async Task<IActionResult> Wanted(string? name, string? corporation, int page = 1)
    {
        page--;
        const int userPerPage = 30;
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE wanted = true AND name LIKE @name AND corporation LIKE @corporation;", connection);
        cmd.Parameters.AddWithValue("name", $"%{name}%");
        cmd.Parameters.AddWithValue("corporation", $"%{corporation}%");
        var nUsers = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        var nPages = (int)MathF.Ceiling(1f * nUsers / userPerPage);
        page = page < 0 ? 0 : page > nPages ? nPages : page;
        
        cmd.CommandText = "SELECT id, name, corporation, wanted_score FROM users WHERE wanted = true AND name LIKE @name AND corporation LIKE @corporation " + 
                          $"ORDER BY wanted_score DESC LIMIT {userPerPage} OFFSET {page * userPerPage};";
        await using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<UserModel>();
        while (await reader.ReadAsync())
        {
            list.Add(new UserModel
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                corporation = reader.GetString(2),
                wantedScore = reader.GetInt32(3)
            });
        }

        ViewBag.name = name;
        ViewBag.corporation = corporation;
        ViewBag.page = page + 1;
        ViewBag.nPages = nPages;
        ViewBag.nUsers = nUsers;
        return View(list);
    }

    [AdminAuthorize, Route("{id}/edit"), HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT message, author FROM criminal_records WHERE target = @id ORDER BY id DESC;", connection);
        cmd.Parameters.AddWithValue("id", id);
        var folders = new List<Folder>();
        await using (var readerFolder = await cmd.ExecuteReaderAsync())
        {
            while (await readerFolder.ReadAsync())
                folders.Add(new Folder { message = readerFolder.GetString(0), authorId = readerFolder.GetInt32(1) });
        }

        cmd.CommandText = "SELECT name, wanted, wanted_score, state, corporation, divers FROM users WHERE id = @id;";
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return View(new UserModel
            {
                id = id,
                name = reader.GetString(0),
                isWanted = reader.GetBoolean(1),
                wantedScore = reader.GetInt32(2),
                state = (UserModel.State)reader.GetInt16(3),
                corporation = reader.GetString(4),
                divers = reader.GetString(5),
                folders = folders.ToArray()
            });
        }

        return NotFound();
    }

    [AdminAuthorize, Route("{id:int}/edit"), HttpPost]
    public async Task<IActionResult> Edit(int id, UserModel model)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd =
            new MySqlCommand(
                "UPDATE users SET name = @name, corporation = @corporation, divers = @divers, state = @state, wanted = @wanted, "
                + "wanted_score = @score, agent = @agent, last_update = CURRENT_TIMESTAMP WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", model.name);
        cmd.Parameters.AddWithValue("corporation", model.corporation);
        cmd.Parameters.AddWithValue("divers", model.divers ?? "");
        cmd.Parameters.AddWithValue("state", model.state);
        cmd.Parameters.AddWithValue("wanted", model.isWanted);
        cmd.Parameters.AddWithValue("score", model.wantedScore);
        cmd.Parameters.AddWithValue("agent", model.agentId);
        await cmd.ExecuteNonQueryAsync();

        return Redirect($"/{id}");
    }

    [AdminAuthorize, Route("create"), HttpGet]
    public IActionResult Create()
    {
        return View("Edit", new UserModel());
    }

    [AdminAuthorize, Route("create"), HttpPost]
    public async Task<IActionResult> Create(UserModel model)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE id = @id", connection);
        cmd.Parameters.Add("id", DbType.Int32);
        int id;
        do
        {
            cmd.Parameters["id"].Value = id = Random.Shared.Next(1000000, 9999999);
        } while ((long)(await cmd.ExecuteScalarAsync())! > 0);

        cmd.CommandText = "INSERT INTO users (id, name, wanted, wanted_score, state, corporation, divers, agent) VALUES "
                          + "(@id, @name, @wanted, @wanted_score, @state, @corporation, @divers, @agent);";
        cmd.Parameters.AddWithValue("name", model.name);
        cmd.Parameters.AddWithValue("wanted", model.isWanted);
        cmd.Parameters.AddWithValue("wanted_score", model.wantedScore);
        cmd.Parameters.AddWithValue("state", model.state);
        cmd.Parameters.AddWithValue("corporation", model.corporation);
        cmd.Parameters.AddWithValue("divers", model.divers ?? "");
        cmd.Parameters.AddWithValue("agent", User.GetId());
        await cmd.ExecuteNonQueryAsync();

        return Redirect($"/{id}");
    }

    [AdminAuthorize, Route("{id:int}/member"), HttpGet]
    public async Task<IActionResult> Member(int id)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT admin FROM members WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            ViewBag.isNew = false;
            ViewBag.isAdmin = reader.GetBoolean(0);
        }
        else
        {
            ViewBag.isNew = true;
            ViewBag.isAdmin = false;
        }

        ViewBag.id = id;
        return View();
    }

    [AdminAuthorize, Route("{id:int}/member"), HttpPost]
    public async Task<IActionResult> Member(int id, string password, bool isAdmin, bool isNew)
    {
        if (isNew)
        {
            await using var connection = await DataBase.cdf.OpenConnectionAsync();
            await using var cmd = new MySqlCommand("INSERT INTO members (id, admin, password) VALUES (@id, @admin, @password);", connection);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("admin", isAdmin);
            cmd.Parameters.AddWithValue("password", BCrypt.Net.BCrypt.HashPassword(password));
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            await using var connection = await DataBase.cdf.OpenConnectionAsync();
            await using var cmd = new MySqlCommand("UPDATE members SET admin = @admin, password = @password, version = version + 1 WHERE id = @id;", connection);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("admin", isAdmin);
            cmd.Parameters.AddWithValue("password", BCrypt.Net.BCrypt.HashPassword(password));
            await cmd.ExecuteNonQueryAsync();
        }

        return Redirect($"/{id}");
    }

    [AdminAuthorize, Route("/members")]
    public async Task<IActionResult> Members(string name = "", string corporation = "", int isAdmin = -1, int page = 1)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT m.id, m.admin, u.name, u.corporation FROM members m INNER JOIN users u ON m.id = u.id WHERE " +
                                               "u.name LIKE @name AND u.corporation LIKE @corporation AND (m.admin = @isAdmin OR @isAdmin = -1);", connection);
        cmd.Parameters.AddWithValue("name", $"%{name}%");
        cmd.Parameters.AddWithValue("corporation", $"%{corporation}%");
        cmd.Parameters.AddWithValue("isAdmin", isAdmin);
        await using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<UserModel>();
        while (await reader.ReadAsync())
            list.Add(new UserModel { id = reader.GetInt32(0), isAdmin = reader.GetBoolean(1), name = reader.GetString(2), corporation = reader.GetString(3) });

        ViewBag.name = name;
        ViewBag.isAdmin = isAdmin;
        ViewBag.corporation = corporation;
        return View(list);
    }

    [AdminAuthorize, Route("{id:int}/remove")]
    public async Task<IActionResult> RemoveMember(int id)
    {
        await using var connection = await DataBase.cdf.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("DELETE FROM members WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();

        return Redirect($"/{id}");
    }
}