using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;

namespace AlternateController
{

    public class HttpRequestThreadPool
    {
        private String ID;
        private WSClient client;
        String URL;
        public HttpWebRequest req;
        public HttpRequestThreadPool(String ID, WSClient client, String URL)
        {
            this.ID = ID;
            this.client = client;
            this.URL = URL;
        }
        public void requestTimeOut(object source, ElapsedEventArgs e)
        {
            this.req.Abort();
        }
        public void processReponse(System.Object threadContext)
        {
            System.Timers.Timer aTimer = new System.Timers.Timer();
            this.context = threadContext;
            aTimer.Elapsed += new ElapsedEventHandler(this.requestTimeOut);
            aTimer.Interval = 30000;
            aTimer.Enabled = true;
            KSPAlternateController.print(URL);
            this.req = (HttpWebRequest)WebRequest.Create(URL);
            AsyncHttpResponse(this.req, (response) =>
            {
                if (!ImageChecker.shouldBase64(response.ContentType.ToLower()))
                {
                    var body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    WebSocketHandler.me.SendTo(client.ID, new ResponseToHttpRequest(ID, false, response.ContentType, body).toJson());
                }
                else
                {
                    var body = new BinaryReader(response.GetResponseStream());
                    WebSocketHandler.me.SendTo(client.ID, new ResponseToHttpRequest(ID, true, response.ContentType, System.Convert.ToBase64String(body.ReadBytes((int)response.ContentLength))).toJson());

                }
            });
        }

        /* Thanks to: http://stackoverflow.com/questions/202481/how-to-use-httpwebrequest-net-asynchronously */
        private void AsyncHttpResponse(HttpWebRequest request, Action<HttpWebResponse> responseAction)
        {
            // Useragent required for reddit API
            request.UserAgent = "V0.1 Kerbal Space Program Controller /u/Plornt";
            Action wrapperAction = () =>
            {
                request.BeginGetResponse(new AsyncCallback((iar) =>
                {
                    HttpWebResponse response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);

                    responseAction(response);
                }), request);
            };
            wrapperAction.BeginInvoke(new AsyncCallback((iar) =>
            {
                var action = (Action)iar.AsyncState;
                action.EndInvoke(iar);
            }), wrapperAction);
        }

        public System.Object context { get; set; }
    }
}
