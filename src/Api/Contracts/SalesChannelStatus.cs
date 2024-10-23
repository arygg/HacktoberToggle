namespace Cli.Contracts;

public class SalesChannelStatus : Toggle
{
    private SalesChannelStatusId _id;

    private SalesChannelStatusId SalesChannelStatusId=> _id ?? SalesChannelStatusId.Parse(Id);

    public string SalesProcessId => SalesChannelStatusId.SalesProcessId;
    public string NautilusShopId => SalesChannelStatusId.NautilusId;

    public static string[] TableHeadings()
    {
        return ["Sales Process Id", "Nautilus Id", "Description", "Environment", "Status"];
    }

    public override string[] TableRowValues()
    {
        return [SalesProcessId, NautilusShopId, Description, Label, Enabled ? "Online" : "Offline"];
    }
}