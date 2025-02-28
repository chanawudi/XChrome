#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：Chrome.cs
// 作者：刹那 https://x.com/chanawudi
// 公司：https://x.com/chanawudi
// 更新日期：2025，2，27，13:54
// 版权所有 © Your Company. 保留所有权利。
// --------------------------------------------------------------------------------
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XChrome.cs.db
{
    public class Chrome
    {
        /// <summary>
        /// Chrome
        /// </summary>
        public Chrome()
        {
        }

        private System.Int64 _id;
        /// <summary>
        /// id
        /// </summary>
        public System.Int64 id { get { return this._id; } set { this._id = value; } }

        private System.String _name;
        /// <summary>
        /// name
        /// </summary>
        public System.String name { get { return this._name; } set { this._name = value; } }

        private System.String _userAgent;
        /// <summary>
        /// userAgent
        /// </summary>
        public System.String userAgent { get { return this._userAgent; } set { this._userAgent = value; } }

        private System.String _proxy;
        /// <summary>
        /// proxy
        /// </summary>
        public System.String proxy { get { return this._proxy; } set { this._proxy = value; } }

        private System.String _proxyText;
        /// <summary>
        /// proxyText
        /// </summary>
        public System.String proxyText { get { return this._proxyText; } set { this._proxyText = value; } }

        private System.Int64? _groupId;
        /// <summary>
        /// groupId
        /// </summary>
        public System.Int64? groupId { get { return this._groupId; } set { this._groupId = value; } }

        private System.DateTime? _createDate;
        /// <summary>
        /// createDate
        /// </summary>
        public System.DateTime? createDate { get { return this._createDate; } set { this._createDate = value; } }

        private System.DateTime? _doDate;
        /// <summary>
        /// doDate
        /// </summary>
        public System.DateTime? doDate { get { return this._doDate; } set { this._doDate = value; } }

        private System.String _cookie;
        /// <summary>
        /// cookie
        /// </summary>
        public System.String cookie { get { return this._cookie; } set { this._cookie = value; } }

        private System.String _remark;
        /// <summary>
        /// remark
        /// </summary>
        public System.String remark { get { return this._remark; } set { this._remark = value; } }

        private System.String _tags;
        /// <summary>
        /// remark
        /// </summary>
        public System.String tags { get { return this._tags; } set { this._tags = value; } }

        private System.String _envs="";
        /// <summary>
        /// remark
        /// </summary>
        public System.String envs { get { return this._envs; } set { this._envs = value; } }

        private System.String _extensions = "";
        /// <summary>
        /// remark
        /// </summary>
        public System.String extensions { get { return this._extensions; } set { this._extensions = value; } }

        //
    }
}
