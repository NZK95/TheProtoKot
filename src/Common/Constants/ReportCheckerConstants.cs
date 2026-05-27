internal static class ReportCheckerConstants
{
    public const int ACCOUNTS_LIMIT_PER_USER = 2;

    public static readonly List<string> Red = new()
    {
        "https://amazing-online.com/my/complaints/1", 
        "https://amazing-online.com/my/complaints/2",  
        "https://amazing-online.com/my/complaints/3",  
        "https://amazing-online.com/my/complaints/4",  
        "https://amazing-online.com/my/complaints/5", 
        "https://amazing-online.com/my/complaints/6", 
    };

    public static readonly List<string> Yellow = new()
    {
        "https://amazing-online.com/my/complaints/8",  
        "https://amazing-online.com/my/complaints/9", 
        "https://amazing-online.com/my/complaints/10", 
        "https://amazing-online.com/my/complaints/11", 
        "https://amazing-online.com/my/complaints/12", 
        "https://amazing-online.com/my/complaints/13", 
    };

    public static readonly List<string> Green = new()
    {
        "https://amazing-online.com/my/complaints/15",
        "https://amazing-online.com/my/complaints/16", 
        "https://amazing-online.com/my/complaints/17", 
        "https://amazing-online.com/my/complaints/18", 
        "https://amazing-online.com/my/complaints/19", 
        "https://amazing-online.com/my/complaints/20",
    };

    public static readonly List<string> Azure = new()
    {
        "https://amazing-online.com/my/complaints/28", 
        "https://amazing-online.com/my/complaints/22", 
        "https://amazing-online.com/my/complaints/23", 
        "https://amazing-online.com/my/complaints/24", 
        "https://amazing-online.com/my/complaints/25", 
        "https://amazing-online.com/my/complaints/26", 
        "https://amazing-online.com/my/complaints/27", 
    };

    public static readonly List<string> Silver = new()
    {
        "https://amazing-online.com/my/complaints/29", 
        "https://amazing-online.com/my/complaints/30", 
        "https://amazing-online.com/my/complaints/31", 
        "https://amazing-online.com/my/complaints/32", 
        "https://amazing-online.com/my/complaints/33", 
        "https://amazing-online.com/my/complaints/34", 
    };

    public static readonly List<string> Rose = new()
    {
        "https://amazing-online.com/my/complaints/36", 
        "https://amazing-online.com/my/complaints/37",
        "https://amazing-online.com/my/complaints/38",
        "https://amazing-online.com/my/complaints/39",
        "https://amazing-online.com/my/complaints/40",
        "https://amazing-online.com/my/complaints/41", 
    };

    public static readonly List<string> Black = new()
    {
        "https://amazing-online.com/my/complaints/43",
        "https://amazing-online.com/my/complaints/44", 
        "https://amazing-online.com/my/complaints/45", 
        "https://amazing-online.com/my/complaints/46", 
        "https://amazing-online.com/my/complaints/47", 
        "https://amazing-online.com/my/complaints/48", 
    };

    public static readonly List<string> Sky = new()
    {
        "https://amazing-online.com/my/complaints/54",
        "https://amazing-online.com/my/complaints/55", 
        "https://amazing-online.com/my/complaints/56",
        "https://amazing-online.com/my/complaints/57", 
        "https://amazing-online.com/my/complaints/58", 
        "https://amazing-online.com/my/complaints/59", 
    };

    public static readonly List<string> Titan = new()
    {
        "https://amazing-online.com/my/complaints/61", 
        "https://amazing-online.com/my/complaints/62",
        "https://amazing-online.com/my/complaints/63", 
        "https://amazing-online.com/my/complaints/64", 
        "https://amazing-online.com/my/complaints/65", 
        "https://amazing-online.com/my/complaints/66",
    };

    public static readonly List<string> X = new()
    {
        "https://amazing-online.com/my/complaints/68", 
        "https://amazing-online.com/my/complaints/69",
        "https://amazing-online.com/my/complaints/70",
        "https://amazing-online.com/my/complaints/71", 
        "https://amazing-online.com/my/complaints/72", 
        "https://amazing-online.com/my/complaints/73", 
    };

    public static readonly List<string> Fire = new()
    {
        "https://amazing-online.com/my/complaints/75", 
        "https://amazing-online.com/my/complaints/76", 
        "https://amazing-online.com/my/complaints/77",
        "https://amazing-online.com/my/complaints/78", 
        "https://amazing-online.com/my/complaints/79",
        "https://amazing-online.com/my/complaints/80",
    };

    public static readonly List<string> Lime = new()
    {
        "https://amazing-online.com/my/complaints/82",
        "https://amazing-online.com/my/complaints/83",
        "https://amazing-online.com/my/complaints/84", 
        "https://amazing-online.com/my/complaints/85",
        "https://amazing-online.com/my/complaints/86", 
        "https://amazing-online.com/my/complaints/87",
    };

    public static readonly Dictionary<string, List<string>> ByServer = new()
    {
        { "Red",    Red    },
        { "Yellow", Yellow },
        { "Green",  Green  },
        { "Azure",  Azure  },
        { "Silver", Silver },
        { "Rose",   Rose   },
        { "Black",  Black  },
        { "Sky",    Sky    },
        { "Titan",  Titan  },
        { "X",      X      },
        { "Fire",   Fire   },
        { "Lime",   Lime   },
    };

    public static List<string>? GetLinks(string server) =>
        ByServer.TryGetValue(server, out var links) ? links : null;
}
