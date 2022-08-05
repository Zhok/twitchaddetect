namespace TwitchAdDetect.Helper;

public class BotSettings
{
    public string ChannelId { get; set; }
    public string ChannelName { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string BotUserName { get; set; }
    public string BotRefreshToken { get; set; }
    public bool SilentMode { get; set; }
    public int CommercialIntervalInMinutes { get; set; }
    public BotText BotText { get; set; }
    public int AlertMinutesPreCommercial { get; set; }
    public int CheckStreamStatusIntervalInMinutes { get; set; }
}