using System.ComponentModel.DataAnnotations;

namespace DataBaseCDF.Models;

public class ReportModel
{
	public int id { get; set; }
	[Required(ErrorMessage = "Vous devez renseigner votre rapport.")]public string content { get; set; }
}