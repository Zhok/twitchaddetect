using TwitchAdDetect.Helper;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TwitchAdDetect;

public class PubSubClient
{
    private readonly TwitchPubSub client;
    private readonly AdBot bot;
    private readonly CommercialTimers commercialTimers;
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public PubSubClient(AdBot bot, CommercialTimers commercialTimers, BotSettings settings)
    {
        this.bot = bot;
        this.commercialTimers = commercialTimers;

        this.client = new TwitchPubSub();

        this.client.OnPubSubServiceConnected += this.OnPubSubServiceConnected;
        this.client.OnListenResponse += this.OnListenResponse;
        this.client.OnStreamUp += this.OnStreamUp;
        this.client.OnStreamDown += this.OnStreamDown;
        this.client.OnCommercial += this.ClientOnOnCommercial;

        this.client.ListenToVideoPlayback(settings.ChannelId);
        this.client.Connect();
    }

    private void ClientOnOnCommercial(object? sender, OnCommercialArgs e)
    {
        var serverTime = ServerTimeHelper.UnixTimeStampToDateTime(double.Parse(e.ServerTime));
        this.commercialTimers.SetLastCommercial(serverTime);
        this.commercialTimers.StartCommercialEndTimer(e.Length);
        this.bot.CommercialStarted(e.Length);
    }

    private void OnPubSubServiceConnected(object? sender, EventArgs e)
    {
        // SendTopics accepts an oauth optionally, which is necessary for some topics
        this.client.SendTopics();
    }
        
    private void OnListenResponse(object? sender, OnListenResponseArgs e)
    {
        if (!e.Successful)
            throw new Exception($"Failed to listen! Response: {e.Response}");
    }

    private void OnStreamUp(object? sender, OnStreamUpArgs e)
    {
        var serverTime = ServerTimeHelper.UnixTimeStampToDateTime(double.Parse(e.ServerTime));
        this.logger.Info($"Stream just went up! Play delay: {e.PlayDelay}, server time: {serverTime}");
        this.commercialTimers.Reset(serverTime);
    }

    private void OnStreamDown(object? sender, OnStreamDownArgs e)
    {
        this.logger.Info($"Stream just went down! Server time: {e.ServerTime}");
        this.commercialTimers.Reset(null);
    }
}