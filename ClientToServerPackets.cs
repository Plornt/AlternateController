using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonFx;
using JsonFx.Json;
using System.Net;
using System.IO;

namespace AlternateController
{
    public class KSPACPackets
    {
        public int t { get; set; }

        public virtual String HandlePacket(WSClient client) { return ""; }

        public virtual String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }

    }

    public class ConnectionRequest : KSPACPackets
    {
        // t = 1
        public string password;
        public string ID;
        public override String HandlePacket(WSClient client)
        {
            if (this.password != null)
            {
                if (this.password == KSPAlternateController.password)
                {
                    client.hasRegistered = true;
                    return new ConnectionAccepted(ID).toJson();
                }
            }
            return new IncorrectPassword(ID).toJson();
        }
    }
    public class ActivateNextStage : KSPACPackets
    {
        // t = 2
        public override String HandlePacket(WSClient client)
        {
            if (client.hasRegistered)
            {
                if (FlightGlobals.ActiveVessel != null) Staging.ActivateNextStage();
            }
            return "";
        }
    }

    // See ServerToClientPackets.cs t = 3 for ThrottleChangeUpdate


    public struct VesselData
    {
        public int t;
        public String ID;
        public String name;
        public double missionTime;
        public double launchTime;
        public String type;
        public bool landedOrSplashed;
    }
    public class RequestAllVesselData : KSPACPackets
    {
        // t = 4
        public String ID;

        public override String HandlePacket(WSClient client)
        {
            if (client.hasRegistered)
            {
                if (FlightGlobals.ready)
                {
                    List<VesselData> vesselData = new List<VesselData>();
                    foreach (Vessel v in FlightGlobals.Vessels)
                    {
                        VesselData vd = new VesselData();
                        vd.name = v.vesselName;
                        vd.ID = v.id.ToString();
                        vd.missionTime = v.missionTime;
                        vd.launchTime = v.launchTime;
                        vd.type = v.vesselType.ToString();
                        vd.landedOrSplashed = v.LandedOrSplashed;
                        vesselData.Add(vd);
                    }
                    KSPAlternateController.print(ID);

                    return new VesselDataResponse(ID, vesselData).toJson();
                }
                else return new DataNotAvailable(ID).toJson();
            }
            return "";
        }
    }
    public class ChangeSubscriptionToEvent : KSPACPackets
    {
        // t = 5
        public int eNum;
        public bool sendEvents;
        public String subID;
        public bool cached = false;

        public override String HandlePacket(WSClient client)
        {
            if (client.hasRegistered)
            {
                if (this.subID == "" || this.subID == null)
                {
                    if (this.sendEvents)
                    {
                        EventSubscriptions.subscribeClient((KSPEvents)this.eNum, client.ID);
                        if (this.cached)
                        {
                            String cachedEvent = EventSubscriptions.getLastEvent((KSPEvents)this.eNum);
                            if (cachedEvent != "") return cachedEvent;
                        }
                    }
                    else EventSubscriptions.unsubscribeClient((KSPEvents)this.eNum, client.ID);
                }
                else
                {
                    if (this.sendEvents)
                    {
                        SubEventSubscriptions.subscribeClient((KSPEvents)this.eNum, client.ID, this.subID);
                        if (this.cached)
                        {
                            String cachedEvent = SubEventSubscriptions.getLastEvent((KSPEvents)this.eNum, this.subID);
                            if (cachedEvent != "") return cachedEvent;
                        }
                    }
                    else SubEventSubscriptions.unsubscribeClient((KSPEvents)this.eNum, client.ID, this.subID);
                }
            }
            return "";
        }
    }
    public class ImageChecker
    {
        public static List<String> types = new List<String> { "image/jpg", "image/jpeg", "image/pjpeg", "image/gif", "image/x-png", "image/png" }; 
        public static bool shouldBase64(String contentType)
        {
            return types.Contains(contentType);
        }
    }
    public class GetCrossOriginHTML : KSPACPackets
    {
        public String url;
        public String ID;
        // t = 6
        public override String HandlePacket(WSClient client)
        {
            if (client.hasRegistered)
            {
                KSPAlternateController.addToList(ID, client, url);
            }
            return "";
        }


    }

    /*
     * 
     * 
     *
    public class PlaceholderPacket : KSPACPackets
    {
        // t = 2
        public override String PlaceholderPacket(WSClient client)
        {
            if (client.hasRegistered)
           {
     
            }
            return "";
        }
    }

     * 
     */
}
