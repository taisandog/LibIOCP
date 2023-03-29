using Buffalo.Kernel;
using LibIOCP.DataProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LibIOCP.DataProtocol
{
    public class WebSocketClientSocket : ClientSocketBase
    {
        

        protected NetByteBuffer _msMessage = null;
        /// <summary>
        /// WebSocket分段数据的缓冲
        /// </summary>
        public NetByteBuffer BufferMessage
        {
            get
            {
                if (_msMessage == null)
                {
                    _msMessage = new NetByteBuffer(256);
                }
                return _msMessage;
            }
        }
        /// <summary>
        /// 判断缓冲区是否为空
        /// </summary>
        public bool IsWSBufferEmpty
        {
            get
            {
                return _msMessage == null || _msMessage.Count <= 0;
            }
        }

       

        private static INetProtocol _defaultAdapter = new WebSocketAdapter();

        protected override INetProtocol CreateDefaultAdapter()
        {
            return _defaultAdapter;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public WebSocketClientSocket(Socket socket, int maxSendPool, int maxLostPool, HeartManager heartManager, bool isServerSocket,
            INetProtocol netProtocol = null, SocketCertConfig certConfig = null)
        : base(socket, maxSendPool, maxLostPool, isServerSocket, heartManager, netProtocol,certConfig)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public WebSocketClientSocket(Socket socket, HeartManager heartManager, bool isServerSocket=false, 
            INetProtocol netProtocol = null, SocketCertConfig certConfig = null)
            : this(socket, 15, 15, heartManager,isServerSocket, netProtocol, certConfig)
        {

        }

        public override DataPacketBase Send(byte[] data)
        {
           
            WebSocketDataPacket dp = _netProtocol.CreateDataPacket(0, false, data,false) as WebSocketDataPacket;
            dp.WebSocketMessageType = OperType.Binary;
            SendPacket(dp);
            return dp;
        }
        public override DataPacketBase Send(string data)
        {
            return SendText(data);
           
        }
        

        /// <summary>
        /// 是否Websocket进行了首次传输
        /// </summary>
        public bool HasWebSocketFirstTransfer
        {
            get { return _hanshakeInfo==null; }
        }
        /// <summary>
        /// 握手信息
        /// </summary>
        private WebSocketHandshake _hanshakeInfo;
        /// <summary>
        /// 握手信息
        /// </summary>
        public WebSocketHandshake HanshakeInfo 
        {
            get { return _hanshakeInfo; }
        }

        /// <summary>
        /// 服务器握手
        /// </summary>
        /// <param name="content"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public WebSocketHandshake ServerHandshake(byte[] content, int start, int count) 
        {
            WebSocketHandshake hanshakeInfo = new WebSocketHandshake(content, start, count);
            _hanshakeInfo = hanshakeInfo;
            hanshakeInfo.IsSuccess = hanshakeInfo.HandshakeContent.ContainsKey("Sec-WebSocket-Key");
            return hanshakeInfo;
            
        }
        /// <summary>
        /// 服务器握手
        /// </summary>
        /// <param name="content"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public WebSocketHandshake ClientHandshake(byte[] content, int start, int count)
        {
            WebSocketHandshake hanshakeInfo = new WebSocketHandshake(content, start, count);
            _hanshakeInfo = hanshakeInfo;
            hanshakeInfo.IsSuccess = hanshakeInfo.HandshakeContent.ContainsKey("Sec-WebSocket-Accept");
            return hanshakeInfo;
            
        }
        public override void SendHeard()
        {
            SendPing();
        }
        /// <summary>
        /// 回复心跳
        /// </summary>
        public void SendPong()
        {
            WebSocketDataPacket data =_netProtocol.CreateDataPacket(0, false, null, false) as WebSocketDataPacket;
            data.IsHeart = true;
            data.WebSocketMask = null;
            data.WebSocketMessageType = OperType.Pong;
            SendPacket(data);
        }
        /// <summary>
        /// 回复心跳
        /// </summary>
        public void SendPing()
        {
            WebSocketDataPacket data = _netProtocol.CreateDataPacket(0, false, null, false) as WebSocketDataPacket;
            data.IsHeart = true;
            data.WebSocketMask = null;
            data.WebSocketMessageType = OperType.Ping;
            SendPacket(data);
        }

        /// <summary>
        /// 发送握手
        /// </summary>
        /// <param name="host">主机</param>
        /// <param name="getParam">get内容参数</param>
        /// <param name="webSocketKey">指定的webSocketKey</param>
        public void SendHandShake(string host,string getParam=null, string webSocketKey=null)
        {
            if (string.IsNullOrWhiteSpace(webSocketKey))
            {
                SHA1 sha1 = new SHA1CryptoServiceProvider();//创建SHA1对象

                webSocketKey = Convert.ToBase64String(sha1.ComputeHash(Guid.NewGuid().ToByteArray()));
            }
            if (string.IsNullOrWhiteSpace(getParam)) 
            {
                getParam = "/";
            }
            string handShakeStr = ProtocolDraft10.GetWebSocketHandShake(host, getParam, webSocketKey);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(handShakeStr);

            SendRaw(data);
        }
        /// <summary>
        /// 发送握手失败
        /// </summary>
        /// <param name="server">服务器(Server:)</param>
        /// <param name="httpError">http标记错误，例如:HTTP/1.1 500 ServerError</param>
        /// <param name="messparams">其他参数</param>
        /// <param name="message">Ws_err_msg错误信息</param>
        /// <param name="postData">其他内容</param>
        public void SendHandShakeFail(string server=null, string httpError=null, IDictionary<string, string> messparams = null,
            string message = null, string postData = null)
        {

            string handShakeStr = ProtocolDraft10.GetWebSocketHandShakeFail(server, httpError, messparams, message, postData);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(handShakeStr);

            SendRaw(data);
        }
        public DataPacketBase SendText(string text) 
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            WebSocketDataPacket dp = _netProtocol.CreateDataPacket(0, false, data,false) as WebSocketDataPacket;
            dp.WebSocketMessageType=OperType.Text;
            SendPacket(dp);
            return dp;
        }

        public override void Close()
        {
            lock (_lokRootObject)
            {
                NetByteBuffer msbuff = _msMessage;
                
                try
                {
                    if (msbuff != null)
                    {
                        msbuff.Dispose();
                    }
                }
                catch { }
                _msMessage = null;

                if (_hanshakeInfo != null) 
                {
                    _hanshakeInfo.Dispose();
                }
                _hanshakeInfo = null;
            }
            base.Close();
        }
    }
}
