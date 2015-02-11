using Microsoft.Kinect;
using SuperSocket.SocketBase.Config;
using SuperWebSocket;
using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        static byte[] encodedBytes;

        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to start the WebSocketServer!");

            Console.ReadKey();
            Console.WriteLine();

            ks = KinectSensor.GetDefault();

            var fd = ks.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            uint frameSize = fd.BytesPerPixel * fd.LengthInPixels;
            colorData.Data = new byte[frameSize];
            colorData.Format = ColorImageFormat.Bgra;

            msfr = ks.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);

            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;
            ks.Open();

            appServer = new WebSocketServer();

            var config = new ServerConfig();
            config.Name = "kinect";
            config.Port = 2012;
            config.MaxRequestLength = (int)frameSize;

            // Setup the appServer 
            if (!appServer.Setup(config)) //Setup with listening port 
            {
                Console.WriteLine("Failed to setup!");
                Console.ReadKey();
                return;
            }

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
            FrameDescription fd = null;
            if (multiFrame.ColorFrameReference != null)
            {
                using (var cf = multiFrame.ColorFrameReference.AcquireFrame())
                {
                    fd = cf.ColorFrameSource.FrameDescription;
                    cf.CopyConvertedFrameDataToArray(colorData.Data, colorData.Format);
                    colorRead = true;
                }
            }

            if (colorRead == true)
            {
                SendColorData(colorData, fd);
            }
        }

        private static void SendColorData(ColorFrameData data, FrameDescription fd)
        {
            if (data == null)
                return;
            var sessions = appServer.GetAllSessions();
            if (sessions.Count() < 1)
                return;

            var dpiX = 96.0;
            var dpiY = 96.0;
            var pixelFormat = PixelFormats.Bgra32;
            var bytesPerPixel = (pixelFormat.BitsPerPixel) / 8;
            var stride = bytesPerPixel * fd.Width;

            var bitmap = BitmapSource.Create(fd.Width, fd.Height, dpiX, dpiY,
                                             pixelFormat, null, data.Data, (int)stride);
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var ms = new MemoryStream())
            using (var br = new BinaryReader(ms))
            {
                encoder.Save(ms);
                ms.Flush();
                ms.Position = 0;
                encodedBytes = br.ReadBytes((int)ms.Length);
            }

            foreach (var session in sessions)
            {
                session.Send(encodedBytes, 0, encodedBytes.Length);
            }
        }
    }
}
