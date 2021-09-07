using System;
using System.Linq;
using System.Net.Http;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 審核
    /// </summary>
    public class MGProductCodeCK
    {

        /// <summary>
        /// 商品申請審核
        /// </summary>
        /// <param name="mb"></param>
        /// <returns></returns>
        public object PApproveRun(Member mb, String pkey) =>
        new DBServer().open((cn, db) =>
        {
            var getP = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1", new { key = pkey }).FirstOrDefault();
            if (getP != null)
            {
                getP.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")} get approve)";
                getP.P_ck = "run";
                getP.MB_appCK = true;
                new DBServer(write: true).open((cn2, db2) =>
                {
                    db2.UpdateStr<Product>(new
                    {
                        getP.P_key,
                        getP.P_ck,
                        getP.MB_appCK,
                        getP.P_mark,
                    });
                });
                return new {error=(int)SystemErrorCode.Null,data= getP.P_ck };
            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.notExistID);
            }
        });

        /// <summary>
        /// 取消送審
        /// </summary>
        /// <param name="mb"></param>
        /// <returns></returns>
        public object PApproveRunCancel(Member mb, String pkey) =>
        new DBServer().open((cn, db) =>
        {
            var getP = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1", new { key = pkey }).FirstOrDefault();
            if (getP != null)
            {
                getP.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")} cancel approve)";
                getP.P_ck = "";
                getP.MB_appCK = true;
                new DBServer(write: true).open((cn2, db2) =>
                {
                    db2.UpdateStr<Product>(new
                    {
                        getP.P_key,
                        getP.P_ck,
                        getP.MB_appCK,
                        getP.P_mark,
                    });
                });
                return new { error = (int)SystemErrorCode.Null, data = getP.P_ck };
            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.notExistID);
            }
        });

        /// <summary>
        /// 商品審核通過/審核失敗
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="fail">審核失敗</param>
        /// <returns></returns>
        public object PApprove(Member mb, String pkey, bool fail = false, string content = "") =>
        new DBServer().open((cn, db) =>
        {
            var getP = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key AND P_ck = 'run' LIMIT 0,1", new { key = pkey }).FirstOrDefault();
            if (getP != null)
            {
                getP.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")} save approve)";
                getP.MB_appCK = (int)MBLevel.MG == mb.MB_level;
                if (getP.P_ck == "run")
                {
                    if (!fail)
                    {//通過審核
                        getP.P_ck = $"{mb.MB_uid}_{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                    }
                    else
                    {//審核失敗
                        getP.P_ck = "runfail";
                        getP.MB_appCK = true;
                    }

                    getP.P_approveMark = content;


                }

                new DBServer(write: true).open((cn2, db2) =>
                {

                    if (!fail)
                    {//通過審核
                     //審核log
                        db2.InsertAutoKey<LogCk>(new LogCk() { LC_level = ((mb.MB_level == (int)MBLevel.Edit) ? (int)logTp.repar : ((!getP.MB_appCK) ? (int)logTp.sysModify : (int)logTp.sysRepar)), LC_log = $"客戶商品審核通過{getP.P_key},signature:{mb.MB_Account}", LC_id = getP.P_key, LC_tb = (int)logDocTB.Product, LC_date = new pub().unixDateTimeNow() });
                        if (!getP.MB_appCK && (int)MBLevel.systemMG == mb.MB_level)
                        {
                            //排除無需審核任務
                            var delObj = cn.Query<LogCk>("SELECT LC_key FROM LogCk WHERE LC_id = @getid AND LC_tb = @tb AND LC_Tag = FALSE AND LC_del = FALSE AND LC_ck = FALSE AND LC_level = 0", new { getid = getP.P_key, tb = (int)logDocTB.Product }).ToList();
                            if (delObj.Count() > 0)
                            {
                                cn2.Execute("UPDATE LogCk SET LC_ck=TRUE,LC_ckDate=@date,LC_ac=@ac WHERE LC_key IN @ary", new { ary = delObj.Select(n => n.LC_key).ToList(), ac = mb.MB_Account, date = new pub().unixDateTimeNow() });
                            }
                        }
                        getP.MB_appCK = !((int)MBLevel.Edit == mb.MB_level);
                    }

                    db2.UpdateStr<Product>(new
                    {
                        getP.P_key,
                        getP.MB_appCK,
                        getP.P_mark,
                        getP.P_ck,
                        getP.P_approveMark
                    });
                });
                return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson(getP) };
            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.notExistID);
            }
        });
    }
}
