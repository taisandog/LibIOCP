using Buffalo.Kernel;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace LibIOCP.DataProtocol
{
    /// <summary>
    /// 连接事件
    /// </summary>
    /// <param name="clientSocket">客户端连接</param>
    public delegate void SocketEvent(ClientSocketBase clientSocket);
    /// <summary>
    /// 数据事件
    /// </summary>
    /// <param name="data">数据</param>
    public delegate void DataEvent(ClientSocketBase socket, DataPacketBase data);

    /// <summary>
    /// 通知事件
    /// </summary>
    public delegate void MessageEvent(ClientSocketBase clientSocket);
    /// <summary>
    /// 一般通知事件
    /// </summary>
    public delegate void NormalMessageHandle(ClientSocketBase clientSocket, string message);
    /// <summary>
    /// 连接处理错误
    /// </summary>
    public delegate void ClientSocketError(ClientSocketBase clientSocket, Exception ex);


    /// <summary>
    /// 数据通讯处理
    /// </summary>
    public abstract partial class ClientSocketBase : IDisposable
    {
        #region 事件

        /// <summary>
        /// 收到数据
        /// </summary>
        private event DataEvent OnReceiveData;

        public void AddReceiveDataHandle(DataEvent receiveData) 
        {
            OnReceiveData += receiveData;
            Receive();
        }
        /// <summary>
        /// 通讯已经关闭
        /// </summary>
        public event MessageEvent OnClose;

        /// <summary>
        /// 通讯错误
        /// </summary>
        public event ClientSocketError OnError;
        /// <summary>
        /// 需要锁的对象
        /// </summary>
        internal object _lokRootObject = new object();
        /// <summary>
        /// 一般通知事件
        /// </summary>
        public event NormalMessageHandle OnMessage;

        //private BlockThreadPool _thdPool = null;
        #endregion
        public void Dispose()
        {
            Close();
        }

        #region 属性
        protected Socket _bindSocket;

        /// <summary>
        /// 绑定的Socket连接
        /// </summary>
        public Socket BindSocket
        {
            get
            {
                return _bindSocket;
            }
        }
        protected DataManager _dataManager;
        /// <summary>
        /// 数据包管理
        /// </summary>
        public DataManager DataManager
        {

            get
            {
                return _dataManager;
            }
        }
        protected INetProtocol _netProtocol;
        /// <summary>
        /// 协议
        /// </summary>
        public INetProtocol NetProtocol
        {
            get
            {
                return _netProtocol;
            }
        }
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool Connected
        {

            get
            {
                return _bindSocket != null && _bindSocket.Connected;
            }
        }
        /// <summary>
        /// 收到的数据缓存区
        /// </summary>
        protected NetByteBuffer _bufferData = new NetByteBuffer(256);
        /// <summary>
        /// 是否连接
        /// </summary>
        public NetByteBuffer BufferData
        {

            get
            {
                return _bufferData;
            }
        }
        ///// <summary>
        ///// WebSocket收到的数据缓存区
        ///// </summary>
        //protected NetByteBuffer _wsBufferData = new NetByteBuffer(1024);
        ///// <summary>
        ///// WebSocket收到的数据缓存区
        ///// </summary>
        //public NetByteBuffer WSBufferData
        //{

        //    get
        //    {
        //        return _wsBufferData;
        //    }
        //}
        ///// <summary>
        ///// 数据缓存区当前存放的数据长度
        ///// </summary>
        //protected int _bufferDataLenght;





        /// <summary>
        /// 连接超时时间
        /// </summary>
        protected int _outTime;
        /// <summary>
        /// 最后收到数据时间
        /// </summary>
        public DateTime LastReceiveTime
        {
            set;
            get;
        }

        /// <summary>
        /// 最后发送数据时间
        /// </summary>
        public DateTime LastSendTime
        {
            set;
            get;
        }

        protected IPEndPoint _remoteIP;
        /// <summary>
        /// 远程IP
        /// </summary>
        public IPEndPoint RemoteIP
        {
            get
            {
                if (_remoteIP == null && _bindSocket != null)
                {
                    _remoteIP = (IPEndPoint)_bindSocket.RemoteEndPoint;
                }
                return _remoteIP;
            }
        }


        /// <summary>
        /// 远程主机IP
        /// </summary>
        protected String _HostIP;
        /// <summary>
        /// 远程主机IP
        /// </summary>
        public String HostIP
        {
            get
            {
                if (_HostIP == null && RemoteIP != null)
                {

                    _HostIP = RemoteIP.Address.ToString();
                }
                return _HostIP;
            }
        }

        protected int _port = -1;
        /// <summary>
        /// 远程主机IP
        /// </summary>
        public int Port
        {
            get
            {
                if (_port < 0 && RemoteIP != null)
                {

                    _port = RemoteIP.Port;
                }
                return _port;
            }
        }
        /// <summary>
        /// 异步接收事件
        /// </summary>
        protected SocketAsyncEventArgs RecevieSocketAsync;
        ///// <summary>
        ///// 异步发送
        ///// </summary>
        //protected SocketAsyncEventArgs SendSocketAsync;

        /// <summary>
        /// 是否正在发送
        /// </summary>
        protected bool IsSend;
        /// <summary>
        /// 心跳管理
        /// </summary>
        protected HeartManager _heartmanager;

        public HeartManager HeartManager 
        {
            get { return _heartmanager; }
        }

        /// <summary>
        /// 本连接ID
        /// </summary>
        protected long _id = 0;
        /// <summary>
        /// 本连接ID
        /// </summary>
        public long Id
        {
            get
            {
                return _id;
            }
        }
        #endregion

        #region 方法
        protected static long _autoincrementId = 0;
        protected static object autoLock = new object();

        private static INetProtocol _defaultAdapter = new DefaultNetAdapter();

        protected virtual INetProtocol CreateDefaultAdapter() 
        {
            return _defaultAdapter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="heartManager"></param>
        public ClientSocketBase(Socket socket, HeartManager heartManager, INetProtocol netProtocol = null)
            : this(socket, 15, 15, heartManager, netProtocol)
        {

        }
        protected ClientSocketBase(Socket socket, int maxSendPool, int maxLostPool,
            HeartManager heartManager = null, INetProtocol netProtocol = null)
        {
            _netProtocol = netProtocol;
            if (_netProtocol == null)
            {
                _netProtocol = CreateDefaultAdapter();
            }
            //_thdPool = new BlockThreadPool(2000);
            _dataManager = new DataManager(maxSendPool, maxLostPool, _netProtocol);
            _bindSocket = socket;
            lock (autoLock)
            {
                _id = _autoincrementId;
                _autoincrementId++;

                if (_netProtocol.ShowLog)
                {
                    _netProtocol.Log("SocketCreate:id=" + _id);
                }

            }
            RecevieSocketAsync = new SocketAsyncEventArgs();
            RecevieSocketAsync.AcceptSocket = _bindSocket;
            int buffLen = _netProtocol.BufferLength;
            RecevieSocketAsync.SetBuffer(new byte[buffLen], 0, buffLen);
            RecevieSocketAsync.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecCompleted);

            
            LastSendTime = DateTime.Now;
            LastReceiveTime = DateTime.Now;
            //SocketCount++;
            
            if (heartManager != null)
            {
                heartManager.AddClient(this);
                _heartmanager = heartManager;
            }
            
        }


        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="dataPacket"></param>
        public virtual void SendPacket(DataPacketBase dataPacket)
        {
            lock (_lokRootObject)
            {
                if (Connected)
                {
                    string err = _dataManager.AddData(dataPacket);
                    if (err != null && OnMessage != null)
                    {
                        OnMessage(this, err);
                    }
                }
            }
            Send();
        }
        /// <summary>
        /// 发送字节数组
        /// </summary>
        /// <param name="data"></param>
        public abstract DataPacketBase Send(byte[] data);
        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="data"></param>
        public abstract DataPacketBase Send(string data);


        private object _lokSend = new object();
        /// <summary>
        /// 发送原始数据
        /// </summary>
        /// <param name="data"></param>
        public virtual DataPacketBase SendRaw(byte[] data) 
        {
            DataPacketBase dp = new DataPacketBase();
            dp.IsRaw = true;
            dp.Data = data;
            SendPacket(dp);
            return dp;
        }
        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="isAsync">是否异步发送</param>
        protected void Send()
        {
            lock (_lokSend)
            {
                if (IsSend)
                {
                    return;
                }
                IsSend = true;
            }
            bool isSync = true;
            DataPacketBase dataPacket = null;
            SocketAsyncEventArgs sendSocketAsync = null;
            try
            {

                if (!Connected)
                {
                    return;
                }


                
                lock (_lokRootObject)
                {
                    dataPacket = _dataManager.GetData();
                }
                if (dataPacket == null)
                {
                    return;
                }
                Socket socket = null;
                
                try
                {
                    byte[] data = null;
                    if (dataPacket.IsRaw) 
                    {
                        data =dataPacket.Data;
                    }
                    else 
                    {
                        data = _netProtocol.ToArray(dataPacket);
                    }

                    //if (_isWebsocketHandShanked)
                    //{
                    //    data = ProtocolDraft10.PackageServerData(data, dataPacket.WebSocketMessageType, dataPacket.WebSocketMask);
                    //}
                    lock (_lokRootObject)
                    {
                        socket = _bindSocket;
                    }
                    sendSocketAsync = new SocketAsyncEventArgs();
                    sendSocketAsync.AcceptSocket = _bindSocket;
                    sendSocketAsync.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompleted);
                    sendSocketAsync.SetBuffer(data, 0, data.Length);

                    sendSocketAsync.UserToken = dataPacket;
                    if (socket != null && Connected)
                    {
                        isSync = socket.SendAsync(sendSocketAsync);
                    }
                    LastSendTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    DoSendFault(dataPacket);
                    if (_netProtocol.ShowError)
                    {
                        _netProtocol.Log("Send Error:" + ex.ToString());
                    }
                }
                finally
                {
                    dataPacket = null;

                    
                    socket = null;
                }


            }
            catch (Exception ex)
            {
                if (_netProtocol.ShowError)
                {
                    _netProtocol.Log("Send Error:" + ex.ToString());
                }
            }
            finally
            {
                IsSend = false;
                if (!isSync) 
                {
                    DoSocketSend(sendSocketAsync);

                }
                sendSocketAsync = null;
            }


        }

        

        /// <summary>
        /// 发送失败
        /// </summary>
        /// <param name="dataPacket"></param>
        private void DoSendFault(DataPacketBase dataPacket)
        {
            if (!dataPacket.IsHeart)
            {
                return;
            }
            lock (_lokRootObject)
            {
                if (Connected && !_dataManager.IsSendPacketFull)
                {
                    string err = _dataManager.AddData(dataPacket);
                    if (err != null && OnMessage != null)
                    {
                        OnMessage(this, err);
                    }
                }
            }



        }


        /// <summary>
        /// 接收
        /// </summary>
        protected void Receive()
        {
            Socket socket = null;

            SocketAsyncEventArgs eventArgs = null;

            if (!Connected)
            {
                return;
            }
            socket = _bindSocket;
            eventArgs = RecevieSocketAsync;

            try
            {
                if (!socket.ReceiveAsync(eventArgs))
                {
                    DoSocketReceive(eventArgs);
                }
            }
            catch (Exception ex)
            {
                if (ShowError)
                {
                    _message.LogError(ex.ToString());
                }
            }
            eventArgs = null;
            socket = null;
        }
        /// <summary>
        /// 检查数据重发
        /// </summary>
        internal void CheckResend(int timeResend)
        {

        }
        protected void OnCompleted(object sender, SocketAsyncEventArgs e)
        {

            //using (SocketAsyncEventArgs ae = e)
            //{
            //    using (DataPacketBase packet = ae.UserToken as DataPacketBase)
            //    {
            //        if (packet != null)
            //        {
            //            packet.SetEvent();
            //        }

                    DoCompleted(sender, e);
                //}
                //EventHandleClean.ClearAllEvents(ae);
            //}

        }
        object _lokBuffdata = new object();
        protected void DoCompleted(object sender, SocketAsyncEventArgs e)
        {

            if (e.SocketError == SocketError.Success)
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        DoSocketReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        DoSocketSend(e);
                        break;
                    case SocketAsyncOperation.Disconnect:
                        DoSocketDisconnect(e);
                        break;
                    default:
                        break;
                }
            }
            else
            {

                HandleClose("Client error:" + e.SocketError);
                Close();
                return;
            }

        }
        /// <summary>
        /// 处理断开
        /// </summary>
        /// <param name="e"></param>
        private void DoSocketDisconnect(SocketAsyncEventArgs e)
        {
            HandleClose("Client Disconnect");
            Close();
            return;
        }
        /// <summary>
        /// 处理发送
        /// </summary>
        /// <param name="e"></param>
        private void DoSocketSend(SocketAsyncEventArgs e)
        {
            if (_netProtocol.ShowLog)
            {
                _netProtocol.Log("Send Success");
            }

            LastSendTime = DateTime.Now;
            IsSend = false;
            Send();
        }

        //protected bool _isWebsocketHandShanked = false;

        /// <summary>
        /// 处理接收
        /// </summary>
        /// <param name="e"></param>
        private void DoSocketReceive(SocketAsyncEventArgs e)
        {
            



            if (e.BytesTransferred <= 0 || e.Buffer == null)
            {
                Close();
                HandleClose("Client Close");
                return;
            }
            if (!Connected)
            {
                return;
            }

            DateTime recDate = DateTime.Now;
            Socket socket = _bindSocket;
            try
            {


                lock (_lokBuffdata)
                {
                    NetByteBuffer bufferData = _bufferData;
                    if (bufferData == null)
                    {
                        return;
                    }
                    bufferData.AppendBytes(e.Buffer, e.Offset, e.BytesTransferred);

                    LastReceiveTime = DateTime.Now;
                    if (socket.Available == 0)
                    {

                        DataPacketBase dataPacket = null;

                       
                        while (_netProtocol.IsDataLegal(out dataPacket, this))
                        {
                            OnReceiveData(this, dataPacket);
                            //DoDataPacket(dataPacket, recDate);
                        }



                    }

                }
                
                Receive();
            }
            catch (Exception ex)
            {
                if (OnError != null)
                {
                    OnError(this, ex);
                }
                HandleClose("Client error:" + e.SocketError);
                Close();
            }
        }

       

        ///// <summary>
        ///// 过滤加载到缓存的内容
        ///// </summary>
        ///// <param name="e">socket</param>
        ///// <param name="bufferData">数据</param>
        ///// <param name="dataPacket">组建出来的Data包</param>
        ///// <returns>是否有数据包返回</returns>
        //protected virtual bool FilterAppendBuffer(SocketAsyncEventArgs e, NetByteBuffer bufferData, ref DataPacketBase dataPacket)
        //{
        //    bufferData.AppendBytes(e.Buffer, e.Offset, e.BytesTransferred);
        //    return false;
        //}
        
        


        public virtual void DoDataPacket(DataPacketBase dataPacket, DateTime recDate) 
        {
            if (dataPacket != null && OnReceiveData != null)
            {
                lock (dataPacket)
                {
                    OnReceiveData(this, dataPacket);
                }
            }
        }

        /// <summary>
        /// 接收信息
        /// </summary>
        /// <param name="dataPacket"></param>
        protected void RunReceiveData(DataPacketBase dataPacket) 
        {
            Thread th=new Thread(new ParameterizedThreadStart(DoReceiveData));
            th.Start(dataPacket);
        }

        /// <summary>
        /// 处理接收数据
        /// </summary>
        /// <param name="args"></param>
        private void DoReceiveData(object args) 
        {
            using (DataPacketBase dataPacket = args as DataPacketBase)
            {
                if (dataPacket == null)
                {
                    return;
                }

                lock (dataPacket)
                {
                    OnReceiveData(this, dataPacket);
                }
            }
        }


        /// <summary>
        /// 是否有接收信息的触发
        /// </summary>
        public bool HasReceiveDataHandle
        {
            get
            {
                return OnReceiveData != null;
            }
        }
        protected void OnRecCompleted(object sender, SocketAsyncEventArgs e)
        {
            DoSocketReceive(e);
            //DoCompleted(sender, e);

        }

        /// <summary>
        /// 关闭
        /// </summary>
        public virtual void Close()
        {
            //Connected = false;
            try
            {
                if (_heartmanager != null)
                {
                    _heartmanager.RemoveSocket(this);
                }
            }
            catch { }
            Socket socket = null;
            SocketAsyncEventArgs eventArgs = null;

            lock (_lokRootObject)
            {

                if (_bindSocket != null)
                {
                    socket = _bindSocket;
                    
                }
                _bindSocket = null;
                if (_dataManager != null)
                {
                    try
                    {
                        _dataManager.Close();

                    }
                    catch (Exception)
                    {
                    }
                    
                }
                _dataManager = null;
                if (RecevieSocketAsync != null)
                {
                    eventArgs = RecevieSocketAsync;
                   
                    RecevieSocketAsync = null;
                }
            }
            if (socket != null)
            {
                try
                {

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Dispose();

                }
                catch { }
            }
            socket = null;
            if (eventArgs != null)
            {
                try
                {

                    EventHandleClean.ClearAllEvents(eventArgs);
                    eventArgs.Dispose();

                }
                catch (Exception)
                {
                }  
                
            }
            eventArgs = null;
            if (_bufferData != null)
            {
                lock (_lokBuffdata)
                {
                    _bufferData.Dispose();
                    _bufferData = null;
                }
            }
            _message = null;
            
            _remoteIP = null;
            EventHandleClean.ClearAllEvents(this);
        }

        internal void HandleClose(String str)
        {
            if (ShowWarning)
            {
                _message.LogWarning(HostIP + ":" + str);
            }
            if (OnClose != null)
            {
                OnClose(this);
            }


        }
        /// <summary>
        /// 发送跳包
        /// </summary>
        public virtual void SendHeard()
        {
            DataPacketBase data = _netProtocol.CreateDataPacket(0, false, null,false);
            data.IsHeart = true;
            SendPacket(data);
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
        #endregion
    }
}
