using System.Timers;
using TwitchAdDetect.Helper;
using Timer = System.Timers.Timer;

namespace TwitchAdDetect;

public class CommercialTimers
{
    private readonly AdBot bot;
    private DateTime? streamStartDate;
    private int lastCommercial;
    private int calledNextCommercial;
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly BotSettings settings;
    private DateTime? lastOnlineCheck;

    public CommercialTimers(AdBot bot, DateTime? streamStartDate, BotSettings settings)
    {
        this.settings = settings;
        this.bot = bot;
        if (streamStartDate.HasValue)
        {
            this.streamStartDate = streamStartDate.Value;
        }
        var callTimer = new Timer(20000);
        callTimer.Elapsed += this.TimerOnElapsed;
        callTimer.Start();
        
        if (streamStartDate.HasValue)
        {
            this.Reset(streamStartDate.Value);
        }
    }

    public void Reset(DateTime? streamStart)
    {
        this.streamStartDate = streamStart;
        this.lastCommercial = 0;
        this.logger.Info($"Reset calltimer with streamStart {this.streamStartDate}");
    }

    public void SetLastCommercial(DateTime now)
    {
        if (this.streamStartDate.HasValue)
        {
            this.lastCommercial = (int)(now - this.streamStartDate.Value).TotalMinutes;
        }
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (this.lastCommercial > 0 && this.streamStartDate.HasValue)
        {
            this.logger.Info("Check for next commercial..");
            var runTimeMinutes = (DateTime.UtcNow - this.streamStartDate.Value).TotalMinutes;
            var nextCommercial = this.lastCommercial + this.settings.CommercialIntervalInMinutes;
        
            if (runTimeMinutes + this.settings.AlertMinutesPreCommercial > nextCommercial && nextCommercial != this.calledNextCommercial)
            {
                this.bot.NextCommercialIncomming(this.settings.ChannelName, nextCommercial - (int)runTimeMinutes);
                this.calledNextCommercial = nextCommercial;
            }
        }
        this.CheckStreamStatus(this.settings.CheckStreamStatusIntervalInMinutes);
    }

    private void CheckStreamStatus(int minutesBetweenChecks)
    {
        if (this.lastOnlineCheck == null || this.lastOnlineCheck.Value.AddHours(minutesBetweenChecks) < DateTime.UtcNow)
        {
            var streamStart = this.bot.GetStreamStart();
            if (streamStart == null)
            {
                this.Reset(streamStart);
            }

            if (streamStart != null && this.streamStartDate == null)
            {
                this.Reset(streamStart.Value);
            }
            
            this.lastOnlineCheck = DateTime.UtcNow;
        }
    }

    public int NextInMinutes()
    {
        if (this.lastCommercial == 0 || this.streamStartDate == null)
        {
            return -1;
        }

        var runTimeMinutes = (DateTime.UtcNow - this.streamStartDate.Value).TotalMinutes;
        var nextCommercial = this.lastCommercial + this.settings.CommercialIntervalInMinutes;
        return nextCommercial - (int)runTimeMinutes;
    }

    public async Task StartCommercialEndTimer(int commercialLengthInSecounds)
    {
        await Task.Delay(commercialLengthInSecounds * 1000);
        this.bot.CommercialEnded();
    }
}