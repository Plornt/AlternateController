using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonFx;
using JsonFx.Json;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.IO;

namespace AlternateController
{


    public class EventHandler
    {
        KSPAlternateController parent;
        public EventHandler(KSPAlternateController parent)
        {
            this.parent = parent;
        }
    
        private float prevThrot = -1;
        private GameScenes prevScene;
        private Dictionary<String, List<double>> prevVessAltVel = new Dictionary<String, List<double>>();
        private int prevPartCount;
        private String prevFlag = "";
        public void checkEvents(int frames)
        {
            WebSocketServer wsServer = parent.ws;
            WebSocketHandler ws = WebSocketHandler.me;

            if (ws == null || wsServer == null || !wsServer.IsListening) return;

            if (HighLogic.LoadedScene != prevScene)
            {
                KSPAlternateController.print(System.Enum.GetName(typeof(GameScenes), HighLogic.LoadedScene));
                EventSubscriptions.dispatchEvent(ws, KSPEvents.KSP_SCENE_CHANGE, new KSPSceneChangeEvent(System.Enum.GetName(typeof(GameScenes), HighLogic.LoadedScene)).toJson());
                prevScene = HighLogic.LoadedScene;
            }

            if (HighLogic.LoadedScene == GameScenes.EDITOR && EditorLogic.fetch != null)
            {
                if (EditorLogic.FlagURL != null && EditorLogic.FlagURL != "" && prevFlag != EditorLogic.FlagURL)
                {
                    prevFlag = EditorLogic.FlagURL;
                    try
                    {
                        using (FileStream sr = new FileStream("GameData/" + EditorLogic.FlagURL + ".png", FileMode.Open, FileAccess.Read))
                        {
                            String base64Flag = System.Convert.ToBase64String(sr.ReadBytes(sr.Length));
                            EventSubscriptions.dispatchEvent(ws, KSPEvents.EDITOR_FLAG_CHANGE, new EditorFlagChange(EditorLogic.FlagURL, base64Flag).toJson());
                        }
                    }
                    catch (Exception e)
                    {
                        KSPAlternateController.print(e.Message);
                    }
                }
                if ((frames % 100) == 0)
                {
                    if (EditorLogic.startPod != null)
                    {
                        List<Part> shipParts = EditorLogic.fetch.getSortedShipList();
                        if (shipParts != null)
                        {
                            if (shipParts.Count != prevPartCount)
                            {
                                EventSubscriptions.dispatchEvent(ws, KSPEvents.EDITOR_PART_COUNT_CHANGE, new EditorPartCountChange(shipParts.Count).toJson());
                                prevPartCount = shipParts.Count;
                            }
                        }
                        else
                        {
                            if (prevPartCount != 0)
                            {
                                EventSubscriptions.dispatchEvent(ws, KSPEvents.EDITOR_PART_COUNT_CHANGE, new EditorPartCountChange(0).toJson());
                                prevPartCount = 0;
                            }
                        }
                    }
                }
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (FlightGlobals.ActiveVessel != null)
                {
                    ActiveVesselAltVelEvent vs = new ActiveVesselAltVelEvent(System.Enum.GetName(typeof(FlightUIController.SpeedDisplayModes), FlightUIController.speedDisplayMode), FlightGlobals.ship_obtVelocity.magnitude,
                                                                             FlightGlobals.ship_srfVelocity.magnitude, FlightGlobals.ship_tgtVelocity.magnitude, FlightGlobals.ship_altitude);
                    EventSubscriptions.dispatchEvent(ws, KSPEvents.ACTIVE_VESSEL_VELALT_CHANGE, vs.toJson());


                    if (prevThrot != FlightGlobals.ActiveVessel.ctrlState.mainThrottle)
                    {
                        prevThrot = FlightGlobals.ActiveVessel.ctrlState.mainThrottle;
                        ActiveVesselThrottleChange tcu = new ActiveVesselThrottleChange(FlightGlobals.ActiveVessel.ctrlState.mainThrottle);
                        EventSubscriptions.dispatchEvent(ws, KSPEvents.ACTIVE_VESSEL_THROTTLE_CHANGE, tcu.toJson());
                    }
                }
                if (FlightGlobals.ready)
                {
                    foreach (Vessel v in FlightGlobals.Vessels)
                    {
                        bool sendUpdate = false;
                        String ID = v.id.ToString();
                        List<double> altVel;
                        if (!prevVessAltVel.ContainsKey(ID))
                        {
                            sendUpdate = true;
                            altVel = new List<double> { v.altitude, v.obt_velocity.magnitude, v.srf_velocity.magnitude };
                            // Altitude, orbitVelocity, surfaceVelocity
                            prevVessAltVel.Add(ID, altVel);
                        }
                        else
                        {
                            altVel = prevVessAltVel[ID];
                            if (altVel.Count >= 3)
                            {
                                if (altVel[0] != v.altitude && altVel[1] != v.obt_velocity.magnitude && altVel[2] != v.srf_velocity.magnitude)
                                {
                                    sendUpdate = true;
                                    prevVessAltVel[ID][0] = v.altitude;
                                    prevVessAltVel[ID][1] = v.obt_velocity.magnitude;
                                    prevVessAltVel[ID][2] = v.srf_velocity.magnitude;
                                    altVel = prevVessAltVel[ID];
                                }
                            }
                        }
                        if (sendUpdate)
                        {
                            VesselAltVelChangeEvent velaltchange = new VesselAltVelChangeEvent(altVel[0], altVel[1], altVel[2], ID);
                            SubEventSubscriptions.dispatchEvent(ws, KSPEvents.VESSEL_VELALT_CHANGE, velaltchange.toJson(), ID);
                        }
                    }
                }
            }
			
        }


        public void onFlagChange(String flagURL)
        {
            // DOESNT DO WHAT I WANT IT TO. NO BLOODY POINT.
        }
        
    
        public void onPartAttach(GameEvents.HostTargetAction<Part,Part> parts)
        {
            // Just double checking, I dont want to have some mod fire this event and causing an issue outside of the editor.
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                // Doesnt do what I want it to but I will use this for something else later.
            }
        }
    }

    public class EventSubscriptions
    {
        private static Dictionary<KSPEvents, String> _cachedEvents = new Dictionary<KSPEvents, String>();
        private static Dictionary<KSPEvents, List<String>> subscribedEvents = new Dictionary<KSPEvents, List<String>>();
        public static void unsubscribeClient(KSPEvents ev, String wsClient)
        {
            if (subscribedEvents.ContainsKey(ev)) {
                subscribedEvents[ev].Remove(wsClient);
            }
        }

        public static void subscribeClient(KSPEvents ev, String wsClient)
        {
            if (!subscribedEvents.ContainsKey(ev))
            {
                subscribedEvents[ev] = new List<String>();
            }
            subscribedEvents[ev].Add(wsClient);
        }
        public static List<WSClient> getSubscribers(KSPEvents ev)
        {
            List<WSClient> clients = new List<WSClient>();
            if (subscribedEvents.ContainsKey(ev))
            {
                List<String> subscribers = subscribedEvents[ev];
                for (int i = 0; i < subscribers.Count; i++)
                {
                    WSClient client = WSClient.getClient(subscribers[i]);
                    if (client != null) { clients.Add(client); }
                    else
                    {
                        subscribedEvents[ev].Remove(subscribers[i]);
                    }
                }
            }

            return clients;
        }
        public static String getLastEvent(KSPEvents ev)
        {
            KSPAlternateController.print("Test3");
            if (_cachedEvents.ContainsKey(ev))
            {
                KSPAlternateController.print("Test4");
                return _cachedEvents[ev];
            }
            return "";
        }
        public static void dispatchEvent (WebSocketHandler ws, KSPEvents ev, String eventData) {
            if (!_cachedEvents.ContainsKey(ev)) _cachedEvents.Add(ev, eventData);
            else _cachedEvents[ev] = eventData;

            getSubscribers(ev).ForEach((WSClient client) => {
                ws.SendTo(client.ID, eventData);
            });
        }

    }
    class SubEventSubscriptions : EventSubscriptions
    {
        private static Dictionary<KSPEvents, Dictionary<String, String>> _cachedEvents = new Dictionary<KSPEvents, Dictionary<String, String>>();
        private static Dictionary<KSPEvents, Dictionary<String, List<String>>> subscribedSubEvents = new Dictionary<KSPEvents, Dictionary<String, List<String>>>();
        public static void subscribeClient(KSPEvents ev, String client, String subEventID)
        {

            if (!subscribedSubEvents.ContainsKey(ev))
            {
                subscribedSubEvents.Add(ev, new Dictionary<String, List<String>>());
            }
            if (!subscribedSubEvents[ev].ContainsKey(client))
            {
                subscribedSubEvents[ev].Add(client, new List<String>());
            }
            subscribedSubEvents[ev][client].Add(subEventID);
            subscribeClient(ev, client);
        }

        public static void unsubscribeClient(KSPEvents ev, String wsClient, String subEventID)
        {
            if (subscribedSubEvents.ContainsKey(ev))
            {
                if (subscribedSubEvents[ev].ContainsKey(wsClient))
                {
                    if (subscribedSubEvents[ev][wsClient].Contains(subEventID))
                    {
                        subscribedSubEvents[ev][wsClient].Remove(subEventID);
                    }    

                    if (subscribedSubEvents[ev][wsClient].Count == 0)
                    {
                        unsubscribeClient(ev, wsClient);
                    }
                }
            }
        }
        public static String getLastEvent(KSPEvents ev, String subID)
        {
            KSPAlternateController.print("Test2");
            if (_cachedEvents.ContainsKey(ev))
            {
                if (_cachedEvents[ev].ContainsKey(subID))
                {
                    KSPAlternateController.print("Test");
                    return _cachedEvents[ev][subID];
                }
            }
            return "";
        }
        public static void dispatchEvent(WebSocketHandler ws, KSPEvents ev, String eventData, String subEventID)
        {
            if (!_cachedEvents.ContainsKey(ev)) _cachedEvents.Add(ev, new Dictionary<String, String>());

            if (!_cachedEvents[ev].ContainsKey(subEventID)) _cachedEvents[ev].Add(subEventID, eventData);
            else _cachedEvents[ev][subEventID] = eventData;

            getSubscribers(ev).ForEach((WSClient client) =>
            {
                if (subscribedSubEvents[ev].ContainsKey(client.ID))
                {
                    if (subscribedSubEvents[ev][client.ID].Contains(subEventID))
                    {
                        ws.SendTo(client.ID, eventData);
                    }
                }
            });
        }
    }
    
    /* EVENT PACKETS */
    public abstract class EventPacket : KSPACPackets
    {
        public abstract int eID { get; }
    }

    public class ActiveVesselThrottleChange : EventPacket
    {
        public override int eID { get { return (int)KSPEvents.ACTIVE_VESSEL_THROTTLE_CHANGE; } }
        public double s;
        public ActiveVesselThrottleChange(double s)
        {
            this.s = s;
            t = 8;
        }
        public override String HandlePacket(WSClient client)
        {
            if (client.hasRegistered)
            {
                FlightInputHandler.state.fastThrottle = (float)s;
                FlightInputHandler.state.mainThrottle = (float)s;
                FlightGlobals.ActiveVessel.ctrlState.mainThrottle = (float)s;
            }
            return "";
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }

    }

    public class ActiveVesselAltVelEvent : EventPacket
    {
        public override int eID { get { return (int)KSPEvents.ACTIVE_VESSEL_VELALT_CHANGE; } }
        public String displayMode;
        public double orbitVel;
        public double surfaceVel;
        public double targetVel;
        public double altitude;
        public ActiveVesselAltVelEvent(String displayMode, double orbitVel, double surfaceVel, double targetVel, double altitude)
        {
            t = 8;
            this.altitude = altitude;
            this.displayMode = displayMode;
            this.orbitVel = orbitVel;
            this.surfaceVel = surfaceVel;
            this.targetVel = targetVel;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }
    public class VesselAltVelChangeEvent : EventPacket
    {
        public override int eID { get { return (int)KSPEvents.VESSEL_VELALT_CHANGE; } }
        public double alt;
        public double orbitVelocity;
        public double surfaceVelocity;
        public String subID;
        public VesselAltVelChangeEvent(double alt, double orbitVelocity, double surfaceVelocity, String vID)
        {
            t = 8;
            this.orbitVelocity = orbitVelocity;
            this.surfaceVelocity = surfaceVelocity;
            this.alt = alt;
            this.subID = vID;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }
    public class KSPSceneChangeEvent : EventPacket
    {
        public override int eID { get { return (int)KSPEvents.KSP_SCENE_CHANGE; } }
        public String sceneName;
       
        public KSPSceneChangeEvent(String sceneName)
        {
            this.sceneName = sceneName;
            t = 8;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }

    public class EditorFlagChange : EventPacket
    {
        public override int eID { get { return (int)KSPEvents.EDITOR_FLAG_CHANGE; } }
        public String flagURL;
        public String base64Image;

        public EditorFlagChange (String flagURL, String base64Image)
        {

            this.flagURL = flagURL;
            this.base64Image = base64Image;
            t = 8;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }
    public class EditorPartCountChange : EventPacket
    {
        public override int eID { get { return (int)KSPEvents.EDITOR_PART_COUNT_CHANGE; } }
        public int partCount;
        public EditorPartCountChange(int count)
        {
            this.partCount = count;
            t = 8;
        }
        public override String toJson()
        {
            var writer = new JsonWriter();
            return writer.Write(this);
        }
    }

    public enum KSPEvents
    {
        ACTIVE_VESSEL_THROTTLE_CHANGE = 0,
        ACTIVE_VESSEL_VELALT_CHANGE = 1,
        VESSEL_VELALT_CHANGE = 2,
        KSP_SCENE_CHANGE = 3,
        EDITOR_FLAG_CHANGE = 4,
        EDITOR_PART_COUNT_CHANGE = 5
    }
}
