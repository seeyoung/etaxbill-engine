﻿/*
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.If not, see<http://www.gnu.org/licenses/>.
*/

using System;
using System.Configuration;
using System.Net.Mail;
using OdinSoft.SDK.Configuration;

namespace OpenETaxBill.Engine.Reporter
{
    /// <summary>
    /// 
    /// </summary>
    public class EConfig : IDisposable
    {
        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
        private readonly static Lazy<EConfig> m_lzyHelper = new Lazy<EConfig>(() => new EConfig());

        /// <summary>
        /// 
        /// </summary>
        public static EConfig SNG
        {
            get
            {
                return m_lzyHelper.Value;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
        private OpenETaxBill.Channel.Interface.IReporter m_ireporter = null;
        private OpenETaxBill.Channel.Interface.IReporter IReporter
        {
            get
            {
                if (m_ireporter == null)
                    m_ireporter = new OpenETaxBill.Channel.Interface.IReporter();

                return m_ireporter;
            }
        }

        private OpenETaxBill.Engine.Library.UAppHelper m_appHelper = null;
        public OpenETaxBill.Engine.Library.UAppHelper UAppHelper
        {
            get
            {
                if (m_appHelper == null)
                    m_appHelper = new OpenETaxBill.Engine.Library.UAppHelper(IReporter.Manager);

                return m_appHelper;
            }
        }

        private string GetAppValue(string p_appkey, string p_default = "")
        {
            if (String.IsNullOrEmpty(p_default) == true)
                p_default = ConfigurationManager.AppSettings[p_appkey];

            return RegHelper.SNG.GetServer(IReporter.Manager.CategoryId, IReporter.Manager.ProductId, p_appkey, p_default);
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
        private int? m_remind_history_term;

        /// <summary>
        /// 
        /// </summary>
        public int RemindHistoryTerm
        {
            get
            {
                if (m_remind_history_term == null)
                    m_remind_history_term = Convert.ToInt32(GetAppValue("RemindHistoryTerm"));

                return m_remind_history_term.Value;
            }
        }

        private int? m_rangeOfOrderMonth;

        /// <summary>
        /// 
        /// </summary>
        public int RangeOfOrderMonth
        {
            get
            {
                if (m_rangeOfOrderMonth == null)
                    m_rangeOfOrderMonth = Convert.ToInt32(GetAppValue("RangeOfOrderMonth"));

                return m_rangeOfOrderMonth.Value;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
        //
        //-------------------------------------------------------------------------------------------------------------------------
        #region IDisposable Members

        /// <summary>
        ///
        /// </summary>
        private bool IsDisposed
        {
            get;
            set;
        }

        /// <summary>
        /// Dispose of the backing store before garbage collection.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the backing store before garbage collection.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> if disposing; otherwise, <see langword="false"/>.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources. 
                    if (m_ireporter != null)
                    {
                        m_ireporter.Dispose();
                        m_ireporter = null;
                    }
                }

                // Dispose unmanaged resources. 

                // Note disposing has been done. 
                IsDisposed = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        ~EConfig()
        {
            Dispose(false);
        }

        #endregion

        //-------------------------------------------------------------------------------------------------------------------------
        // 
        //-------------------------------------------------------------------------------------------------------------------------
    }
}