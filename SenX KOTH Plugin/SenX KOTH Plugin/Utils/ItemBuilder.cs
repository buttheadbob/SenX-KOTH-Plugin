using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;

namespace SenX_KOTH_Plugin.Utils
{
    internal static class ItemBuilder
    {
        /// <summary>
        /// Creates a physical object builder from a type and subtype pair.
        /// Accepts a type with or without the "MyObjectBuilder_" prefix.
        /// Returns null for unsupported types.
        /// </summary>
        internal static MyObjectBuilder_PhysicalObject? Create(string typeId, string subtypeId)
        {
            if (string.IsNullOrWhiteSpace(typeId) || string.IsNullOrWhiteSpace(subtypeId))
                return null;

            if (typeId.StartsWith("MyObjectBuilder_", System.StringComparison.OrdinalIgnoreCase))
                typeId = typeId.Substring("MyObjectBuilder_".Length);

            switch (typeId)
            {
                case "Ore": return new MyObjectBuilder_Ore { SubtypeName = subtypeId };
                case "Ingot": return new MyObjectBuilder_Ingot { SubtypeName = subtypeId };
                case "Component": return new MyObjectBuilder_Component { SubtypeName = subtypeId };
                case "AmmoMagazine": return new MyObjectBuilder_AmmoMagazine { SubtypeName = subtypeId };
                case "PhysicalGunObject": return new MyObjectBuilder_PhysicalGunObject { SubtypeName = subtypeId };
                case "PhysicalObject": return new MyObjectBuilder_PhysicalObject { SubtypeName = subtypeId };
                case "ConsumableItem": return new MyObjectBuilder_ConsumableItem { SubtypeName = subtypeId };
                case "SeedItem": return new MyObjectBuilder_SeedItem { SubtypeName = subtypeId };
                case "Datapad": return new MyObjectBuilder_Datapad { SubtypeName = subtypeId };
                case "GasContainerObject": return new MyObjectBuilder_GasContainerObject { SubtypeName = subtypeId };
                case "OxygenContainerObject": return new MyObjectBuilder_OxygenContainerObject { SubtypeName = subtypeId };
                default: return null;
            }
        }
    }
}
