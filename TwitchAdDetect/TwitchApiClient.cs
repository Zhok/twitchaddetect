using TwitchAdDetect.Helper;
using TwitchLib.Api;

namespace TwitchAdDetect;

public class TwitchApiClient
{
    private readonly BotSettings settings;
    private readonly TwitchAPI api;

    public TwitchApiClient(BotSettings settings)
    {
        this.settings = settings;
        this.api = new TwitchAPI
        {
            Settings =
            {
                ClientId = settings.ClientId,
                Secret = settings.ClientSecret
            }
        };
    }

    public DateTime? GetStreamStart()
    {
        var data = this.api.Helix.Streams.GetStreamsAsync(null, null, 1, null, null, "all", new List<string>() { this.settings.ChannelId }, null).Result;
        var stream = data.Streams.FirstOrDefault();
        return stream?.StartedAt;
    }

    public string GetOAuth()
    {
        var response = this.api.Auth.RefreshAuthTokenAsync(this.settings.BotRefreshToken, this.settings.ClientSecret, this.settings.ClientId).Result;
        return response.AccessToken;
    }
}