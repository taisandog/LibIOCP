﻿using Buffalo.Kernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace LibIOCP.DataProtocol
{
    public class FastNetAdapter : INetProtocol
    {
        /// <summary>
        /// 数据包头
        /// </summary>
        private byte[] _DATA_HEAD = new byte[] { 0X00, 0X0F };
        /// <summary>
        /// 包头
        /// </summary>
        public byte[] DATA_HEAD
        {
            get
            {
                return _DATA_HEAD;
            }
        }

        /// <summary>
        /// 数据包尾
        /// </summary>
        public byte[] DATA_TAIL
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 数据包空包长度
        /// </summary>
        public int _PACKET_LENGHT = 10;
        /// <summary>
        /// 数据包空包长度
        /// </summary>
        public int PACKET_LENGHT
        {
            get
            {
                return _PACKET_LENGHT;
            }
        }

        /// <summary>
        /// 将数据包输出为数组
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(DataPacketBase packet)
        {
            using (MemoryStream stm = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(stm))
                {
                    bw.Write(_DATA_HEAD);
                    byte[] packetId = IntToBytes((int)packet.PacketID);
                    bw.Write(packetId);

                    int len=0;

                    if (packet.Data != null)
                    {
                        len = packet.Data.Length;
                    }

                    byte[] prefixBytes = IntToBytes(len);
                    bw.Write(prefixBytes);
                    if (packet.Data != null)
                    {
                        bw.Write(packet.Data);
                    }
                }
                return stm.ToArray();
            }
        }

        /// <summary>
        /// 整型转Java字节数组
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private byte[] IntToBytes(int number)
        {
            byte[] result = new byte[4];
            result[0] = (byte)((number >> 24) & 0xFF);
            result[1] = (byte)((number >> 16) & 0xFF);
            result[2] = (byte)((number >> 8) & 0xFF);
            result[3] = (byte)(number & 0xFF);
            return result;
        }
        /// <summary>
        /// Java字节数组转整型
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private int BytesToInt(NetByteBuffer arrayBytes,int startIndex)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                int num = (3 - i) * 8;
                value += (arrayBytes[i+startIndex] & 0xFF) << num;
            }
            return value;
        }


        /// <summary>
        /// 根据数据格式化一个包,如果数据长度小于最小返回null
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>失败返回null</returns>
        public FastDataPacket Format(int packetId, byte[] data)
        {
            if (data == null || data.Length < _PACKET_LENGHT)
            {
                return null;
            }
            FastDataPacket packet = null;
            //判断是否为空包
            if (data.Length == _PACKET_LENGHT)
            {
                packet = new FastDataPacket(packetId, null, false, this);
            }
            else
            {
                byte[] da = new byte[data.Length - _PACKET_LENGHT];
                Array.Copy(data, _PACKET_LENGHT, da, 0, da.Length);
                packet = new FastDataPacket(packetId, da, false, this);
            }
            packet.IsHeart = packetId <= 0;
            return packet;
        }

       
        private const int MaxLen = 1024 * 1024 * 1024;

        private object _lokRootObject = new object();
        /// <summary>
        /// 判断数据是否合法,并进行一次数据解析
        /// </summary>
        /// <returns>是否进行下一次判断</returns>
        public bool IsDataLegal(out DataPacketBase recPacket, ClientSocketBase socket)
        {
            recPacket = null;
            NetByteBuffer bufferData = socket.BufferData;
            DataManager dataManager = socket.DataManager;
            //检查是否达到最小包长度
            if (bufferData.Count < _PACKET_LENGHT)
            {
                return false;
            }
            //检查包头是否正确，出错就重新寻找包头
            if (bufferData[0] != _DATA_HEAD[0] || bufferData[1] != _DATA_HEAD[1])
            {
                for (int i = 2; i < bufferData.Count - 1; i++)
                {
                    if (bufferData[i] == _DATA_HEAD[0] && bufferData[i + 1] == _DATA_HEAD[1])
                    {

                        bufferData.RemoveHeadBytes(i);
                        return true;
                    }
                    if (i == bufferData.Count - 2)//没有找到数据包头,清空缓存
                    {
                        bufferData.Clear();
                        return false;
                    }
                }
            }

            int packetId= BytesToInt(bufferData, 2);
            if (packetId < 0) //错误长度.丢弃包头
            {
                bufferData.RemoveHeadBytes(2);
                return true;
            }
            //检查收到的包的长度是否达到
            int dataLenght = BytesToInt(bufferData, 6);// new byte[] { bufferData[2], bufferData[3], bufferData[4], bufferData[5] });
            if (dataLenght >= MaxLen) //错误长度.丢弃包头
            {
                bufferData.RemoveHeadBytes(2);
                return true;
            }
            int totalLen = dataLenght + _PACKET_LENGHT;//总长度
            if (bufferData.Count < totalLen)
            {
                return false;
            }



            //检查通过，从缓存中取出数据，开始进行数据操作
            byte[] tempdata = new byte[totalLen];

            bufferData.ReadBytes(0, tempdata, 0, tempdata.Length);

            bufferData.RemoveHeadBytes(totalLen);

            //socket.LastReceiveTime = DateTime.Now;

            if (packetId <= 0)//心跳包
            {
                
                return false;
            }

            FastDataPacket dataPacket = Format(packetId, tempdata);

            if (socket.HasReceiveDataHandle)
            {
                lock (_lokRootObject)
                {
                    if (dataManager != null)
                    {
                        recPacket = dataPacket;
                    }
                }
            }


            return true;
        }



        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <returns></returns>
        public DataPacketBase CreateDataPacket(object packetId,  bool isLost, byte[] data, bool verify)
        {
            FastDataPacket packet = new FastDataPacket((int)packetId, data, isLost, this);
            return packet;
        }
       
        /// <summary>
        /// 创建socket连接
        /// </summary>
        /// <returns></returns>
        public ClientSocketBase CreateClientSocket(Socket socket, int maxSendPool = 15, int maxLostPool = 15,
            HeartManager heartManager = null, bool isServerSocket = false)
        {
            FastClientSocket ret = new FastClientSocket(socket, maxSendPool, maxLostPool, heartManager,this );
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
        private IConnectMessage _message;

        public IConnectMessage Messager
        {
            get
            {
                return _message;
            }
            set { _message = value; }
        }

        public object EmptyPacketId
        {
            get
            {
                return null;
            }
        }

        public int BufferLength 
        {
            get { return 1024; }
        }
    }
}