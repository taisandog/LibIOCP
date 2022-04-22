using LibIOCP.DataProtocol;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LibIOCP.DataProtocol
{
    public class FastDataPacket : DataPacketBase
    {
        public object EmptyPacketId
        {
            get
            {
                return 0;
            }
        }

        public int PacketIDValue
        {
            get 
            {
                return (int)PacketID;
            }
        }

        /// <summary>
        /// 构造，生成一个数据包，用来发送
        /// </summary>
        /// <param name="packetID">包编号</param>
        /// <param name="lost">是否验证丢失</param>
        /// <param name="verify">是否验证数据体</param>
        /// <param name="data">数据体</param>
        public FastDataPacket(int packetID,  byte[] data,bool isLost,   INetProtocol netProtocol)
        {
            _netProtocol = netProtocol;
            if (_netProtocol == null)
            {
                _netProtocol = new FastNetAdapter();
            }
            this.PacketID = packetID;
            
            this.IsLost = isLost;
            //this.IsVerify = verify;


            if (data != null)
            {
                this.Data = data;
                this.Length = data.Length + _netProtocol.PACKET_LENGHT;
                
            }
            else //空包
            {
                this.Data = null;
                this.Length = _netProtocol.PACKET_LENGHT;
                
            }
            
            //if (hasResetEvent)
            //{
            //    _event = new ManualResetEvent(false);
            //    ResetEvent();
            //}
        }


       
    }
}
