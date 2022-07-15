﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace LibIOCP.DataProtocol
{
    public class FastClientSocket : ClientSocketBase
    {
        private static INetProtocol _defaultAdapter = new FastNetAdapter();

        protected override INetProtocol CreateDefaultAdapter()
        {
            return _defaultAdapter;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public FastClientSocket(Socket socket, int maxSendPool, int maxLostPool, HeartManager heartManager,
            bool isServerSocket, INetProtocol netProtocol = null, SocketCertConfig certConfig = null)
        : base(socket, maxSendPool, maxLostPool, isServerSocket, heartManager, netProtocol, certConfig)
        {
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public FastClientSocket(Socket socket, HeartManager heartManager, bool isServerSocket, 
            INetProtocol netProtocol = null, SocketCertConfig certConfig = null)
            : this(socket, 15, 15, heartManager, isServerSocket, netProtocol,certConfig)
        {

        }

        private string _userId;
        /// <summary>
        /// 用户id
        /// </summary>
        public string UserId
        {
            get
            {
                return _userId;
            }
            set
            {
                _userId = value;
            }
        }
        private int _curId = 0;

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="lost">是否判断丢失</param>
        /// param name="mergeTag">合并标签</param>
        /// <param name="verify">是否验证</param>
        public FastDataPacket Send(byte[] data,
            object mergeTag = null)
        {
            int curId = 0;
            lock (this)
            {
                _curId++;
                curId = _curId;
            }
            return Send(curId, data, mergeTag);

        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="lost">是否判断丢失</param>
        /// param name="mergeTag">合并标签</param>
        /// <param name="verify">是否验证</param>
        public FastDataPacket Send(int packetId, string data,
            object mergeTag = null)
        {
            return Send(packetId, System.Text.Encoding.UTF8.GetBytes(data), mergeTag);
        }
        private static int MaxId = int.MaxValue - 1000;
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="lost">是否判断丢失</param>
        /// param name="mergeTag">合并标签</param>
        /// <param name="verify">是否验证</param>
        public FastDataPacket Send(string data,object mergeTag = null)
        {
            int curId = 0;
            lock (this)
            {
                _curId++;
                if(_curId> MaxId) 
                {
                    _curId=0;
                }
                curId = _curId;
            }
            return Send(curId, data, mergeTag);

        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="lost">是否判断丢失</param>
        /// param name="mergeTag">合并标签</param>
        /// <param name="verify">是否验证</param>
        public FastDataPacket Send(int packetId, byte[] data,
             object mergeTag = null)
        {
            FastDataPacket packet = null;
            if (!Connected)
            {
                packet = new FastDataPacket(packetId, data, false, _netProtocol);
                return packet;
            }
            packet = new FastDataPacket(packetId, data, false, _netProtocol);
            //packet.IsVerify = verify;
            packet.MergeTag = mergeTag;
            SendPacket(packet);
            return packet;

        }

        public override DataPacketBase Send(byte[] data)
        {
            return Send(data,null);
        }
        public override DataPacketBase Send(string data)
        {
            return Send(System.Text.Encoding.UTF8.GetBytes(data), null);
        }
        public override void DoDataPacket(DataPacketBase dataPacket, DateTime recDate)
        {
            if (dataPacket != null && HasReceiveDataHandle)
            {
                if (dataPacket.IsHeart)
                {
                    return;
                }
                RunReceiveData(dataPacket);


            }
        }

        public override void SendHeard()
        {
            Send(0,(byte[])null);
        }
    }
}
