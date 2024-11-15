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
	[AdminAuthorize, Route("/create"), HttpGet]
	public IActionResult Create()
	{
		ViewBag.edit = false;
		return View("Edit", new CreateUserModel());
	}

	[AdminAuthorize, Route("/create"), HttpPost]
	public async Task<IActionResult> Create(CreateUserModel model)
	{
		if (!ModelState.IsValid)
			return View("Edit", model);

		ViewBag.edit = false;
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


	[AdminAuthorize, Route("/{id:int}/edit"), HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("SELECT name, wanted, wanted_score, state, corporation, divers FROM users WHERE id = @id;", connection);
		cmd.Parameters.AddWithValue("id", id);
		await using var reader = await cmd.ExecuteReaderAsync();

		if (await reader.ReadAsync())
		{
			ViewBag.edit = true;
			return View(new CreateUserModel
			{
				name = reader.GetString(0), wanted = reader.GetBoolean(1), wantedScore = reader.GetInt32(2), state = (UserModel.State)reader.GetInt32(3),
				corporation = reader.GetString(4), divers = reader.GetString(5), id = id
			});
		}

		return NotFound();
	}

	[AdminAuthorize, Route("/{id:int}/edit"), HttpPost]
	public async Task<IActionResult> Edit(CreateUserModel model)
	{
		ViewBag.edit = true;
		if (!ModelState.IsValid)
			return View(model);

		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("SELECT TRUE FROM users WHERE id = @id;", connection);
		cmd.Parameters.AddWithValue("id", model.id);
		if ((int)(await cmd.ExecuteScalarAsync() ?? 0) == 0)
		{
			ViewBag.error = $"Aucun utilisateur avec l'id {model.id} a été trouvé.";
			return View(model);
		}
		
		cmd.CommandText = "UPDATE users SET name = @name, wanted = @wanted, wanted_score = @score, state = @state, corporation = @corporation, divers = @divers WHERE id = @id;";
		cmd.Parameters.AddWithValue("name", model.name);
		cmd.Parameters.AddWithValue("wanted", model.wanted);
		cmd.Parameters.AddWithValue("score", model.wantedScore);
		cmd.Parameters.AddWithValue("state", model.state);
		cmd.Parameters.AddWithValue("corporation", model.corporation);
		cmd.Parameters.AddWithValue("divers", model.divers);
		try
		{
			await cmd.ExecuteNonQueryAsync();
		}
		catch(Exception ex)
		{
			ViewBag.error = $"Impossible de continuer : {ex.Message}";
			return View(model);
		}
		
		return Redirect($"/{model.id}");
	}

	[Route("/{id:int}")]
	public async Task<IActionResult> Folder(int id)
	{
		await using var connection = await db.OpenConnectionAsync();
		await using var cmd = new MySqlCommand("SELECT message, author FROM criminal_records WHERE target = @id;", connection);
		cmd.Parameters.AddWithValue("id", id);
		var records = new List<CriminalRecord>();
		await using (var readerRecords = await cmd.ExecuteReaderAsync())
			while (await readerRecords.ReadAsync())
				records.Add(new CriminalRecord { message = readerRecords.GetString(0), authorId = readerRecords.GetInt32(1) });

		cmd.CommandText = "SELECT id, wanted, wanted_score, corporation, divers, last_update, agent, name FROM users WHERE id = @id;";

		await using var reader = await cmd.ExecuteReaderAsync();
		if (await reader.ReadAsync())
		{
			return View(new FolderModel(new UserModel
			{
				id = reader.GetInt32(0),
				isWanted = reader.GetBoolean(1),
				wantedScore = reader.GetInt32(2),
				corporation = reader.GetString(3),
				divers = reader.GetString(4),
				lastUpdate = reader.GetDateTime(5),
				agentId = reader.GetInt32(6),
				name = reader.GetString(7)
			}, records));
		}

		return NotFound();
	}
}