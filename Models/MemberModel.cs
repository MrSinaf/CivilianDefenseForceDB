using System.ComponentModel.DataAnnotations;

namespace DataBaseCDF.Models;

public class MemberModel
{
    public int id { get; set; }

    [Required(ErrorMessage = "Veuillez entrer un mot de passe."), DataType(DataType.Password),
     StringLength(125, MinimumLength = 3, ErrorMessage = "Le mot de passe doit comporter entre 3 et 125 caractères.")]
    public string password { get; set; }

    public bool isAdmin { get; set; }

    public int version;
}