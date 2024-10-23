namespace Cli.Contracts;

public abstract class Toggle
{
    public string Id { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
    public string Label { get; set; }
    public object Conditions { get; set; }

    public bool IsSalesChannelStatus => SalesChannelStatusId.IsSalesChannelId(Id);

    public abstract string[] TableRowValues();
}

public class FeatureToggle : Toggle
{
    //public Toggle() { }

    //public Toggle(Toggle toggle)
    //{
    //    Id = toggle.Id;
    //    Description = toggle.Description;
    //    Enabled = toggle.Enabled;
    //    Label = toggle.Label;
    //    Conditions = toggle.Conditions;
    //}

    // "id":"Cyrus.TestToggle","description":"","enabled":false,"conditions":{"client_filters":[]}
    //public string Id { get; set; }
    //public string Description { get; set; }
    //public bool Enabled { get; set; }
    //public string Label { get; set; }
    //public object Conditions { get; set; }
    //public bool IsSalesChannelStatus => Id.StartsWith("CHANNEL_");

    public override string ToString()
    {
        return $"Feature:{Id};Description:{Description};Enabled:{Enabled};Environment:{Label}";
    }

    public static string[] TableHeadings()
    {
        return ["Feature", "Enabled", "Environment"];
    }

    public override string[] TableRowValues()
    {
        return [Id, Enabled.ToString(), Label];
    }
}