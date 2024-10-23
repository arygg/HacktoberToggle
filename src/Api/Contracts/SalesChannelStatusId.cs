using System;

namespace Cli.Contracts;

public record SalesChannelStatusId
{
    public SalesChannelStatusId(string salesProcessId, string nautilusId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(salesProcessId, nameof(salesProcessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nautilusId, nameof(nautilusId));

        SalesProcessId = salesProcessId;
        NautilusId = nautilusId;
        Id = CreateId();
        FullFeatureKey = CreateFeatureKey();
    }

    public string Id { get; }

    public string SalesProcessId { get; }

    public string NautilusId { get; }

    public string FullFeatureKey { get; }

    public static bool IsSalesChannelId(string id)
    {
        return id.Contains(Constants.SalesChannelMarker);
    }

    public static SalesChannelStatusId NautilusIdWithWildCardKey(string nautilusId)
    {
        return new SalesChannelStatusId("*", nautilusId);
    }

    public static SalesChannelStatusId Parse(string featureFlagName)
    {
        // Format to parse = SalesChannelMarker_NautilusId_SalesProcessId
        if (!featureFlagName.Contains(Constants.SalesChannelMarker))
            return null;

        var featureFlagComponents = featureFlagName.Split('_');
        if (featureFlagComponents.Length != 3)
            return null;

        return new SalesChannelStatusId(featureFlagComponents[2], featureFlagComponents[1]);
    }

   

    private string CreateFeatureKey()
    {
        return $"{Constants.FeatureMarker}{CreateId()}";
    }

    private string CreateId()
    {
        return $"{Constants.SalesChannelMarker}{NautilusId}_{SalesProcessId}";
    }
}