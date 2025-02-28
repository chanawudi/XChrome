#region copyright
/**
// --------------------------------------------------------------------------------
// 文件名：MyDb.cs
// 作者：刹那 https://x.com/chanawudi
// 公司：https://x.com/chanawudi
// 更新日期：2025，2，27，13:54
// 版权所有 © Your Company. 保留所有权利。
// --------------------------------------------------------------------------------
*/
#endregion
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace XChrome.cs.db
{
    public class MyDb
    {
        public static SqlSugarClient DB
        {
            
            get
            {
                string basePath = Directory.GetCurrentDirectory() + "\\db\\xchrome_db.db";
                var db = new SqlSugarClient(new ConnectionConfig()
                {
                    ConnectionString = "DataSource="+ basePath,//必填, 数据库连接字符串
                    DbType = DbType.Sqlite,         //必填, 数据库类型
                    IsAutoCloseConnection = false,     //默认false, 时候知道关闭数据库连接, 设置为true无需使用using或者Close操作
                    InitKeyType = InitKeyType.SystemTable    //默认SystemTable, 字段信息读取, 如：该属性是不是主键，是不是标识列等等信息
                });

                return db;
            }

        }


        /// <summary>
        /// 初始化，完成一些表格建立，表格更新等
        /// </summary>
        public static async Task ini()
        {
            string basePath = Directory.GetCurrentDirectory() + "\\db";
            if (!Directory.Exists(basePath)) { 
                Directory.CreateDirectory(basePath);
            }


            var db=MyDb.DB;
            //创建group
            string sql = "CREATE TABLE IF NOT EXISTS [Group] (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, remark TEXT, createTime DATETIME DEFAULT CURRENT_TIMESTAMP);";
            try {await db.Ado.ExecuteCommandAsync(sql);}catch (Exception ex) {}

            //插入一条默认分组
            sql = "insert into [Group](id,name,remark) values(1,'默认分组','默认分组不能删除')";
            try { await db.Ado.ExecuteCommandAsync(sql); } catch (Exception ex) { }

            //创建chrome
            sql = @"CREATE TABLE IF NOT EXISTS [Chrome](id INTEGER PRIMARY KEY AUTOINCREMENT,
name TEXT, userAgent TEXT,proxy TEXT,proxyText TEXT,groupId INTEGER,createDate DATETIME DEFAULT CURRENT_TIMESTAMP,doDate DATETIME DEFAULT CURRENT_TIMESTAMP,cookie TEXT,remark TEXT,tags TEXT)";
            try { await db.Ado.ExecuteCommandAsync(sql); } catch (Exception ex) { }
            //chrome 增加其他环境参数
            sql = "ALTER TABLE [Chrome] ADD COLUMN envs TEXT";
            try { await db.Ado.ExecuteCommandAsync(sql); } catch (Exception ex) { }

            //chrome 增加其他环境参数 extensions
            sql = "ALTER TABLE [Chrome] ADD COLUMN extensions TEXT";
            try { await db.Ado.ExecuteCommandAsync(sql); } catch (Exception ex) { }


            db.Close();
        }
    }
}