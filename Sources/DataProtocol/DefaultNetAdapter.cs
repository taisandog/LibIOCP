using Buffalo.Kernel;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace LibIOCP.DataProtocol
{
    /// <summary>
    /// 默认协议
    /// </summary>
    public class DefaultNetAdapter: INetProtocol
    {
        /// <summary>
        /// 数据包头
        /// </summary>
        private byte[] _DATA_HEAD = new byte[] { 0xB7, 0x55 };
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
        public byte[] _DATA_TAIL = new byte[] { 0xF1, 0xB8 };
        /// <summary>
        /// 数据包尾
        /// </summary>
        public byte[] DATA_TAIL 
        {
            get 
            {
                return _DATA_TAIL;
            }
        }

        /// <summary>
        /// 数据包空包长度
        /// </summary>
        public int _PACKET_LENGHT = 16;
        /// <summary>
        /// 数据包空包长度
        /// </summary>
        public override int PACKET_LENGHT 
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
        public override byte[] ToArray(DataPacketBase packetBase)
        {
            DataPacket packet = packetBase as DataPacket;
            byte[] result;
            if (packet.Data == null)
            {
                result = new byte[_PACKET_LENGHT];
            }
            else
            {
                result = new byte[_PACKET_LENGHT + packet.Data.Length];
            }
            byte[] tempLenght = Util.IntToByte4(packet.Length);
            byte[] tempPacketID = Util.IntToByte4(packet.PIdValue);
            Array.Copy(DATA_HEAD, 0, result, 0, _DATA_HEAD.Length);
            Array.Copy(tempLenght, 0, result, 2, 4);
            Array.Copy(tempPacketID, 0, result, 6, 4);
            result[10] = (byte)(packet.IsLost ? 1 : 0);
            result[11] = (byte)(packet.IsVerify ? 1 : 0);
            result[12] = packet.PacketCRC;
            result[13] = packet.DataCRC;
            Array.Copy(DATA_TAIL, 0, result, result.Length - 2, DATA_TAIL.Length);
            if (packet.Data != null)
            {
                Array.Copy(packet.Data, 0, result, 14, packet.Data.Length);
            }
            return result;
        }

        /// <summary>
        /// 根据数据格式化一个包,如果数据长度小于最小返回null
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>失败返回null</returns>
        public DataPacket Format(byte[] data)
        {
            DataPacket packet = null;
            if (data == null || data.Length < _PACKET_LENGHT)
            {
                return null;
            }
            //判断是否为空包
            if (data.Length == _PACKET_LENGHT)
            {
                packet = new DataPacket(Util.ByteToInt4(new byte[] { data[6], data[7], data[8], data[9] }),false,null,false, this);
                if (packet.PIdValue <= 0)
                {
                    packet.IsHeart = true;
                }
            }
            else
            {
                int packetID = Util.ByteToInt4(new byte[] { data[6], data[7], data[8], data[9] });
                bool lost = data[10] == 1 ? true : false;
                bool verify = data[11] == 1 ? true : false;
                byte[] da = new byte[data.Length - PACKET_LENGHT];
                Array.Copy(data, 14, da, 0, da.Length);
                // byte[] data=
                packet=new DataPacket(packetID, lost, da, verify, this);
                
            }
            return packet;
        }
        private object _lokRootObject = new object();
        /// <summary>
        /// 判断数据是否合法,并进行一次数据解析
        /// </summary>
        /// <returns>是否进行下一次判断</returns>
        public override bool IsDataLegal(out DataPacketBase recPacket, ClientSocketBase socket)
        {
            recPacket = null;
            NetByteBuffer bufferData = socket.BufferData;
            DataManager dataManager = socket.DataManager;
            if(dataManager == null || bufferData == null) 
            {
                return false;
            }
            //检查是否达到最小包长度
            if (bufferData.Count < _PACKET_LENGHT)
            {
                return false;
            }
            //检查包头是否正确，出错就重新寻找包头
            if (bufferData[0] != _DATA_HEAD[0] || bufferData[1] != _DATA_HEAD[1])
            {


                if (ShowWarning)
                {
                    LogWarning("Packet head error,rediscover packet head");
                }

                for (int i = 2; i < bufferData.Count - 1; i++)
                {
                    if (bufferData[i] == _DATA_HEAD[0] && bufferData[i + 1] == _DATA_HEAD[1])
                    {
                        if (ShowLog)
                        {
                            Log("Find the Packet head");
                        }
                        //_bufferDataLenght -= i;
                        //Array.Copy(_bufferData, i, _bufferData, 0, _bufferDataLenght);
                        bufferData.RemoveHeadBytes(i);
                        return true;
                    }
                    if (i == bufferData.Count - 2)
                    {
                        bufferData.Clear();
                        //_bufferDataLenght = 0;
                        if (ShowWarning)
                        {
                            LogWarning("No packet head found,clear cache");
                        }
                        return false;
                    }
                }
            }
            //检验数据包CRC
            byte crc = Util.CreateCRC(bufferData[2],
                                       bufferData[3],
                                       bufferData[4],
                                       bufferData[5],
                                       bufferData[6],
                                       bufferData[7],
                                       bufferData[8],
                                       bufferData[9],
                                       bufferData[10],
                                       bufferData[11],
                                       bufferData[13]);
            if (crc != bufferData[12])
            {
                //_bufferDataLenght -= 2;
                //Array.Copy(_bufferData, 2, _bufferData, 0, _bufferDataLenght);
                bufferData.RemoveHeadBytes(2);

                if (ShowWarning)
                {
                    LogWarning("CRC error,remove packet head,find next packet!");
                }
                return true;
            }
            //检查收到的包的长度是否达到
            int dataLenght = Util.ByteToInt4(new byte[] { bufferData[2], bufferData[3], bufferData[4], bufferData[5] });
            if (bufferData.Count < dataLenght)
            {
                return false;
            }
            //检查指令结束符是否正确
            if (bufferData[dataLenght - 1] != _DATA_TAIL[1] || bufferData[dataLenght - 2] != _DATA_TAIL[0])
            {
                //_bufferDataLenght -= 2;
                //Array.Copy(_bufferData, 2, _bufferData, 0, _bufferDataLenght);
                bufferData.RemoveHeadBytes(2);

                if (ShowWarning)
                {
                    LogWarning("Packet end error, find next packet!");
                }
                return true;
            }
            //对数据体进行校验
            byte[] tempdata;
            if (dataLenght != _PACKET_LENGHT && bufferData[11] == 1)
            {
                tempdata = new byte[dataLenght - _PACKET_LENGHT];
                //Array.Copy(_bufferData, 14, tempdata, 0, tempdata.Length);
                bufferData.ReadBytes(14, tempdata, 0, tempdata.Length);
                crc = Util.CreateCRC(tempdata, tempdata.Length);
                if (crc != bufferData[13])
                {
                    //_bufferDataLenght -= dataLenght;
                    //Array.Copy(_bufferData, dataLenght, _bufferData, 0, _bufferDataLenght);
                    bufferData.RemoveHeadBytes(dataLenght);

                    if (ShowWarning)
                    {
                        LogWarning("Data CRC error,remove packet head,find next packet!");
                    }
                    return true;
                }

            }
            //检查通过，从缓存中取出数据，开始进行数据操作
            tempdata = new byte[dataLenght];
            //Array.Copy(_bufferData, 0, tempdata, 0, dataLenght);
            bufferData.ReadBytes(0, tempdata, 0, tempdata.Length);

            //_bufferDataLenght -= dataLenght;
            //Array.Copy(_bufferData, dataLenght, _bufferData, 0, _bufferDataLenght);
            bufferData.RemoveHeadBytes(dataLenght);

            DataPacket dataPacket = Format(tempdata);
            
            socket.LastReceiveTime = DateTime.Now;

            //判断是不是回应包   
           
            if (dataPacket.IsHeart)
            {
                return true;
            }
            else
            {
                if (dataPacket.IsLost)
                {
                    lock (_lokRootObject)
                    {
                        if (dataManager != null)
                        {
                            dataManager.AddData(new DataPacket(dataPacket.PIdValue, false, null, false, this));
                        }
                    }
                }

                //如判断是否为回应空包
                if (dataLenght == _PACKET_LENGHT)
                {
                    lock (_lokRootObject)
                    {
                        if (dataManager != null)
                        {
                            dataManager.AddReceive(dataPacket);
                        }
                    }
                }
                else if (socket.HasReceiveDataHandle)
                {
                    //bool needSend = false;
                    lock (_lokRootObject)
                    {
                        if (dataManager != null)
                        {
                            if (!dataManager.IsReceive(dataPacket.PacketID))
                            {
                                recPacket = dataPacket;
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <returns></returns>
        public override DataPacketBase CreateDataPacket(object packetId, bool lost, byte[] data, bool verify) 
        {
            DataPacket packet = new DataPacket((int)packetId, lost, data, verify, this);
            return packet;
        }

        /// <summary>
        /// 创建socket连接
        /// </summary>
        /// <returns></returns>
        public override ClientSocketBase CreateClientSocket(Socket socket, int maxSendPool=15, int maxLostPool=15,
            HeartManager heartManager = null, bool isServerSocket = false, SocketCertConfig certConfig = null) 
        {
            ClientSocket ret = new ClientSocket(socket, maxSendPool, maxLostPool, heartManager, isServerSocket, this,certConfig);
            return ret;
        }

        

       

        
       
        public override int BufferLength
        {
            get { return 1024; }
        }
    }
}
