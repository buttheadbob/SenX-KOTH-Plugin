using System.Collections.Generic;
using ProtoBuf;

namespace SenX_KOTH_Plugin.Messages
{
    [ProtoContract]
    public class QuestUpdateMessage
    {
        [ProtoMember(1)] public string ZoneName;
        [ProtoMember(2)] public List<string> Lines;
        [ProtoMember(3)] public double ZoneX;
        [ProtoMember(4)] public double ZoneY;
        [ProtoMember(5)] public double ZoneZ;
        [ProtoMember(6)] public float QuestDistance;
        [ProtoMember(7)] public bool Clear;
        [ProtoMember(8)] public long Timestamp;
    }
}
