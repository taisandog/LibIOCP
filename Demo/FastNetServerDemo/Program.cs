using LibIOCP;
using LibIOCP.DataProtocol;
using Microsoft.AspNetCore.Hosting.Server;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace FastNetServerDemo
{
    internal class Program
    {
        private static ServerSocket _serverFast = null;
        private static ServerSocket _serverWebSocket = null;
        private static HeartManager _heart = null;
        private static ServerMessageLog _log = new ServerMessageLog();

        /// <summary>
        /// 默认协议
        /// </summary>
        private static FastNetAdapter _defaultNetAdapter = new FastNetAdapter();

        /// <summary>
        /// WebSocket协议
        /// </summary>
        private static WebSocketAdapter _wsNetAdapter = null;
        static void Main(string[] args)
        {

            _heart = new HeartManager(20000, 5000, 1000, _log);
            _heart.StartHeart();
            _serverFast = ConnectFast(8587);
            _serverWebSocket = ConnectWebSocket(8588);

            Console.WriteLine("服务开启");
            string line = null;
            while (true)
            {
                Console.WriteLine("请输入命令");
                line = Console.ReadLine();
                if (string.Equals(line, "exit", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("正在停止");
                    break;
                }

            }
        }

        /// <summary>
        /// 简易协议
        /// </summary>
        /// <returns></returns>
        private static ServerSocket ConnectFast(int port) 
        {
            ServerSocket server = new ServerSocket("0.0.0.0", port, _heart, _defaultNetAdapter, _log);

            server.OnAccept += Server_OnAccept;
            server.OnClose += Server_OnClose;
            server.OnReceiveData += server_OnReceiveData;
            server.OnMessage += Server_OnMessage;
            server.OnError += Server_OnError;
            server.Start();
            Console.WriteLine("Fast:0.0.0.0:" + port);
            return server;
        }
        /// <summary>
        /// 简易协议
        /// </summary>
        /// <returns></returns>
        private static ServerSocket ConnectWebSocket(int port)
        {
            _wsNetAdapter = new WebSocketAdapter();
            _wsNetAdapter.OnSendPacket += _wsNetAdapter_OnSendPacket;
            
            ServerSocket server = new ServerSocket("0.0.0.0", port, _heart, _wsNetAdapter, _log);

            ////此段是ssl模式的wss使用的证书方式验证
            //string fileName = "app_data\\cert.cer";
            //string password = "";
            //if (!string.IsNullOrWhiteSpace(fileName))
            //{
            //    X509Certificate2 cert = null;
            //    if (!string.IsNullOrWhiteSpace(password))
            //    {
            //        cert = new X509Certificate2(fileName, password, X509KeyStorageFlags.Exportable);
            //    }
            //    else
            //    {
            //        cert = new X509Certificate2(fileName);
            //    }
            //    SslProtocols sslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
            //    //X509Certificate2 cert = new X509Certificate2(fileName);

            //    server.CertConfig = SocketCertConfig.CreateServerConfig(cert, false, sslProtocols, false, true, DoRemoteCertificateValidation, null, EncryptionPolicy.AllowNoEncryption);

            //}


            server.OnAccept += Server_OnAccept;
            server.OnClose += Server_OnClose;
            server.OnReceiveData += server_OnReceiveData;
            server.OnMessage += Server_OnMessage;
            server.OnError += Server_OnError;
            server.Start();
            Console.WriteLine("Websocket:ws://0.0.0.0:" + port);
            return server;
        }

        private static void _wsNetAdapter_OnSendPacket(DataPacketBase packet)
        {
            WebSocketDataPacket dp = packet as WebSocketDataPacket;
            if (dp != null)
            {
                dp.WebSocketMessageType = OperType.Text;//强行把send(byte[])的类型改成Text类型
            }
        }
        static void server_OnReceiveData(ClientSocketBase socket, DataPacketBase data)
        {
            try
            {
                string error = null;
                byte[] bdata = data.Data;

                string mess=System.Text.Encoding.UTF8.GetString(bdata);
                if (_log.ShowLog)
                {
                    _log.Log("收到:" + mess);
                }
                socket.Send("服务器已收到:"+mess);
            }
            catch (Exception e)
            {
                _log.LogError(e.ToString());
            }
        }
        static void Server_OnClose(ClientSocketBase clientSocket)
        {
            try
            {
                
                if (_log.ShowLog)
                {
                    _log.LogWarning("用户断开" );
                }
                
            }
            catch (Exception e)
            {
                _log.LogError(e.ToString());
            }

        }
        private static bool Server_OnMessage(ClientSocketBase clientSocket, int type, object message)
        {
            WebSocketHandshake handshake = message as WebSocketHandshake;
            if( handshake != null ) 
            {
                Console.WriteLine("握手地址:"+handshake.Url+"参数:"+JsonConvert.SerializeObject(handshake.Param));
                
            }
            return true;
        }
        private static void Server_OnError(ClientSocketBase clientSocket, Exception ex)
        {
            try
            {
                _log.LogError(ex.ToString());
            }
            catch (Exception ex1) { }
        }
        static void Server_OnAccept(ClientSocketBase clientSocket)
        {
            //if (FCRUnit.IsDebug) 
            //{
            //    Console.WriteLine("当前连接数:" + _heart.Clients.Count);
            //}
        }
        public static bool DoRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {

            return true;//跳过验证合法性

        }
    }
}