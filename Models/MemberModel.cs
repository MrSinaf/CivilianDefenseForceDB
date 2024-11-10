using System.ComponentModel.DataAnnotations;

namespace DataBaseCDF.Models;

public class MemberModel
{
    public const string PASS_PATTERN = @"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

    public int id { get; set; }

    [DataType(DataType.Password), StringLength(125, MinimumLength = 8, ErrorMessage = "Le mot de passe doit comporter entre 8 et 125 caractères."),
     RegularExpression(PASS_PATTERN, ErrorMessage = "Le mot de passe doit contenir au moins une majuscule, un chiffre et un caractère spécial.")]
    public string password { get; set; }

    public bool isAdmin { get; set; }

    public int version;
}