using Microsoft.Kinect;
using Newtonsoft.Json;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectWebServer
{
    public class ColorFrameData
    {
        public byte[] Data { get; set; }
        public ColorImageFormat Format { get; set; }
    }

    class Program
    {
        static KinectSensor ks;
        static MultiSourceFrameReader msfr;
        static WebSocketServer appServer;
        static ColorFrameData colorData = new ColorFrameData();

        static void Main(string[] args)
        {
            ks = KinectSensor.GetDefault();

            var fd = ks.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            colorData.Data = new byte[fd.BytesPerPixel * fd.LengthInPixels];
            colorData.Format = ColorImageFormat.Bgra;

            msfr = ks.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);

            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;
            ks.Open();

            Console.WriteLine("Press any key to start the WebSocketServer!");

            Console.ReadKey();
            Console.WriteLine();

            appServer = new WebSocketServer();

            var config = new ServerConfig();
            config.Name = "kinect";
            config.Port = 2012;

            // Setup the appServer 
            if (!appServer.Setup(config)) //Setup with listening port 
            {
                Console.WriteLine("Failed to setup!");
                Console.ReadKey();
                return;
            }

            //appServer.NewMessageReceived += new SessionHandler<WebSocketSession, string>(appServer_NewMessageReceived);

            Console.WriteLine();

            // Try to start the appServer 
            if (!appServer.Start())
            {
                Console.WriteLine("Failed to start!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("The server started successfully, press key 'q' to stop it!");

            while (Console.ReadKey().KeyChar != 'q')
            {
                Console.WriteLine();
                continue;
            }

            //Stop the appServer 
            appServer.Stop();

            Console.WriteLine();
            Console.WriteLine("The server was stopped!");
            Console.ReadKey();
        }

        static void msfr_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (e.FrameReference == null)
                return;
            var multiFrame = e.FrameReference.AcquireFrame();
            if (multiFrame == null)
                return;

            bool colorRead = false;
            if (multiFrame.ColorFrameReference != null)
            {
                using (var cf = multiFrame.ColorFrameReference.AcquireFrame())
                {
                    cf.CopyConvertedFrameDataToArray(colorData.Data, colorData.Format);
                    colorRead = true;
                }
            }

            if (colorRead == true)
            {
                SendColorDataAsync(colorData);
            }
        }

        private async static Task SendColorDataAsync(ColorFrameData data)
        {
            if (data == null)
                return;
            var sessions = appServer.GetAllSessions();
            foreach (var session in sessions)
            {
                var str = await JsonConvert.SerializeObjectAsync(data);
                if (!string.IsNullOrEmpty(str))
                {
                    //Console.WriteLine("Sending Colour Frame");
                    // serialise color frame data and send
                    session.Send(str);
                }
            }
        }

        //static void appServer_NewMessageReceived(WebSocketSession session, string message)
        //{
        //    //Send the received message back 
        //    session.Send("Server: " + message);
        //}
    }
}
