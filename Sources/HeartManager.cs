using LibIOCP.DataProtocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LibIOCP
{

    /// <summary>
    /// 心跳,超时,数据重发管理
    /// </summary>
    public class HeartManager
    {
       

        /// <summary>
        /// 超时时间
        /// </summary>
        public int TimeOut
        {
            private set;
            get;
        }
        /// <summary>
        /// 心跳时间
        /// </summary>
        public int TimeHeart
        {
            private set;
            get;
        }
        /// <summary>
        /// 数据重发间隔
        /// </summary>
        public int TimeResend
        {
            private set;
            get;
        }
        /// <summary>
        /// 消息类
        /// </summary>
        private IConnectMessage _message;
        /// <summary>
        /// 消息
        /// </summary>
        public IConnectMessage Message
        {
            get
            {
                return _message;
            }
        }
        private AutoResetEvent _threadHandle;
        public ConcurrentDictionary<ClientSocketBase, bool> Clients
        {
            get
            {
                return _clients;
            }
        }
        private bool _needSendheart=true;
        /// <summary>
        /// 是否需要主动发心跳
        /// </summary>
        public bool NeedSendheart 
        {
            get { return _needSendheart; }
            set { _needSendheart = value; }
        }
        /// <summary>
        /// 客户端数
        /// </summary>
        //public List<ClientSocket> Clients;
        private ConcurrentDictionary<ClientSocketBase, bool> _clients = new ConcurrentDictionary<ClientSocketBase, bool>();

        /// <summary>
        /// 检查线程
        /// </summary>
        private Thread CheckThread;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeOut">超时时间</param>
        /// <param name="timeHreat">心跳时间</param>
        /// <param name="timeResend">重发间隔</param>
        /// <param name="message">日志输出器</param>
        public HeartManager(int timeOut, int timeHreat, int timeResend, IConnectMessage message)
        {
            //_clients = new ConcurrentDictionary<ClientSocket, bool>();
           
            TimeResend = timeResend;
            TimeOut = timeOut;
            TimeHeart = timeHreat;
            _message = message;
            
        }
       
        /// <summary>
        /// 连接是否存在
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool ExistsClient(ClientSocketBase client)
        {
            return _clients.ContainsKey(client);
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        /// <param name="client"></param>
        public bool AddClient(ClientSocketBase client)
        {
            if (!_running) 
            {
                return false;
            }
            bool ret=_clients.TryAdd(client,true);
            CheckRunning();
            return ret;
        }
        /// <summary>
        /// 检测线程是否运行
        /// </summary>
        public void CheckRunning()
        {
            if (!_running)
            {
                return;
            }
            if (CheckThread == null || !CheckThread.IsAlive)
            {
                if (Util.HasShowWarning(_message))
                {
                    _message.LogWarning("Start Heart");
                }
                StartHeart();
            }
        }
        /// <summary>
        /// 开始心跳管理
        /// </summary>
        public void StartHeart()
        {
            StopHeart();
            _running = true;
            CheckThread = new Thread(new ThreadStart(DoRun));
            
            CheckThread.Start();

        }
        private bool _running = false;
        /// <summary>
        /// 关闭心跳管理
        /// </summary>
        public void StopHeart()
        {
            _running = false;
            if (CheckThread == null)
            {
                return;
            }
            try
            {
                if (_threadHandle!=null && !_threadHandle.WaitOne(5000)) 
                {
                    try
                    {
                        CheckThread.Abort();
                        Thread.Sleep(100);
                    }
                    catch { }
                    
                }
                _threadHandle = null;
                CheckThread = null;
                
            }
            catch (Exception ex)
            {
                //if (_message.ShowWarning)
                //{
                //    _message.LogWarning("关闭心跳管理EX:" + ex);
                //}
            }
            

        }
        /// <summary>
        /// 删除心跳管理的连接
        /// </summary>
        /// <param name="socket"></param>
        public bool RemoveSocket(ClientSocketBase socket)
        {
            bool ret = false;
            return _clients.TryRemove(socket, out ret);
        }
        private void DoRun()
        {
            int pertimeResend = 0;

            pertimeResend = TimeResend / 2;
            if (pertimeResend <= 0) 
            {
                pertimeResend = TimeHeart / 2;
            }
            if (pertimeResend <= 0)
            {
                pertimeResend = TimeOut / 2;
            }
            if (pertimeResend <= 0)
            {
                return;
            }
            try
            {
                _threadHandle = new AutoResetEvent(false);
                _threadHandle.Reset();
                ClientSocketBase connection = null;
                Queue<ClientSocketBase> lstremovelist =null;
                Queue<ClientSocketBase> lstcloselist = null;
                DateTime dtLast=DateTime.MinValue;
                DateTime nowDate = DateTime.MinValue;
                while (_running)
                {
                    nowDate = DateTime.Now;
                    if (_clients.Count > 0 && nowDate.Subtract(dtLast).TotalMilliseconds >= pertimeResend)
                    {
                        lstremovelist = new Queue<ClientSocketBase>();
                        lstcloselist = new Queue<ClientSocketBase>();
                        try
                        {
                            foreach (KeyValuePair<ClientSocketBase, bool> kvp in _clients)
                            {
                                if (!_running)
                                {
                                    break;
                                }
                                connection = kvp.Key;
                                if (connection == null)
                                {
                                    continue;
                                }
                                if (!connection.Connected)//空连接
                                {
                                    lstremovelist.Enqueue(connection);
                                    continue;
                                }
                                if (_needSendheart && TimeHeart > 0 && nowDate.Subtract(connection.LastSendTime).TotalMilliseconds > TimeHeart)
                                {
                                    connection.SendHeard();
                                    continue;
                                }
                                else if (TimeOut > 0 && nowDate.Subtract(connection.LastReceiveTime).TotalMilliseconds > TimeOut)
                                {
                                    lstcloselist.Enqueue(connection);
                                    continue;
                                }

                                try
                                {
                                    if (TimeResend > 0)
                                    {
                                        connection.DataManager.CheckResend(TimeResend);
                                    }
                                }
                                catch { }

                            }
                            bool ret = false;
                            foreach (ClientSocketBase conn in lstremovelist)
                            {
                                if (!_running)
                                {
                                    break;
                                }
                                conn.HandleClose("Destroy invalid connection:" + conn.HostIP);//通知断开
                                _clients.TryRemove(conn, out ret);

                            }
                            if (Util.HasShowWarning(_message) && lstremovelist.Count > 0)
                            {
                                if (_message.ShowWarning)
                                {
                                    _message.LogWarning("Clear destroyed connection:" + lstremovelist.Count);
                                }
                            }

                            int tid = 0;
                            foreach (ClientSocketBase conn in lstcloselist)
                            {
                                if (!_running)
                                {
                                    break;
                                }
                                conn.HandleClose("Connection timedout:" + conn.HostIP);//通知断开
                                conn.Close();
                                _clients.TryRemove(conn, out ret);
                                if (Util.HasShowWarning(_message))
                                {
                                    string key = conn.SocketKey;
                                    tid = 0;
                                    if (!string.IsNullOrWhiteSpace(key))
                                    {
                                        try
                                        {
                                            tid = Convert.ToInt32(conn.SocketKey, 16);
                                        }
                                        catch { }
                                    }
                                    if (_message.ShowWarning)
                                    {
                                        StringBuilder sb = new StringBuilder(50);
                                        sb.Append("Connection timedout:ID:");
                                        sb.Append(tid);
                                        sb.Append(",IP:");
                                        sb.Append(conn.HostIP);
                                        _message.LogWarning(sb.ToString());
                                    }
                                }

                            }
                            


                        }
                        catch { }
                        finally
                        {
                            dtLast = nowDate;
                            lstremovelist.Clear();
                            lstremovelist = null;
                            lstcloselist.Clear();
                            lstcloselist = null;
                        }
                        
                        
                    }
                    Thread.Sleep(200);
                }
            }
            finally 
            {
                _threadHandle.Set();
            }
            CheckThread = null;
        }
    }


}
