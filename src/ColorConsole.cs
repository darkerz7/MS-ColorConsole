using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using System.Text.RegularExpressions;

namespace MS_ColorConsole
{
    public partial class ColorConsole : IModSharpModule, IGameListener
    {
        public string DisplayName => "ColorConsole";
        public string DisplayAuthor => "DarkerZ[RUS]";
        public ColorConsole(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        {
            _modSharp = sharedSystem.GetModSharp();
            _entities = sharedSystem.GetEntityManager();
            _events = sharedSystem.GetEventManager();
            _convars = sharedSystem.GetConVarManager();
        }

        public static IModSharp? _modSharp;
        public static IEntityManager? _entities;
        public static IEventManager? _events;
        private readonly IConVarManager _convars;

        private IConVar? g_cvar_ChatTimer;
        bool g_bChatTimer = true;
        private IConVar? g_cvar_HudTimer;
        bool g_bHudTimer = true;
        private IConVar? g_cvar_MaxHudElement;
        public static ushort g_bMaxHudElement = 3; 
        private IConVar? g_cvar_Timer_MaxTime;
        ushort g_iTimerMaxTime = 180;

        private readonly Hud g_HUD = new();

        public bool Init()
        {
            g_cvar_ChatTimer = _convars.CreateConVar("ms_colorconsole_chattimer", true, "Disabled/enabled timer in chat[0/1]", ConVarFlags.Notify);
            if (g_cvar_ChatTimer != null) _convars.InstallChangeHook(g_cvar_ChatTimer, OnCvarChatTimerChanged);
            g_cvar_HudTimer = _convars.CreateConVar("ms_colorconsole_hudtimer", true, "Disabled/enabled timer in hud[0/1]", ConVarFlags.Notify);
            if (g_cvar_HudTimer != null) _convars.InstallChangeHook(g_cvar_HudTimer, OnCvarHudTimerChanged);
            g_cvar_MaxHudElement = _convars.CreateConVar("ms_colorconsole_maxelement", (ushort)3, (ushort)1, (ushort)10, "Maximum number of elements to show in the hud[1-10]", ConVarFlags.Notify);
            if (g_cvar_MaxHudElement != null) _convars.InstallChangeHook(g_cvar_MaxHudElement, OnCvarMaxElementChanged);
            g_cvar_Timer_MaxTime = _convars.CreateConVar("ms_colorconsole_maxtime", (ushort)180, (ushort)5, (ushort)3600, "Maximum time for displaying the timer[5-3600]", ConVarFlags.Notify);
            if (g_cvar_Timer_MaxTime != null) _convars.InstallChangeHook(g_cvar_Timer_MaxTime, OnCvarMaxTimeChanged);

            _modSharp!.InstallGameListener(this);
            return true;
        }

        public void Shutdown()
        {
            _modSharp!.RemoveGameListener(this);

            if (g_cvar_ChatTimer != null) _convars.RemoveChangeHook(g_cvar_ChatTimer, OnCvarChatTimerChanged);
            if (g_cvar_HudTimer != null) _convars.RemoveChangeHook(g_cvar_HudTimer, OnCvarHudTimerChanged);
            if (g_cvar_Timer_MaxTime != null) _convars.RemoveChangeHook(g_cvar_Timer_MaxTime, OnCvarMaxTimeChanged);
        }

        private void OnCvarChatTimerChanged(IConVar conVar)
        {
            g_bChatTimer = conVar.GetBool();
        }

        private void OnCvarHudTimerChanged(IConVar conVar)
        {
            g_bHudTimer = conVar.GetBool();
        }

        private void OnCvarMaxTimeChanged(IConVar conVar)
        {
            g_iTimerMaxTime = conVar.GetUInt16();
        }

        private void OnCvarMaxElementChanged(IConVar conVar)
        {
            g_bMaxHudElement = conVar.GetUInt16();
        }

        public void OnRoundRestart()
        {
            g_HUD.RemoveAllElements();
        }

        public ECommandAction ConsoleSay(string message)
        {
            string sCleanString;
            int iTime = 0;
            string sAfterTimer = "";

            if (g_bChatTimer || g_bHudTimer)
            {
                sCleanString = CleanRegex().Replace(message, "");
                Match matchSec = SecRegex().Match(sCleanString);
                Match matchMin = MinRegex().Match(sCleanString);

                if (matchSec.Success && matchMin.Success) //Found "blabla 1 minute boom 30 seconds bam"
                {
                    if (matchMin.Index < matchSec.Index) //Not "blabla 30 seconds boom 1 minute bam"
                    {
                        sCleanString = sCleanString.Remove(matchMin.Index, matchSec.Index - matchMin.Index + matchSec.Length).Insert(matchMin.Index, $"{{0}} {matchSec.Groups[2].Value}");
                        iTime = int.Parse(matchMin.Groups[1].Value) * 60 + int.Parse(matchSec.Groups[1].Value);
                    }
                    else
                    {
                        sCleanString = sCleanString.Replace(matchSec.Value, $"{{0}} {matchSec.Groups[2].Value}");
                        iTime = int.Parse(matchSec.Groups[1].Value);
                    }
                }
                else if (matchSec.Success) // Only seconds: 20 seconds left
                {
                    sCleanString = sCleanString.Replace(matchSec.Value, $"{{0}} {matchSec.Groups[2].Value}");
                    iTime = int.Parse(matchSec.Groups[1].Value);
                }
                else if (matchMin.Success) // Only minute: 2 minutes left
                {
                    sCleanString = sCleanString.Replace(matchMin.Value, $"{{0}} seconds");
                    iTime = int.Parse(matchMin.Groups[1].Value) * 60;
                }
                //Console.WriteLine($"[Debug] CleanString: {sCleanString}", iTime - 1);

                if (iTime > 4 && iTime <= g_iTimerMaxTime)
                {
                    if (g_bChatTimer)
                    {
                        float fAfterTimer = _modSharp!.GetGameRules().GetRoundRemainingTime() - iTime;
                        if (fAfterTimer > 0.0f)
                        {
                            sAfterTimer = $" @{(int)(fAfterTimer / 60)}:{(int)(fAfterTimer % 60):D2}";
                        }
                    }
                    if (g_bHudTimer) g_HUD.AddElementToList(sCleanString, iTime);
                }
            }

            Console.ForegroundColor = (ConsoleColor)15;
            Console.Write("[Console]: ");
            Console.ForegroundColor = (ConsoleColor)1;
            Console.Write(message);
            if (!string.IsNullOrEmpty(sAfterTimer))Console.ForegroundColor = (ConsoleColor)3;
            Console.WriteLine(sAfterTimer);
            Console.ResetColor();

            foreach(var player in _entities!.GetPlayerControllers(true))
            {
                player.Print(HudPrintChannel.Chat, $" {ChatColor.DarkRed}[Console]: {ChatColor.White}{message}{(string.IsNullOrEmpty(sAfterTimer) ? "" : $"{ChatColor.Gold}{sAfterTimer}")}");
            }

            return ECommandAction.Stopped;
        }

        int IGameListener.ListenerVersion => IGameListener.ApiVersion;
        int IGameListener.ListenerPriority => 0;

        [GeneratedRegex(@"[^\w\s():.\[\]]+")]
        private static partial Regex CleanRegex();
        
        [GeneratedRegex(@"(\d{1,})\s(sec\w*)", RegexOptions.IgnoreCase)]
        private static partial Regex SecRegex();

        [GeneratedRegex(@"(\d{1,})\smin\w*", RegexOptions.IgnoreCase)]
        private static partial Regex MinRegex();
    }
}
