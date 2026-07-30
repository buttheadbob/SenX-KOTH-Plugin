using System;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace SenX_KOTH_Plugin.Mod
{
    internal static class ParticleBurst
    {
        private const string EffectNameWin = "KoTH_Fireworks_Win";
        private const string EffectNameLose = "KoTH_Fireworks_Lose";
        private const int BurstCount = 8;
        private static bool _loggedFailure;

        public static void Spawn(Vector3D origin, float zoneRadius, bool bright)
        {
            float scale = Math.Max(zoneRadius * 0.06f, 1f);
            string effectName = bright ? EffectNameWin : EffectNameLose;
            var pos = origin;
            bool anySpawned = false;

            for (int i = 0; i < BurstCount; i++)
            {
                double angle = (Math.PI * 2.0 * i) / BurstCount;
                var dir = new Vector3D(Math.Cos(angle), 0.3, Math.Sin(angle));
                var matrix = MatrixD.CreateWorld(origin, dir, Vector3D.Up);

                MyParticleEffect effect;
                if (MyParticlesManager.TryCreateParticleEffect(
                    effectName, ref matrix, ref pos, uint.MaxValue, out effect))
                {
                    effect.UserScale = scale;
                    effect.Autodelete = true;
                    anySpawned = true;
                }
            }

            if (anySpawned)
                MyLog.Default.WriteLineAndConsole("KoTH Mod: Fireworks spawned (bright=" + bright + ", scale=" + scale + ")");
            else if (!_loggedFailure)
            {
                MyLog.Default.WriteLineAndConsole("KoTH Mod: Particle effect may not exist");
                _loggedFailure = true;
            }
        }
    }
}
