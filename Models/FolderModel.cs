namespace DataBaseCDF.Models;

public record class FolderModel(UserModel user, IEnumerable<CriminalRecord> records);