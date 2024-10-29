namespace DataBaseCDF.Models;

public record class Member(int id, string password, bool admin, int version);