﻿using System;
using System.Messaging;
using OpenETaxBill.SDK.Data.Collection;
using OpenETaxBill.SDK.Queue;
using OpenETaxBill.SDK.Security;

namespace OpenETaxBill.Engine.Responsor
{
    public class Host : IDisposable
    {
        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
        private OpenETaxBill.Channel.Interface.IResponsor m_iresponsor = null;
        private OpenETaxBill.Channel.Interface.IResponsor IResponsor
        {
            get
            {
                if (m_iresponsor == null)
                    m_iresponsor = new OpenETaxBill.Channel.Interface.IResponsor();

                return m_iresponsor;
            }
        }

        private OpenETaxBill.Engine.Library.UAppHelper m_appHelper = null;
        public OpenETaxBill.Engine.Library.UAppHelper UAppHelper
        {
            get
            {
                if (m_appHelper == null)
                    m_appHelper = new OpenETaxBill.Engine.Library.UAppHelper(IResponsor.Manager);

                return m_appHelper;
            }
        }

        private OpenETaxBill.SDK.Queue.QWriter p_qwriter = null;
        private OpenETaxBill.SDK.Queue.QWriter QWriter
        {
            get
            {
                if (p_qwriter == null)
                    p_qwriter = new OpenETaxBill.SDK.Queue.QWriter();

                return p_qwriter;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
        private OpenETaxBill.SDK.Communication.WcfServer m_wcf_service = null;
        private OpenETaxBill.SDK.Communication.WcfServer WcfService
        {
            get
            {
                if (m_wcf_service == null)
                {
                    IResponsor.Proxy.SetServerPortSharing();

                    m_wcf_service = new OpenETaxBill.SDK.Communication.WcfServer(
                        typeof(ResponseService), typeof(IResponseService),
                        IResponsor.Proxy.BindingNames,
                        IResponsor.Proxy.ProductName,
                        IResponsor.Proxy.IpAddress,
                        IResponsor.Proxy.ServicePorts,
                        IResponsor.Proxy.BehaviorPort,
                        IResponsor.Proxy.IsPortSharing,
                        IResponsor.Proxy.SharingPort
                        )
                    {
                        ReceiveTimeout = TimeSpan.FromDays(7),
                        SendTimeout = TimeSpan.FromDays(7)
                    };

                    m_wcf_service.Start();

                    m_wcf_service.ServerHost.Opened += ServerHost_Opened;
                    m_wcf_service.ServerHost.Closed += ServerHost_Closed;
                    m_wcf_service.ServerHost.Faulted += ServerHost_Faulted;
                }

                return m_wcf_service;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
        private void ServerHost_Opened(object sender, EventArgs e)
        {
            IResponsor.WriteDebug("server channel opened....");
        }

        private void ServerHost_Closed(object sender, EventArgs e)
        {
            IResponsor.WriteDebug("server channel closed....");
        }

        private void ServerHost_Faulted(object sender, EventArgs e)
        {
            IResponsor.WriteDebug("server channel faulted....");
            Stop();
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            IResponsor.WriteDebug(String.Format("server address {0}...", WcfService.WcfAddress));

            try
            {
                WcfService.ServerHost.Open();

                QReader.QReadEvents += QReader_QReadEvents;
                QReader.QRemoveEvents += QReader_QRemoveEvents;

                //CPermit.QStart();
                QWriter.QStart(IResponsor.Manager);
            }
            catch (Exception ex)
            {
                ELogger.SNG.WriteLog(ex);
            }
        }

        void QReader_QRemoveEvents(object sender, ReceiveCompletedEventArgs e)
        {
            QMessage _qmessage = e.Message.Body as QMessage;
            IResponsor.WriteDebug(String.Format("remove: {0}, {1}, {2}, {3}, {4}", _qmessage.ProductId, _qmessage.Command, _qmessage.ProductId, _qmessage.IpAddress, _qmessage.Message));

            //if (_qmessage.ProductId == CPermit.QSlave.ProductId)
            //{
            //    CPermit.Stop();
            //}
        }

        void QReader_QReadEvents(object sender, ReceiveCompletedEventArgs e)
        {
            QMessage _qmessage = e.Message.Body as QMessage;

            QClient _client = new QClient(_qmessage);
            string _command = _qmessage.Command.ToLower();

            string _message = _qmessage.Message;
            if (_qmessage.UsePackage == true)
                _message = Serialization.SNG.ReadPackage<string>(_qmessage.Package);

            if (Environment.UserInteractive == true)
            {
                if (e.Message.Label != "CFG")
                {
                    IResponsor.WriteDebug(String.Format("READ: '{0}', {1}, {2}, {3}, {4}, {5}, {6}", e.Message.Label, _qmessage.Command, _qmessage.ProductId, _qmessage.pVersion, _qmessage.IpAddress, _qmessage.HostName, _message));
                }
                else
                {
                    var _dbps = _qmessage.Package.ToParameters();
                    string _companyId = _dbps["companyId"].ToString();
                    string _corporateId = _dbps["corporateId"].ToString();
                    string _productId = _dbps["productId"].ToString();
                    string _pVersion = _dbps["pVersion"].ToString();
                    string _appkey = _dbps["appkey"].ToString();
                    string _appvalue = _dbps["appValue"].ToString();

                    IResponsor.WriteDebug(String.Format("READ: '{0}', {1}, {2}, {3}, {4}, {5}, {6}", e.Message.Label, _companyId, _corporateId, _productId, _pVersion, _appkey, _appvalue));
                }
            }

            if (e.Message.Label == "CMD")         // command
            {
                string _product = _qmessage.ProductId;

                if (_product != IResponsor.Manager.ProductId)
                {
                    if (_command == "pong")
                    {
                        QWriter.SetPingFlag(new QClient(_qmessage));
                    }
                    else if (_command == "signin")
                    {
                        QWriter.AddAgency(IResponsor.Manager, _qmessage);
                    }
                    else if (_command == "signout")
                    {
                        QWriter.RemoveAgency(IResponsor.Manager, new Guid(_message));
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            IResponsor.WriteDebug("Stop");

            try
            {
                QWriter.QStop(IResponsor.Manager);
                //CPermit.QStop();

                if (m_wcf_service != null)
                {
                    m_wcf_service.Stop();
                    m_wcf_service = null;
                }
            }
            catch (Exception ex)
            {
                ELogger.SNG.WriteLog(ex);
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_iresponsor != null)
                {
                    m_iresponsor.Dispose();
                    m_iresponsor = null;
                }
                if (m_wcf_service != null)
                {
                    m_wcf_service.Dispose();
                    m_wcf_service = null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        ~Host()
        {
            Dispose(false);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
    }
}