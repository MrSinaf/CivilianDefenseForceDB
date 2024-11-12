using DataBaseCDF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace DataBaseCDF.Controllers;

public class HomeController(MySqlDataSource db) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        await using var connection = await db.OpenConnectionAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE TRUE;", connection);
        ViewBag.nUsers = (long)(await cmd.ExecuteScalarAsync() ?? 0);

        cmd.CommandText = "SELECT id, name, wanted_score, corporation, divers FROM users WHERE wanted = TRUE ORDER BY wanted_score DESC LIMIT 3;";
        var array = new UserModel[3];
        var i = 0;
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                array[i++] = new UserModel
                {
                    id = reader.GetInt32(0), name = reader.GetString(1), wantedScore = reader.GetInt32(2), corporation = reader.GetString(3), divers = reader.GetString(4)
                };
            }
        }

        // Place le plus recherché au centre :
        (array[0], array[1]) = (array[1], array[0]);

        return View(array);
    }

    [Route("about")]
    public IActionResult About()
    {
        return View();
    }
}