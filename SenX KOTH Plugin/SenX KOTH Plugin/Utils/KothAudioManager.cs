

using Sandbox.Game;
using VRage.Audio;
using VRageMath;

namespace SenX_KOTH_Plugin.Utils;

public class KothAudioManager
{
    public void Play2DSound(long targetIdentityId, SoundCueType cue)
    {
        switch (cue)
        {
            case SoundCueType.PointEarned:
                // Crisp objective chime
                MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.HudObjectiveComplete, targetIdentityId);
                break;

            case SoundCueType.EnemyEnteredZone:
                // Rejection/warning buzz
                MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.HudUnable, targetIdentityId);
                break;

            case SoundCueType.ZoneLocking:
                // Progress tick while capturing
                MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.HudLockingProgress, targetIdentityId);
                break;

            case SoundCueType.ZoneLost:
                // Lock lost / error cue
                MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.HudLockingLost, targetIdentityId);
                break;

            case SoundCueType.MatchWon:
                MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.MatchVictory, targetIdentityId);
                break;

            case SoundCueType.MatchLost:
                MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.MatchDefeat, targetIdentityId);
                break;
        }
    }

    /// <summary>
    /// Emits a 3D sound in world space at the capture point coordinate.
    /// </summary>
    public void Play3DZoneAlarm(Vector3D zonePosition, bool isContested)
    {
        // Plays alarm siren at the physical hill position for all nearby players
        MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(isContested 
            ? "ArcSoundBlockAlarm2" 
            : "ArcHudObjectiveComplete"
            , zonePosition);
    }
    
    /// <summary>
    /// True 2D GUI Audio (Restricted strictly to MyGuiSounds enum).
    /// </summary>
    public void Play2DHudSound(long targetIdentityId, MyGuiSounds sound)
    {
        MyVisualScriptLogicProvider.PlayHudSound(sound, targetIdentityId);
    }
}

public enum SoundCueType
{
    PointEarned,
    EnemyEnteredZone,
    ZoneLocking,
    ZoneLost,
    MatchWon,
    MatchLost
}
