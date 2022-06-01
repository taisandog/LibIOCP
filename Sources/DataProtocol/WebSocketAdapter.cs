using Buffalo.Kernel;
using LibIOCP;
using LibIOCP.DataProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LibIOCP.DataProtocol
{
    /// <summary>
    /// WebSocket适配器
    /// </summary>
    public class WebSocketAdapter : INetProtocol
    {

        public int BufferLength
        {
            get { return 1024; }
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="mess"></param>
        /// <returns>返回是否被处理</returns>
        protected virtual bool DoWSMessage(Message mess, WebSocketClientSocket socket, out DataPacketBase recPacket)
        {
            recPacket = null;
            OperType otype = mess.Header.Opcode;
            switch (otype)
            {
                case OperType.Binary:
                case OperType.Text:
                    recPacket = Format( mess.Payload);
                    return true;

                case OperType.Ping:
                    socket.SendPong();
                    return true;
                case OperType.Pong:
                case OperType.Row:
                    return true;
                case OperType.Close:
                    socket.Close();
                    return true;
                default:
                    break;
            }
            return true;
        }

        
        /// <summary>
        /// 数据包空包长度
        /// </summary>
        public int PACKET_LENGHT
        {
            get
            {
                return ProtocolDraft10.EmptyPacketLength;
            }
        }


        /// <summary>
        /// 将数据包输出为数组
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(DataPacketBase packetBase)
        {
            WebSocketDataPacket packet = packetBase as WebSocketDataPacket;
            byte[] result=null;
            if (packet.Data == null)
            {
                result = new byte[0];
            }
            else
            {
                result = new byte[packet.Data.Length];
            }
       
            
            if (packet.Data != null)
            {
                result = ProtocolDraft10.PackageServerData(packet.Data, packet.WebSocketMessageType, packet.WebSocketMask);
                //Array.Copy(packet.Data, 0, result, 0, packet.Data.Length);
            }
            return result;
        }



        /// <summary>
        /// 根据数据格式化一个包,如果数据长度小于最小返回null
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>失败返回null</returns>
        public WebSocketDataPacket Format(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            WebSocketDataPacket packet = CreateDataPacket(0, false, data,false) as WebSocketDataPacket;

            return packet;

        }

        private object _lokRootObject = new object();
        /// <summary>
        /// 判断数据是否合法,并进行一次数据解析
        /// </summary>
        /// <returns>是否进行下一次判断</returns>
        public bool IsDataLegal(out DataPacketBase recPacket, ClientSocketBase socket)
        {
            Console.WriteLine("有数据");
            NetByteBuffer buffer = socket.BufferData;
            DataManager dataManager = socket.DataManager;
            recPacket = null;
            WebSocketClientSocket wsocket = socket as WebSocketClientSocket;
            if(wsocket == null)
            {
                return false;
            }
            if (!wsocket.HasWebSocketFirstTransfer) 
            {
                byte[] allData=new byte[buffer.Count];
                buffer.ReadBytes(0, allData, 0, allData.Length);
                if (wsocket.IsServerSocket)
                {
                    if (ProtocolDraft10.IsWebSocketHandShake(allData, 0, allData.Length))
                    {
                        ProtocolDraft10.ResponseWebSocketHandShake(allData, wsocket);//握手
                    }
                    else 
                    {
                        wsocket.Close();
                        return false;
                    }
                }
                else
                {
                    if (!ProtocolDraft10.IsHandShakeResponse(allData, 0, allData.Length))
                    {
                        wsocket.Close();
                        return false;
                    }
                }
                buffer.RemoveHeadBytes(allData.Length);
                wsocket.HasWebSocketFirstTransfer = true;
                return true;
            }

            if (dataManager == null || buffer == null)
            {
                return false;
            }
            //检查是否达到最小包长度
            if (buffer.Count < PACKET_LENGHT)
            {
                return false;
            }
            bool ret = false;
            Message mess = ProtocolDraft10.AnalyzeClientData(buffer);
            if (mess == null)
            {
                return false;
            }

            WebSocketClientSocket wssocket = socket as WebSocketClientSocket;
            if (mess.Header.FIN)
            {
                if (!wssocket.IsWSBufferEmpty)
                {
                    NetByteBuffer msMessage = wssocket.BufferMessage;
                    msMessage.AppendBytes(mess.Payload, 0, mess.Payload.Length);
                    mess.Payload = msMessage.ToByteArray();
                    msMessage.Clear();
                }

                ret = DoWSMessage(mess, wssocket, out recPacket);
            }
            else
            {
                NetByteBuffer msMessage = wssocket.BufferMessage;
                msMessage.AppendBytes(mess.Payload, 0, mess.Payload.Length);
                ret = true;
            }


            return ret;
        }

        

        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <returns></returns>
        public DataPacketBase CreateDataPacket(object packetId, bool lost, byte[] data, bool verify)
        {
            WebSocketDataPacket packet = new WebSocketDataPacket((int)packetId, lost, data, this);
            packet.WebSocketMessageType = OperType.Text;
            return packet;
        }

        /// <summary>
        /// 创建socket连接
        /// </summary>
        /// <returns></returns>
        public ClientSocketBase CreateClientSocket(Socket socket, int maxSendPool = 15, int maxLostPool = 15,
            HeartManager heartManager = null, bool isServerSocket = false, SocketCertConfig certConfig = null)
        {
            WebSocketClientSocket ret = new WebSocketClientSocket(socket, maxSendPool, maxLostPool, heartManager, isServerSocket, this,certConfig);
            return ret;
        }

        public void Log(string message)
        {
            if (_message != null)
            {
                _message.Log(message);
            }
        }

        public void LogError(string message)
        {
            if (_message != null)
            {
                _message.LogError(message);
            }
        }

        public void LogWarning(string message)
        {
            if (_message != null)
            {
                _message.LogWarning(message);
            }
        }

        
        public bool ShowLog
        {
            get
            {
                if (_message == null)
                {
                    return false;
                }
                return _message.ShowLog;
            }

        }
        public bool ShowError
        {
            get
            {
                if (_message == null)
                {
                    return false;
                }
                return _message.ShowError;
            }

        }
        public bool ShowWarning
        {
            get
            {
                if (_message == null)
                {
                    return false;
                }
                return _message.ShowWarning;
            }

        }

        public object EmptyPacketId
        {
            get
            {
                return 0;
            }
        }

        
        private IConnectMessage _message;

        public IConnectMessage Messager
        {
            get
            {
                return _message;
            }
            set { _message = value; }
        }

       
    }
}
