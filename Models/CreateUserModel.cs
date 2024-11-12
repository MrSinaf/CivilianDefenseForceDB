using System.ComponentModel.DataAnnotations;

namespace DataBaseCDF.Models;

public class CreateUserModel
{
	[Required(ErrorMessage = "Un nom d'utilisateur est requis.")] public string name { get; set; }
	[Required(ErrorMessage = "La corporation est requise.")] public string corporation { get; set; }
	public string? divers { get; set; }
	public bool wanted  { get; set; }
	public int wantedScore { get; set; }
	[Required] public UserModel.State state { get; set; } = UserModel.State.Neutral;
}