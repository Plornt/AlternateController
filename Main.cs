using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using UnityEngine;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;
using JsonFx;
using JsonFx.Json;
using System.Threading;
using System.Net;
using System.Timers;

namespace AlternateController
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KSPAlternateController : MonoBehaviour
    {
		public static String password = "tempPass";
		bool Connected = false;
        private static int windowHeight = 100;
        private static int windowWidth = 100;
        private Rect windowLocation = new Rect(Screen.width - windowWidth, (Screen.height / 2) - (windowHeight / 2), windowWidth, windowHeight);
		public WebSocketServer ws;
        public EventHandler eventHandler;

    
        public static void addToList(String ID, WSClient client, String url)
        {
            HttpRequestThreadPool newObj = new HttpRequestThreadPool(ID, client, url);
            ThreadPool.QueueUserWorkItem(newObj.processReponse);
        }
        
		private void OnGUI () {

            GUI.skin = HighLogic.Skin;
            windowLocation = GUILayout.Window(1, windowLocation, renderWindow, "Connector", GUILayout.ExpandWidth(true));
		}
		
        public void renderWindow(int windowID)
        {
        	if (Connected) {
				
			}
            GUI.skin = HighLogic.Skin;
            GUILayout.BeginVertical();
            GUILayout.Label("Password:");
            password = GUILayout.TextField(password);
            GUILayout.EndVertical();
			
            GUI.DragWindow();
		}
        private int frames = 0;
        private void FixedUpdate() {
            
            frames++;
            //Events that dont have handlers 
            if (eventHandler == null) eventHandler.checkEvents(frames);

        }
        private void Start()
        {
			print ("Started Server");
			try {
				ws = new WebSocketServer(81);
                ws.AddWebSocketService<WebSocketHandler>("/");
                ws.Start();

                if (eventHandler == null) eventHandler = new EventHandler(this);
            }
			catch (TypeInitializationException exception) {
				print (exception.Message);
			}
			catch {
			    print ("Unhandled");	
			}


            // Neither of these event handlers actually do what I want them to.
            // I mean, you would think they would atleast start with the editor. But nope! 
            // GameEvents.onMissionFlagSelect.Add(new EventData<string>.OnEvent(eventHandler.onFlagChange));
            GameEvents.onPartAttach.Add(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(eventHandler.onPartAttach));
            GameEvents.onPartRemove.Add(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(eventHandler.onPartDetach));
        }
	        private void Awake () {      DontDestroyOnLoad(this.gameObject);       }
		private void OnDestroy () {
            ws.Stop();
		}
    }
   

	public class WSClient {
		public static Dictionary<String, WSClient> sessions = new Dictionary<String, WSClient>();
        public String ID;
		public bool hasRegistered = false;
        public bool isConnected = true;
       
        public WSClient(String sess)
        { 
			this.ID = sess;
			sessions.Add (sess, this);
		}
		
		public static WSClient getClient (String ID) {
			WSClient currClient;
			sessions.TryGetValue(ID, out currClient);
			return currClient;
		}
        public static void removeClient(String ID)
        {
            sessions.Remove(ID);
        }
	}
}












































