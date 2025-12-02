using Sharp.Shared.Enums;

namespace MS_ColorConsole
{
    public class Hud
    {
        Guid? Timer = null;
        readonly LinkedList<HudElement> HudList = new();

        public void AddElementToList(string sCleanString, int iTime)
        {
            double fEndTime = ColorConsole._modSharp!.EngineTime() + iTime;
            foreach (var element in HudList.ToArray())
            {
                if (element.EndTime >= fEndTime && element.EndTime <= fEndTime + 1.0f)
                {
                    element.CleanString = sCleanString;
                    return;
                }
            }
            HudList.AddFirst(new HudElement(sCleanString, fEndTime));
            CreateTimer();
        }

        public void RemoveAllElements()
        {
            KillTimer();
            HudList.Clear();
        }

        void Show()
        {
            var DublicateList = HudList.ToList();
            string sMessage = "";
            double fCurrentTime = ColorConsole._modSharp!.EngineTime();
            RemoveElementsFromList(fCurrentTime);
            bool bFirst = true;
            for (int i = 0; i < DublicateList.Count && i < ColorConsole.g_bMaxHudElement; i++)
            {
                sMessage += string.Format($"{(bFirst ? "": "<br>")}<font color='aqua'>{DublicateList[i].CleanString}</font>", $"<font color='red'>{(int)(DublicateList[i].EndTime - fCurrentTime)}</font>");
                bFirst = false;
            }

            if (!string.IsNullOrEmpty(sMessage))
            {
                PrintToCenterHtml(sMessage);
            }
            else KillTimer();
        }

        void PrintToCenterHtml(string message)
        {
            //FlashingHtmlHudFix
            ColorConsole._modSharp!.GetGameRules().IsGameRestart = ColorConsole._modSharp!.GetGameRules().RestartRoundTime < ColorConsole._modSharp!.EngineTime();
            if (ColorConsole._events!.CreateEvent("show_survival_respawn_status", true) is { } Event)
            {
                Event.SetString("loc_token", message);
                Event.SetInt("duration", 1);
                Event.FireToClients();
                Event.Dispose();
            }
        }

        void RemoveElementsFromList(double fCurrentTime)
        {
            foreach (var element in HudList.ToArray())
            {
                if (element.EndTime <= fCurrentTime + 1) HudList.Remove(element);
            }
        }

        void CreateTimer()
        {
            KillTimer();
            Timer = ColorConsole._modSharp!.PushTimer(() => Show(), 0.5f, GameTimerFlags.Repeatable);
        }

        void KillTimer()
        {
            if (Timer != null)
            {
                ColorConsole._modSharp!.StopTimer((Guid)Timer);
                Timer = null;
            }
        }
    }

    public class HudElement(string sCleanString, double fEndTime)
    {
        public string CleanString = sCleanString;
        public double EndTime = fEndTime;
    }
}
