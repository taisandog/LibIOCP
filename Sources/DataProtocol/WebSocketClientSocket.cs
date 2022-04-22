using Buffalo.Kernel;
using GameBoxIOCP.DataProtocol;
using LibIOCP.DataProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

        private bool _isServerSocket = false;
        /// <summary>
        /// 是否监听创建的连接
        /// </summary>
        public bool IsServerSocket 
        {
            get { return _isServerSocket; }
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
        public WebSocketClientSocket(Socket socket, int maxSendPool, int maxLostPool, HeartManager heartManager, INetProtocol netProtocol = null, bool isServerSocket = false)
        : base(socket, maxSendPool, maxLostPool, heartManager, netProtocol)
        {
            _isServerSocket=isServerSocket;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public WebSocketClientSocket(Socket socket, HeartManager heartManager, INetProtocol netProtocol = null)
            : this(socket, 15, 15, heartManager, netProtocol)
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
        private bool _hasWebSocketFirstTransfer = false;

        /// <summary>
        /// 是否Websocket进行了首次传输
        /// </summary>
        public bool HasWebSocketFirstTransfer
        {
            get { return _hasWebSocketFirstTransfer; }
            set { _hasWebSocketFirstTransfer = value; }
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
        public void SendHandShake(string host, string webSocketKey=null)
        {
            if (string.IsNullOrWhiteSpace(webSocketKey))
            {
                webSocketKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            }
            string handShakeStr = ProtocolDraft10.GetWebSocketHandShake(host, webSocketKey);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(handShakeStr);
            _bindSocket.Send(data);
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
            try
            {
                if (_msMessage != null)
                {
                    _msMessage.Dispose();
                }
            }
            catch { }
            _msMessage = null;
            base.Close();
        }
    }
}
