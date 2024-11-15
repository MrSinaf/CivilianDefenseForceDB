namespace DataBaseCDF.Models;

public class UserModel
{
    public enum State { Ally, Neutral, Hostile }

    public int id { get; set; }
    public string name { get; set; }
    public State state { get; set; }
    public string corporation { get; set; }
    public string? divers { get; set; }
    public int agentId { get; set; }
    public bool isWanted { get; set; }
    public int wantedScore { get; set; }
    public DateTime lastUpdate { get; set; }
    public bool isAdmin;
    public bool isMember;
    public CriminalRecord[] folders;

    public static string StateToString(State state) => state switch
    {
        State.Ally => "Allié",
        State.Neutral => "Neutre",
        State.Hostile => "Hostile",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    public static string GetStringId(int id) => $"{id:CDF-###-####}";

    public string GetStringId() => GetStringId(id);
}