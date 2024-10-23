using System;

namespace Cli.Contracts;

public class AppSetting
{
    public string Etag { get; set; }
    public string Key { get; set; }
    public string Label { get; set; }
    public string Content_Type { get; set; }
    public string Value { get; set; }
    public object Tags{ get; set; }
    public bool Locked { get; set; }
    public DateTime Last_Modified { get; set; }
    public Toggle Toggle { get; set; }
    public bool IsToggle => Content_Type.StartsWith(FeatureFlagContentType);
    public bool IsSalesChannelStatus => IsToggle && SalesChannelStatusId.IsSalesChannelId(Key);
    
    public const string FeatureFlagContentType = "application/vnd.microsoft.appconfig.ff+json";

    public override string ToString()
    {
        if (IsToggle)
        {
            return $"{Toggle.ToString()};Label:{Label}";
        }

        return $"'{Key}';Value:{Value};Label:{Label}";
    }

    public static string[] TableHeadings()
    {
        return ["Id", "Value", "Environment"];
    }

    public string[] TableRowValues()
    {
        if (IsToggle)
        {
            return Toggle.TableRowValues();
        }

        return [Key, Value, Label];
    }
}