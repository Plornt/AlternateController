using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using JsonFx;
using JsonFx.Json;

namespace AlternateController
{
    public class WebSocketHandler : WebSocketService
    {
        public static WebSocketHandler me;
        protected override void OnOpen()
        {
            if (me == null) me = this;
            new WSClient(ID);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var reader = new JsonReader();
            KSPACPackets message = reader.Read<KSPACPackets>(e.Data);
            if (message != null)
            {
                switch (message.t)
                {
                    case 1:
                        ConnectionRequest connReqPacket = reader.Read<ConnectionRequest>(e.Data);

                        Send(connReqPacket.HandlePacket(WSClient.getClient(ID)));
                        break;
                    case 2:
                        ActiveVesselThrottleChange throtChg = reader.Read<ActiveVesselThrottleChange>(e.Data);
                        throtChg.HandlePacket(WSClient.getClient(ID));
                        break;
                    case 3:
                        new ActivateNextStage().HandlePacket(WSClient.getClient(ID));
                        break;
                    case 4:
                        RequestAllVesselData ravd = reader.Read<RequestAllVesselData>(e.Data);
                        Send(ravd.HandlePacket(WSClient.getClient(ID)));
                        break;
                    case 5:
                        // Tis an event
                        ChangeSubscriptionToEvent cste = reader.Read<ChangeSubscriptionToEvent>(e.Data);
                        String sendData = cste.HandlePacket(WSClient.getClient(ID));
                        if (sendData != "") Send(sendData);
                        break;
                    case 6:
                        GetCrossOriginHTML gcoh = reader.Read<GetCrossOriginHTML>(e.Data);
                        gcoh.HandlePacket(WSClient.getClient(ID));
                        break;
                    default:
                        Send(new UnrecognizedCommand(message.t).toJson());
                        break;
                }
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            KSPAlternateController.print("CLOSED CONNECTION "+ e.Code + " Reason: "+e.Data);
            WSClient.getClient(ID).isConnected = false;
            WSClient.removeClient(ID);
        }

    }
}
