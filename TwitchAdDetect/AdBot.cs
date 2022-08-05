using TwitchAdDetect.Helper;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchAdDetect;

public class AdBot
{
    private readonly TwitchClient client;
    private readonly BotSettings settings;
    private readonly TwitchApiClient twitchApiClient;
    private DateTime commandSilentUntil = DateTime.UtcNow;
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly CommercialTimers commercialTimers;

    public AdBot(BotSettings settings, TwitchApiClient twitchApiClient)
    {
        this.settings = settings;
        this.twitchApiClient = twitchApiClient;
        this.commercialTimers = new CommercialTimers(this, this.twitchApiClient.GetStreamStart(), settings);
        new PubSubClient(this, this.commercialTimers, settings);

        var credentials = new ConnectionCredentials(settings.BotUserName, this.twitchApiClient.GetOAuth());
	    var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
        var customClient = new WebSocketClient(clientOptions);
        this.client = new TwitchClient(customClient);
        this.client.Initialize(credentials, settings.ChannelName);
        this.client.OnJoinedChannel += this.Client_OnJoinedChannel;
        this.client.OnMessageReceived += this.Client_OnMessageReceived;
        this.client.OnConnected += this.Client_OnConnected;

        this.client.Connect();
        this.client.JoinChannel(settings.ChannelName);
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        this.logger.Info($"Connected to {e.AutoJoinChannel ?? "NO CHANNEL"}");
    }
    
    private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        this.logger.Info($"{e.BotUsername} joined Channel {e.Channel}");
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        if (this.commandSilentUntil >= DateTime.UtcNow) 
            return;
        
        if (!e.ChatMessage.Message.ToLower().Contains("!werbung")) 
            return;
        
        this.client.JoinChannel(this.settings.ChannelName);
        this.AskedForNextCommercial(e.ChatMessage.Username);
        this.commandSilentUntil = DateTime.UtcNow.AddSeconds(10);
    }

    public void SendMessage(string msg, string? replayToUsername = null)
    {
        this.client.JoinChannel(this.settings.ChannelName);
        this.logger.Info(msg);
        if (this.settings.SilentMode)
        {
            this.logger.Info($"Channel:{this.settings.ChannelName}, Message:{msg}");
        }
        else
        {
            if (replayToUsername != null)
            {
                this.client.SendReply(this.settings.ChannelName, replayToUsername, msg);
            }
            else
            {
                this.client.SendMessage(this.settings.ChannelName, msg);
            }
            
        }
    }

    private void AskedForNextCommercial(string username)
    {
        var nextCommercial = this.commercialTimers.NextInMinutes();
        if (nextCommercial == -1)    
        {
            this.SendMessage(this.settings.BotText.AskedForNextCommercialButUnknown, username);
        }
        else
        {
            this.SendMessage(string.Format(this.settings.BotText.AskedForNextCommercial, nextCommercial), username);    
        }
    }

    public void CommercialStarted(int commercialLength)
    {
        this.SendMessage(string.Format(this.settings.BotText.CommercialStarted, this.settings.ChannelName, commercialLength));
    }

    public void CommercialEnded()
    {
        this.SendMessage(string.Format(this.settings.BotText.CommercialEnded, this.settings.ChannelName));
    }

    public void NextCommercialIncomming(string channelName, int nexctCommercial)
    {
        this.SendMessage(string.Format(this.settings.BotText.NextCommercialIncoming, channelName, nexctCommercial));
    }

    public DateTime? GetStreamStart()
    {
        return this.twitchApiClient.GetStreamStart();
    }
}