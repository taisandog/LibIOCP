using LibIOCP.DataProtocol;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LibIOCP.DataProtocol
{
    /// <summary>
    /// 发送的数据包
    /// </summary>
    public class DataPacket:DataPacketBase
    {
        #region 属性
        
        

        /// <summary>
        /// 是否验证数据体
        /// </summary>
        public bool IsVerify
        {
            private set;
            get;
        }

        /// <summary>
        /// 数据包CRC校验
        /// </summary>
        public byte PacketCRC
        {
            private set;
            get;
        }

        

        /// <summary>
        /// 数据体CRC校验
        /// </summary>
        public byte DataCRC
        {
            private set;
            get;
        }


        #endregion

        #region 方法
        ///// <summary>
        ///// 构造，生成一个空包，用来回应收到,空包不验证丢失和CRC
        ///// </summary>
        ///// <param name="packetID">包编号</param>
        ///// <param name="hasResetEvent">是否有阻塞函数</param>
        //public DataPacket(int packetID, bool hasResetEvent, INetProtocol netProtocol)
        //{
        //    _netProtocol = netProtocol;
        //    if (_netProtocol == null)
        //    {
        //        _netProtocol = new DefaultNetAdapter();
        //    }
        //    this.PacketID = packetID;
        //    this.Length = _netProtocol.PACKET_LENGHT;
        //    this.IsLost = false;
        //    this.IsVerify = false;
        //    this.Data = null;
        //    this.DataCRC = 0;
        //    this.PacketCRC = Util.CreateCRC(GetVeryftPakcetData());
        //    if (hasResetEvent)
        //    {
        //        _event = new ManualResetEvent(false);
        //        ResetEvent();
        //    }

        //}
        /// <summary>
        /// 构造，生成一个数据包，用来发送
        /// </summary>
        /// <param name="packetID">包编号</param>
        /// <param name="lost">是否验证丢失</param>
        /// <param name="verify">是否验证数据体</param>
        /// <param name="data">数据体</param>
        public DataPacket(int packetID, bool lost,  byte[] data,bool verify, INetProtocol netProtocol)
        {
            _netProtocol = netProtocol;
            if (_netProtocol == null)
            {
                _netProtocol = new DefaultNetAdapter();
            }
            if (packetID > 0)
            {
                this.PacketID = packetID.ToString();
            }

            this.IsLost = lost;
            this.IsVerify = verify;
           

            if (data != null)
            {
                this.Data = data;
                this.Length = data.Length + _netProtocol.PACKET_LENGHT;
                if (IsVerify)
                {
                    this.DataCRC = Util.CreateCRC(Data);
                }
            }
            else //空包
            {
                this.Data = null;
                this.Length = _netProtocol.PACKET_LENGHT;
                this.DataCRC = 0;
            }
            this.PacketCRC = Util.CreateCRC(GetVeryftPakcetData());
            //if (hasResetEvent)
            //{
            //    _event = new ManualResetEvent(false);
            //    ResetEvent();
            //}
        }
        
        /// <summary>
        /// 整型方式的包ID
        /// </summary>
        public int PIdValue 
        {
            get 
            {
               
                int pid = 0;
                object oid = PacketID;
                if (oid != null)
                {
                    pid = Convert.ToInt32(oid);
                }
                return pid;
            }
        }
        
        

        /// <summary>
        /// 返回此包需要校验的数据,0-3长度，4-7包编号，8丢失，9验证，10数据体CRC，
        /// </summary>
        /// <returns></returns>
        private byte[] GetVeryftPakcetData()
        {
            byte[] result = new byte[11];
            byte[] temp = Util.IntToByte4(Length);
            Array.Copy(temp, 0, result, 0, temp.Length);
            
            temp = Util.IntToByte4(PIdValue);
            Array.Copy(temp, 0, result, 4, temp.Length);
            result[8] = (byte)(IsLost ? 1 : 0);
            result[9] = (byte)(IsVerify ? 1 : 0);
            result[10] = DataCRC;
            return result;
        }
        

        
        
        
        #endregion
    }
}
