
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LibIOCP.DataProtocol
{
    public class ClientSocket: ClientSocketBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public ClientSocket(Socket socket, int maxSendPool, int maxLostPool, HeartManager heartManager, INetProtocol netProtocol = null)
        : base(socket, maxSendPool, maxLostPool, heartManager, netProtocol)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public ClientSocket(Socket socket, HeartManager heartManager, INetProtocol netProtocol = null)
            : this(socket, 15, 15, heartManager, netProtocol)
        {
           
        }
        public override DataPacketBase Send(byte[] data)
        {
            return Send(data, false, false,null);
        }
        public override DataPacketBase Send(string data)
        {
            return Send(System.Text.Encoding.UTF8.GetBytes(data), false,  false, null);
        }

        /// <summary>
        /// 发送数据包ID
        /// </summary>
        private int _sendPakcetID =0;
        

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="lost">是否判断丢失</param>
        /// <param name="verify">是否验证</param>
        public DataPacket Send(string data, bool lost = false, bool verify = false,  object mergeTag = null)
        {
            byte[] content = System.Text.Encoding.UTF8.GetBytes(data);
            return Send(content, lost, verify, mergeTag);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="lost">是否判断丢失</param>
        /// param name="mergeTag">合并标签</param>
        /// <param name="verify">是否验证</param>
        public DataPacket Send(byte[] data, bool lost = false, bool verify = false, 
            object mergeTag = null)
        {
            DataPacket packet = null;
            if (!Connected)
            {
                packet= new DataPacket(0, lost, data, verify, _netProtocol);

                return packet;
            }
            Interlocked.Increment(ref _sendPakcetID);
            packet = new DataPacket(_sendPakcetID, lost, data, verify, _netProtocol);

            packet.MergeTag = mergeTag;
            SendPacket(packet);
            return packet;

        }
    }
}
