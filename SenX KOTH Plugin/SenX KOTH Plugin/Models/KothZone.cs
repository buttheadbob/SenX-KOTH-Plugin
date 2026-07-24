using VRageMath;

namespace SenX_KOTH_Plugin.Models;

internal enum QuestDisplayMode { None, Notifications, RichHUD }

internal sealed class KothZone
{
    public string Name { get; set; } = "";

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public float Radius { get; set; } = 50f;

    public float ColorR { get; set; } = 0.529f;
    public float ColorG { get; set; } = 0.808f;
    public float ColorB { get; set; } = 0.922f;

    public string Texture { get; set; } = "SafeZone_Texture_Default";

    public bool Enabled { get; set; } = true;
    public bool PersistVisual { get; set; } = true;

    public bool ScheduleMonday { get; set; } = true;
    public bool ScheduleTuesday { get; set; } = true;
    public bool ScheduleWednesday { get; set; } = true;
    public bool ScheduleThursday { get; set; } = true;
    public bool ScheduleFriday { get; set; } = true;
    public bool ScheduleSaturday { get; set; } = true;
    public bool ScheduleSunday { get; set; } = true;
    public int ScheduleStartHour { get; set; }
    public int ScheduleStartMinute { get; set; }
    public int ScheduleEndHour { get; set; } = 23;
    public int ScheduleEndMinute { get; set; } = 59;

    public int CapturePointsNeeded { get; set; } = 100;
    public int CaptureRatePerSuit { get; set; } = 1;
    public int CaptureRatePerGrid { get; set; }
    public int CaptureDecayPerSuit { get; set; } = 3;
    public int CaptureDecayPerGrid { get; set; } = 3;
    public int CapturePointIntervalSeconds { get; set; } = 5;
    public int PointAwardIntervalSeconds { get; set; } = 5;
    public int PointsPerSuit { get; set; } = 1;
    public int PointsPerGrid { get; set; }

    public bool DynamicZone { get; set; }
    public double OriginX { get; set; }
    public double OriginY { get; set; }
    public double OriginZ { get; set; }
    public double MinDistance { get; set; } = 100;
    public double MaxDistance { get; set; } = 500;
    public int DynamicMoveIntervalSeconds { get; set; } = 60;
    public bool AnnounceGps { get; set; } = true;

    public ulong DiscordChannelId { get; set; }

    public QuestDisplayMode DisplayMode { get; set; } = QuestDisplayMode.Notifications;
    public float QuestDistance { get; set; } = 25000f;
    public bool ShowEnemiesOutside { get; set; }

    public bool EvictionEnabled { get; set; }
    public int EvictionFrequencyMinutes { get; set; } = 60;
    public int EvictionDurationSeconds { get; set; } = 30;
    public bool EvictionResetCapture { get; set; }
    public bool EvictionDuringDowntime { get; set; }
    public float EvictionColorR { get; set; } = 1f;
    public float EvictionColorG { get; set; }
    public float EvictionColorB { get; set; }
    public string EvictionTexture { get; set; } = "";

    internal Vector3D Position => new Vector3D(X, Y, Z);

    internal Vector3D OriginVector3D => new Vector3D(OriginX, OriginY, OriginZ);

    internal Vector3 ColorVector3 => new Vector3(ColorR, ColorG, ColorB);
}