
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace LibIOCP.DataProtocol
{
    /// <summary>
    /// 网络协议
    /// </summary>
    public interface INetProtocol: IConnectMessage
    {
        /// <summary>
        /// 数据包空包长度
        /// </summary>
        int PACKET_LENGHT
        {
            get;
        }

        /// <summary>
        /// 将数据包输出为数组
        /// </summary>
        /// <returns></returns>
        byte[] ToArray(DataPacketBase packet);

       
        /// <summary>
        /// 判断数据是否合法,并进行一次数据解析
        /// </summary>
        /// <returns>是否进行下一次判断</returns>
        bool IsDataLegal(out DataPacketBase recPacket, ClientSocketBase socket);
        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <returns></returns>
        DataPacketBase CreateDataPacket(object packetId, bool lost, byte[] data,bool verify);

        /// <summary>
        /// 创建socket连接
        /// </summary>
        /// <param name="socket">连接</param>
        /// <param name="maxSendPool">最大发送池</param>
        /// <param name="maxLostPool">最大重发池</param>
        /// <param name="heartManager">心跳管理</param>
        /// <param name="isServerSocket">是否监听创建连接</param>
        /// <returns></returns>
        ClientSocketBase CreateClientSocket(Socket socket, int maxSendPool = 15, int maxLostPool = 15,
            HeartManager heartManager = null,bool isServerSocket=false, SocketCertConfig certConfig = null);

        /// <summary>
        /// 空ID
        /// </summary>
        /// <param name="packetId"></param>
        /// <returns></returns>
        object EmptyPacketId { get; }
        /// <summary>
        /// 缓冲长度
        /// </summary>
        int BufferLength { get; }
    }
}
