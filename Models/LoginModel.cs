using System.ComponentModel.DataAnnotations;

namespace DataBaseCDF.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Vous devez renseigner un identifiant."), DataType(DataType.Text)] public string id { get; set; }
    [Required(ErrorMessage = "Vous devez renseigner un mot de passe."), DataType(DataType.Password)] public string password { get; set; }
}