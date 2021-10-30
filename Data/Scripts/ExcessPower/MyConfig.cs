using System;
using System.Collections.Generic;
using ProtoBuf;
using System.Xml.Serialization;
using VRageMath;
using VRage.Game;
using System.Text;
using ExcessPower;

namespace ExcessPower
{
    [ProtoContract]
    [Serializable]
    public class MyConfig
    {
        [ProtoMember(1)]
        public string ItemId = "MyObjectBuilder_Ingot/Uranium";
        [ProtoMember(2)]
        public float ItemPerMWs = 0.00027778f;

        [ProtoIgnore]
        public MyObjectBuilder_PhysicalObject MYOB;
    }


}