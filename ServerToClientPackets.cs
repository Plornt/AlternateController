using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonFx;
using JsonFx.Json;

namespace AlternateController
{
    /* 
     * SERVER PACKETS ARE SPLIT BETWEEN THIS AND EVENTS.CS WHERE AVAILABLE TO KEEP THIS FILE
     * EASIER TO MANAGE, PLEASE KEEP THEM IN ORDER AND IF A PACKET IS NOT IN THIS FILE
     * DISPLAY THE TYPE NUMBER AND WHERE EXACTLY IT IS FOUND.
     */
    public class UnrecognizedCommand : KSPACPackets
    {
        public int command;
        public UnrecognizedCommand(int s)
        {
            t = 1;
            command = s;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }
    public class ConnectionAccepted : KSPACPackets
    {
        public string ID;
        public ConnectionAccepted(string ID)
        {
            this.ID = ID;
            t = 2;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }
    public class IncorrectPassword : KSPACPackets
    {
        public string ID;
        public IncorrectPassword(string ID)
        {
            this.ID = ID;
            t = 3;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }

    // t = 4 = DELETED. REUSE TYPE WHERE POSSIBLE

   
    // t = 5 = DELETED. REUSE TYPE WHERE POSSIBLE

    public class VesselDataResponse : KSPACPackets
    {
        public string ID;
        public List<VesselData> Vessels;
        public VesselDataResponse(string ID, List<VesselData> dict)
        {
            t = 6;
            this.ID = ID;
            this.Vessels = dict;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            /*
            String easyWayOut = "{ \"t\": " + t + ", \"ID\": \"" + ID + "\", \"Vessels\": [";
            bool i = false;
            foreach (VesselData vess in dict)
            {
                easyWayOut += (i ? "," : "") + writer.Write(vess);
                i = true;
            }
            return easyWayOut + "] }";
             */
            return writer.Write(this);
        }
    }
    public class DataNotAvailable : KSPACPackets
    {
        public string ID;
        public DataNotAvailable(string ID)
        {
            this.ID = ID;
            t = 7;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }

    // EVENT PACKETS ALL HAVE TYPE = 8. PLEASE VIEW Events.cs TO VIEW THESE

    public class ResponseToHttpRequest : KSPACPackets
    {
        public string body;
        public bool base64;
        public string ID;
        public string mimetype;
        public ResponseToHttpRequest(string ID, bool base64, string mimetype, string body)
        {
            t = 9;
            this.base64 = base64;
            this.mimetype = mimetype;
            this.ID = ID;
            this.body = body;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }
}
