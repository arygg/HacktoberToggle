using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cli.Contracts;

public class KeyValueResponse
{
    public List<AppSetting> Items { get; set; } = new();
    public string NextLink { get; set; }
    public static KeyValueResponse Empty { get; set; }

    public IEnumerable<AppSetting> GetFilteredValues(AppSettingType settingType, EnvironmentFilter environment, string nameFilter = null)
    {
        IEnumerable<AppSetting> filteredAppSettings = Enumerable.Empty<AppSetting>();
        if (settingType == AppSettingType.Feature)
            filteredAppSettings = Items.Where(i => i.IsToggle);
        if (settingType == AppSettingType.Setting)
            filteredAppSettings = Items.Where(i => !i.IsToggle);
        if (settingType == AppSettingType.SalesChannelStatus)
            filteredAppSettings = Items.Where(i => i.IsSalesChannelStatus);
        if (nameFilter != null)
            filteredAppSettings = Items.Where(i => i.Key.Contains(nameFilter));

        if (environment != EnvironmentFilter.All)
            return filteredAppSettings.Where(i => i.Label == environment.ToString().ToLower());

        return filteredAppSettings;
    }

    public override string ToString()
    {
        var str = new StringBuilder();
        foreach (var item in Items)
        {
            str.Append(item.ToString() + Environment.NewLine);
        }

        return str.ToString();
    }
}

public enum EnvironmentFilter { All, ITest, STest, ATest, Prod }
public enum AppSettingType { Setting, Feature, SalesChannelStatus }