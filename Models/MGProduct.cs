using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    public class MGProduct
    {
        /// <summary>
        /// 起用 關閉 商品
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="pkey"></param>
        /// <returns></returns>
        public object ProductOnOff(Member mb, String P_key) =>
       new DBServer().open((cn, db) =>
       {
           var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key  LIMIT 0,1", new
           {
               P_key
           }).FirstOrDefault();//判斷是否真實存在

           //因異動需申請審核
           if (obj != null)
           {

               var getRevObj = JObj.EnumList<ProductType>();
               var valKey = getRevObj.Where(a => a.value == obj.P_type).FirstOrDefault().key;//取得Key String
               obj.P_type = getRevObj.Where(n => n.key == $"{((obj.P_type > -1) ? "Stop" : "")}{((obj.P_type > -1) ? valKey : valKey.Substring(4))}").FirstOrDefault().value;//轉換為 禁用模式
               obj.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(Product 客戶{((obj.P_type > -1) ? "起用" : "禁用")})";
               new DBServer(write: true).open((cn2, db2) =>
               {
                   if (new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                   {
                       //審核log
                       db2.InsertAutoKey<LogCk>(new LogCk() { LC_level = ((mb.MB_level == (int)MBLevel.Edit) ? (int)logTp.repar : ((!obj.MB_appCK) ? (int)logTp.sysModify : (int)logTp.sysRepar)), LC_log = $"商品-{((obj.P_type > 0) ? "起用" : "關閉")}{obj.P_key},signature:{mb.MB_Account}", LC_id = obj.P_key, LC_tb = (int)logDocTB.Product, LC_date = new pub().unixDateTimeNow() });
                       if (!obj.MB_appCK && (int)MBLevel.systemMG == mb.MB_level)
                       {
                           //排除無需審核任務
                           var delObj = cn.Query<LogCk>("SELECT LC_key FROM LogCk WHERE LC_id = @getid AND LC_tb = @tb AND LC_Tag = FALSE AND LC_del = FALSE AND LC_ck = FALSE AND LC_level = 0", new { getid = obj.P_key, tb = (int)logDocTB.Product }).ToList();
                           if (delObj.Count() > 0)
                           {
                               cn2.Execute("UPDATE LogCk SET LC_ck=TRUE,LC_ckDate=@date,LC_ac=@ac WHERE LC_key IN @ary", new { ary = delObj.Select(n => n.LC_key).ToList(), ac = mb.MB_Account, date = new pub().unixDateTimeNow() });
                           }
                       }
                   }
                   obj.MB_appCK = (int)MBLevel.Edit != mb.MB_level;
                   db2.UpdateStr<Product>(new
                   {
                       obj.P_key,
                       obj.MB_appCK,
                       obj.P_type
                   });
               });
               return new { error = (int)SystemErrorCode.Null, type = obj.P_type, mark = obj.P_mark,appck= obj.MB_appCK  };
           }
           else
           {
               return JFormat.returnFail((int)SystemErrorCode.prdocutNotExist);
           }
       }, e => JFormat.returnFail());

        #region 一般商品 order
        /// <summary>
        /// 商品排序往上移
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="key">主要切換order對像</param>
        /// <param name="key2">對換對像</param>
        /// <returns></returns>
        public object ProductPre(Member mb, String key, String key2) =>
        new DBServer().open((cn, db) =>
        {
            var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key", new
            {
                P_key = key
            }).FirstOrDefault();//判斷是否真實存在

            var minOder = cn.Query<Int32>("SELECT P_order FROM Product WHERE P_class = @cs  ORDER BY P_order ASC LIMIT 0,1", new { cs = obj.P_class }).FirstOrDefault();
            if (obj.P_order > minOder)
            {
                var obj2 = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key AND P_class = @cs", new
                {
                    P_key = key2,
                    cs = obj.P_class
                }).FirstOrDefault();//取得上一層oder 號

                if (obj2 != null)
                {
                    new DBServer(write: true).open((cn2, db2) =>
                    {
                        cn2.Execute("UPDATE Product SET P_order = @order2 WHERE P_key != 'empty' AND P_order = @order;", new { order = obj2.P_order, order2 = obj.P_order });//修改商品排序
                        obj.P_order = obj2.P_order;
                        obj.P_mark = $"[{mb.MB_Account}:oder pre]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                        db2.UpdateStr<Product>(new { obj.P_key, obj.P_order, obj.P_mark });
                    });
                }
            }
            return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson<Product>(obj) };
        }, e => JFormat.returnFail());

        /// <summary>
        /// 商品排序往下移
        /// </summary>
        /// <param name="key">主要切換order對像</param>
        /// <param name="key2">對換對像</param>
        /// <returns></returns>
        public object ProductNext(Member mb, String key, String key2) =>
      new DBServer().open((cn, db) =>
      {
          var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key", new
          {
              P_key = key
          }).FirstOrDefault();//判斷是否真實存在

          var minOder = cn.Query<Int32>("SELECT P_order FROM Product WHERE P_class = @cs  ORDER BY P_order DESC LIMIT 0,1", new { cs = obj.P_class }).FirstOrDefault();
          if (obj.P_order < minOder)
          {
              var obj2 = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key AND P_class = @cs", new
              {
                  P_key = key2,
                  cs = obj.P_class
              }).FirstOrDefault();//取得上一層oder 號

              if (obj2 != null)
              {
                  new DBServer(write: true).open((cn2, db2) =>
                  {
                      cn2.Execute("UPDATE Product SET P_order = @order2 WHERE P_key != 'empty' AND P_order = @order;", new { order = obj2.P_order, order2 = obj.P_order });//修改商品排序
                      obj.P_order = obj2.P_order;
                      obj.P_mark = $"[{mb.MB_Account}:oder next]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                      db2.UpdateStr<Product>(new { obj.P_key, obj.P_order, obj.P_mark });
                  });
              }
          }
          return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson<Product>(obj) };
      }, e => JFormat.returnFail());
        #endregion

        #region 商品描述
        /// <summary>
        /// 商品簡述-edit(語系)
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="content"></param>
        /// <param name="langNu"></param>
        /// <returns></returns>
        public object PDEdit(Member mb, String pkey, String content, Int32 langNu) =>
        new DBServer().open((cn, db) =>
        {
            var getP = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1", new { key = pkey }).FirstOrDefault();
            if (getP != null)
            {
                if ((getP.P_ck == "chiefadmin" && new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                //系統管理者建立才可以編緝
                || (getP.P_ck == "" && new List<int>() { (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1 && getP.MB_uid == mb.MB_uid)
                //非系統管理者建立 需取消送審才可以編緝
                )
                {
                    string bestPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "product");
                    if (!Directory.Exists(bestPath))
                    {//建置商品簡述放置地
                        Directory.CreateDirectory(bestPath);
                        Thread.Sleep(50);
                    }
                    JObj.WriteTxt(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content)), Path.Combine(bestPath, $"{pkey}_lang{langNu}.json"), root: false);
                    getP.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")} save Doucment)";

                    new DBServer(write: true).open((cn2, db2) =>
                    {
                        if (new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                        {
                            //審核log
                            db2.InsertAutoKey<LogCk>(new LogCk() { LC_level = ((mb.MB_level == (int)MBLevel.Edit) ? (int)logTp.repar : ((!getP.MB_appCK) ? (int)logTp.sysModify : (int)logTp.sysRepar)), LC_log = $"商品-描述{getP.P_key},signature:{mb.MB_Account}", LC_id = getP.P_key, LC_tb = (int)logDocTB.Product, LC_date = new pub().unixDateTimeNow() });
                            if (!getP.MB_appCK && (int)MBLevel.systemMG == mb.MB_level)
                            {
                                //排除無需審核任務
                                var delObj = cn.Query<LogCk>("SELECT LC_key FROM LogCk WHERE LC_id = @getid AND LC_tb = @tb AND LC_Tag = FALSE AND LC_del = FALSE AND LC_ck = FALSE AND LC_level = 0", new { getid = getP.P_key, tb = (int)logDocTB.Product }).ToList();
                                if (delObj.Count() > 0)
                                {
                                    cn2.Execute("UPDATE LogCk SET LC_ck=TRUE,LC_ckDate=@date,LC_ac=@ac WHERE LC_key IN @ary", new { ary = delObj.Select(n => n.LC_key).ToList(), ac = mb.MB_Account, date = new pub().unixDateTimeNow() });
                                }
                            }
                        }

                        getP.MB_appCK = !((int)MBLevel.Edit == mb.MB_level);

                        if (getP.P_type >= 0)
                        {//轉停用不顯示
                            var getRevObj = JObj.EnumList<ProductType>();
                            var valKey = getRevObj.Where(a => a.value == getP.P_type).FirstOrDefault().key;//取得Key String
                            getP.P_type = getRevObj.Where(n => n.key == $"Stop{valKey}").FirstOrDefault().value;//轉換為 禁用模式
                        }

                        db2.UpdateStr<Product>(new
                        {
                            getP.P_key,
                            getP.MB_appCK,
                            getP.P_mark,
                            getP.P_type
                        });
                    });
                    return new {error=(int)SystemErrorCode.Null,data=JObj.DBJson(getP) };
                }
                else
                {
                    return JFormat.returnFail((int)SystemErrorCode.limit);
                }
            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.notExistID);
            }
        });

        /// <summary>
        /// 商品簡述-read(語系)
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="langNu"></param>
        /// <returns></returns>
        public object PDRead(String pkey, Int32 langNu)
        {

            string bestPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "product", $"{pkey}_lang{langNu}.json");
            String data = "";
            if (File.Exists(bestPath))
            {
                data = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(JObj.ReadTxt(bestPath, false)));

            }

            return new { error = (int)SystemErrorCode.Null, data = data };

        }
        #endregion

        /// <summary>
        /// 商品管理-編緝
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="pobj"></param>
        /// <param name="del">刪除模式</param>
        /// <param name="inCount">增加庫存</param>
        /// <param name="deCount">減少庫存</param>
        /// <param name="countLimit">限製購買數量 model</param>
        /// <param name="pStore">通貨設定 model</param>
        /// <param name="lang">語系設定 model</param>
        /// <param name="ybe">video youtube url model</param>
        /// <returns></returns>
        public object ProdcutEdit(Member mb, Product pobj, bool del = false,bool inCount=false,bool deCount=false,bool countLimit = false,bool pStore = false, bool lang = false,bool ybe=false) =>
         new DBServer().open((cn, db) =>
         {
             if (pobj.P_key == "-insert-")
             {//insert
                 var objOder = cn.Query<Product>("SELECT * FROM Product WHERE P_class = 2 ORDER BY P_order ASC LIMIT 0,1").FirstOrDefault();//目前商品 oder 最小號

                 pobj.P_Name = JObj.ser(new List<String>() { $"新品{DateTime.UtcNow.ToString("ddHHmmss")}" });
                 pobj.P_unit = JObj.ser(new List<String>() { "(單位)" });
                 int order = 0;
                 if (objOder != null)
                 {//排序號
                     order = objOder.P_order;
                 }
                 else
                 {
                     var getNewOrder = cn.Query<Product>("SELECT * FROM Product ORDER BY P_order DESC LIMIT 0,1").FirstOrDefault();
                     if (getNewOrder != null)
                     {
                         order = getNewOrder.P_order + 1;
                     }
                     else
                     {
                         order = 1;
                     }
                 }

                 pobj.P_order = order;//oder 至first
                 pobj.P_date = new pub().unixDateTimeNow();
                 pobj.P_store = (int)ProductStore.line;
                 pobj.P_type = (int)ProductType.StopLimit;
                 pobj.P_class =(int)ProductClass.product;
                 pobj.P_mark = $"[{mb.MB_Account}:insert]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                 pobj.MB_uid = mb.MB_uid;

                 int usCount = 0;
                 int point = 0;
                 //確認 insert
                 bool ckInsert = false;
                 if (new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                 {//系統管理者
                     pobj.P_ck = "chiefadmin";
                     ckInsert = true;
                 }
                 if (mb.MB_level == (int)MBLevel.pay)
                 {//VIP
                     ckInsert = true;
                 }
                 else if (mb.MB_level != (int)MBLevel.pay)
                 {//其它用戶扣點
                     var getMB = cn.Query<Member>("SELECT * FROM Member WHERE MB_Account = @ac LIMIT 0,1", new { ac = mb.MB_Account }).FirstOrDefault();
                     if (mb.MB_level == (int)MBLevel.MG && getMB.MB_Count >= 29000)
                     {
                         getMB.MB_Count -= 29000;
                         getMB.MB_usCount += 29000;
                         new DBServer(write: true).open((cn2, db2) =>
                         {
                             db2.UpdateStr<Member>(new { mb.MB_Account, getMB.MB_Count, getMB.MB_usCount });

                         });
                         ckInsert = true;
                         usCount = getMB.MB_usCount;
                         point = getMB.MB_Count;
                     }
                 }

                 if (ckInsert)
                 {
                     new DBServer(write: true).open((cn2, db2) =>
                     {
                         if (new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                         {
                             //審核log
                             db2.InsertAutoKey<LogCk>(new LogCk() { LC_level = ((mb.MB_level == (int)MBLevel.Edit) ? (int)logTp.repar : (int)logTp.sysRepar), LC_log = $"商品-建置{pobj.P_key},signature:{mb.MB_Account}", LC_id = pobj.P_key, LC_tb = (int)logDocTB.Product, LC_date = new pub().unixDateTimeNow() });
                             if (!pobj.MB_appCK && (int)MBLevel.systemMG == mb.MB_level)
                             {
                                 //排除無需審核任務
                                 var delObj = cn.Query<LogCk>("SELECT LC_key FROM LogCk WHERE LC_id = @getid AND LC_tb = @tb AND LC_Tag = FALSE AND LC_del = FALSE AND LC_ck = FALSE AND LC_level = 0", new { getid = pobj.P_key, tb = (int)logDocTB.Product }).ToList();
                                 if (delObj.Count() > 0)
                                 {
                                     cn2.Execute("UPDATE LogCk SET LC_ck=TRUE,LC_ckDate=@date,LC_ac=@ac WHERE LC_key IN @ary", new { ary = delObj.Select(n => n.LC_key).ToList(), ac = mb.MB_Account, date = new pub().unixDateTimeNow() });
                                 }
                             }
                         }
                         pobj.MB_appCK = !((int)MBLevel.Edit == mb.MB_level);
                         cn2.Execute("UPDATE Product SET P_order = P_order + 1 WHERE P_key != 'empty' AND P_order >= @order;", new { order = pobj.P_order });//修改商品排序
                         pobj = db2.InsertAutoKey(pobj);
                     });

                     var getpctkey = cn.Query<ProductClassName>("SELECT * FROM ProductClassName WHERE PC_key = @pckey LIMIT 0,1", new { pckey = pobj.PC_key }).FirstOrDefault();//二次分類別名
                     if (getpctkey != null)
                     {//取得首次分類 key
                         pobj.pctkey = getpctkey.PCT_key;
                     }

                     if (mb.MB_level == (int)MBLevel.MG)
                     {
                         return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson(pobj), point = point, repoint = usCount };

                     }
                     else
                     {
                         return new
                         {
                             error = (int)SystemErrorCode.Null,
                             data = JObj.DBJson(pobj),
                         };
                     }
                 }
                 else
                 {//點數不足限制
                     return JFormat.returnFail((int)SystemErrorCode.notpointError);
                 }
             }
             else
             {
                 var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key LIMIT 0,1",
                 new
                 {
                     pobj.P_key
                 }).FirstOrDefault();//判斷是否真實存在
                 if (obj != null)
                 {
                     if (((mb.MB_level == (int)MBLevel.MG) ? mb.MB_payDate > new pub().unixDateTimeNow() : true))
                     {//判斷支付時間(店主限制編緝)

                         if ((obj.P_ck == "chiefadmin" && new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                         //系統管理者建立才可以編緝
                         || (obj.P_ck == "" && new List<int>() { (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1 && obj.MB_uid == mb.MB_uid ) || inCount || deCount || countLimit || pStore
                         //非系統管理者建立 需取消送審才可以編緝
                         )
                         {
                             //因異動需申請審核
                             if (!del && !inCount && !deCount && !countLimit && !pStore && !lang && !ybe)
                             {//編緝

                                 if (new List<Int32>() { (int)ProductClass.app, (int)ProductClass.pApp }.IndexOf(obj.P_class) == -1)
                                 {//系統App不可更動
                                     obj.P_Name = pobj.P_Name;

                                     #region 如已存在單據則不能修改 庫存狀態
                                     //偵聽是否存在單據
                                     bool productSet = true;

                                     //是否需偵聽庫存異動
                                     bool checkLimit = ((obj.P_type != pobj.P_type) ? !((obj.P_type == (int)ProductType.NotLimit && pobj.P_type == (int)ProductType.StopNotLimit) || (obj.P_type == (int)ProductType.Limit && pobj.P_type == (int)ProductType.StopLimit)) : false);
                                     if (checkLimit)
                                     {
                                         productSet = !cn.Query<bool>("SELECT EXISTS(SELECT * FROM PayOptions WHERE P_key = @P_key LIMIT 0,1)", new { obj.P_key }).FirstOrDefault();
                                     }
                                     #endregion
                                     if (JObj.EnumList<ProductType>().Where(n => n.value == pobj.P_type).FirstOrDefault() != null && productSet)
                                     {//防止異常修改
                                         obj.P_type = pobj.P_type;
                                     }
                                     else
                                     {
                                         return JFormat.returnFail((int)((!checkLimit || productSet) ? SystemErrorCode.inputFormat : SystemErrorCode.ExistData));
                                     }
                                     obj.P_unit = pobj.P_unit;
                                 }
                                 obj.P_fee = Math.Abs(pobj.P_fee);
                                 obj.PC_key = pobj.PC_key;
                                 if (obj.P_class == (int)ProductClass.product)
                                 {//只有實體商品才能設定容積
                                     obj.P_shunit = pobj.P_shunit;
                                 }

                                 #region 防誤觸設定錯誤金額
                                 if (obj.P_type >= 0)
                                 {//轉停用不顯示
                                     var getRevObj = JObj.EnumList<ProductType>();
                                     var valKey = getRevObj.Where(a => a.value == obj.P_type).FirstOrDefault().key;//取得Key String
                                     obj.P_type = getRevObj.Where(n => n.key == $"Stop{valKey}").FirstOrDefault().value;//轉換為 禁用模式
                                 }
                                 #endregion

                                 obj.P_cash = Math.Abs(pobj.P_cash);
                                 obj.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(Product Edit)";

                                 new DBServer(write: true).open((cn2, db2) =>
                                 {
                                     if (new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                                     {
                                         //審核log
                                         db2.InsertAutoKey<LogCk>(new LogCk() { LC_level = ((mb.MB_level == (int)MBLevel.Edit) ? (int)logTp.repar : ((!obj.MB_appCK) ? (int)logTp.sysModify : (int)logTp.sysRepar)), LC_log = $"商品-Edit{pobj.P_key},signature:{mb.MB_Account}", LC_id = obj.P_key, LC_tb = (int)logDocTB.Product, LC_date = new pub().unixDateTimeNow() });
                                         if (!obj.MB_appCK && (int)MBLevel.systemMG == mb.MB_level)
                                         {
                                             //排除無需審核任務
                                             var delObj = cn.Query<LogCk>("SELECT LC_key FROM LogCk WHERE LC_id = @getid AND LC_tb = @tb AND LC_Tag = FALSE AND LC_del = FALSE AND LC_ck = FALSE AND LC_level = 0", new { getid = obj.P_key, tb = (int)logDocTB.Product }).ToList();
                                             if (delObj.Count() > 0)
                                             {
                                                 cn2.Execute("UPDATE LogCk SET LC_ck=TRUE,LC_ckDate=@date,LC_ac=@ac WHERE LC_key IN @ary", new { ary = delObj.Select(n => n.LC_key).ToList(), ac = mb.MB_Account, date = new pub().unixDateTimeNow() });
                                             }
                                         }
                                     }
                                     obj.MB_appCK = !((int)MBLevel.Edit == mb.MB_level);
                                     db2.UpdateStr<Product>(new
                                     {
                                         obj.P_Name,
                                         obj.P_key,
                                         obj.PC_key,
                                         obj.P_unit,
                                         obj.P_type,
                                         obj.P_cash,
                                         obj.P_mark,
                                         obj.P_shunit,
                                         obj.P_fee,
                                         obj.MB_appCK
                                     });
                                 });

                                 var getpctkey = cn.Query<ProductClassName>("SELECT * FROM ProductClassName WHERE PC_key = @pckey LIMIT 0,1", new { pckey = obj.PC_key }).FirstOrDefault();//二次分類別名
                                 if (getpctkey != null)
                                 {//取得首次分類 key
                                     obj.pctkey = getpctkey.PCT_key;
                                 }
                                 return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson(obj) };
                             }
                             else if (ybe)
                             {
                                 obj.P_mark = $"[{mb.MB_Account}:video add youtube]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                                 obj.P_ybe = pobj.P_ybe;
                                 if (obj.P_type > 0)
                                 {
                                     var getRevObj = JObj.EnumList<ProductType>();
                                     var valKey = getRevObj.Where(a => a.value == obj.P_type).FirstOrDefault().key;//取得Key String
                                     obj.P_type = getRevObj.Where(n => n.key == $"Stop{valKey}").FirstOrDefault().value;//轉換為 禁用模式
                                 }
                                 new DBServer(write: true).open((cn2, db2) =>
                                 {
                                     db2.UpdateStr<Product>(new { obj.P_key, obj.P_ybe, obj.P_mark, obj.P_type });
                                 });
                                 return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson(obj) };
                             }
                             else if (lang)
                             {//語系支持設定
                                 obj.P_mark = $"[{mb.MB_Account}:lang choose]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                                 obj.P_lang = JObj.ser(JObj.des<List<int>>(pobj.P_lang));//格式驗證
                                 if (obj.P_type > 0)
                                 {
                                     var getRevObj = JObj.EnumList<ProductType>();
                                     var valKey = getRevObj.Where(a => a.value == obj.P_type).FirstOrDefault().key;//取得Key String
                                     obj.P_type = getRevObj.Where(n => n.key == $"Stop{valKey}").FirstOrDefault().value;//轉換為 禁用模式
                                 }
                                 new DBServer(write: true).open((cn2, db2) =>
                                 {
                                     db2.UpdateStr<Product>(new { obj.P_key, obj.P_lang, obj.P_mark, obj.P_type });
                                 });
                                 return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson(obj) };
                             }
                             else if (pStore)
                             {//通貨設定
                                 if (JObj.EnumList<ProductStore>().Where(n => n.value == pobj.P_store).Any() && new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                                 {//檢測是否合法分類所屬(本公司才能進行修改)
                                     obj.P_store = pobj.P_store;
                                     new DBServer(write: true).open((cn2, db2) =>
                                     {
                                         obj.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(通貨設定{JObj.EnumList<ProductStore>().Where(n => n.value == pobj.P_store).FirstOrDefault().Enum.ToString()})";

                                         db2.UpdateStr<Product>(new { obj.P_store, obj.P_key, obj.P_mark });
                                     });
                                 }
                                 return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson<Product>(obj) };
                             }
                             else if (countLimit)
                             {//限制購買數量
                                 if (pobj.P_countLimit >= 0)
                                 {
                                     new DBServer(write: true).open((cn2, db2) =>
                                     {
                                         obj.P_countLimit = pobj.P_countLimit;
                                         obj.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(購買數量限制設定{obj.P_countLimit})";

                                         db2.UpdateStr<Product>(new { obj.P_countLimit, obj.P_key, obj.P_mark });
                                     });
                                 }
                                 return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson<Product>(obj) };
                             }
                             else if (inCount)
                             {//增加庫存
                                 new DBServer(write: true).open((cn2, db2) =>
                                 {
                                     obj.P_Count += pobj.P_Count;
                                     obj.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(庫存增加{pobj.P_Count})";
                                     cn2.Execute("UPDATE Product SET P_Count = P_Count + @count,P_mark=@P_mark  WHERE P_key = @key", new { key = obj.P_key, count = pobj.P_Count, obj.P_mark });

                                 });
                                 return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson<Product>(obj) };
                             }
                             else if (deCount)
                             {//減少庫存
                                 new DBServer(write: true).open((cn2, db2) =>
                                 {
                                     obj.P_Count -= pobj.P_Count;
                                     obj.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(庫存減少{pobj.P_Count})";
                                     cn2.Execute("UPDATE Product SET P_Count = P_Count - @count,P_mark=@P_mark  WHERE P_key = @key", new { key = obj.P_key, count = pobj.P_Count, obj.P_mark });

                                 });
                                 return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson<Product>(obj) };
                             }
                             else
                             {//刪除
                                 if (obj.P_class != (int)ProductClass.app && obj.P_class != (int)ProductClass.pApp)
                                 {//系統App不可更動

                                     if (obj != null)
                                     {

                                         bool productSet = true;//商品是否被取用
                                         using (var useExist = cn.QueryMultiple(
                                         @"SELECT EXISTS(SELECT * FROM PayOptions WHERE P_key = @P_key LIMIT 0,1) as ck;
SELECT EXISTS(SELECT * FROM usApp WHERE P_key = @P_key LIMIT 0,1) as ck;", new
                                         {
                                             obj.P_key
                                         }))
                                         {
                                             productSet = !useExist.Read<bool>().FirstOrDefault() && !useExist.Read<bool>().FirstOrDefault();
                                         };

                                         if (productSet)
                                         {
                                             return new DBServer(write: true).open((cn2, db2) =>
                                             {
                                                 cn2.Execute("UPDATE Product SET P_order = P_order - 1 WHERE P_key != 'empty' AND P_order > @order;"//一般商品排序
                                                 + (((obj.P_GiftOrder != -1)) ? "UPDATE Product SET P_GiftOrder = P_GiftOrder - 1 WHERE P_key != 'empty' AND P_GiftOrder > @giftorder;" : "")//推薦排序
                                                 , new { order = obj.P_order, giftorder = obj.P_GiftOrder });//修改商品排序

                                                 db2.DeleteStr<Product>(obj);

                                                 for (Int32 nu = 0; nu <= 36; nu++)
                                                 {
                                                     string bestPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "product", $"{obj.P_key}_lang{nu}.json");
                                                     if (File.Exists(bestPath))
                                                     {//刪除描述
                                                         File.Delete(bestPath);
                                                     }
                                                 }
                                                 string bestPathImg = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "product", $"{obj.P_key}.json");
                                                 if (File.Exists(bestPathImg))
                                                 {
                                                     obj.imgAry = JObj.readFile<List<string>>(bestPathImg);

                                                     foreach (var n in obj.imgAry)
                                                     {//移除實際圖片
                                                         String nowExist = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "product", n);
                                                         if (File.Exists(nowExist))
                                                         {
                                                             File.Delete(nowExist);
                                                         }
                                                     }

                                                     File.Delete(bestPathImg);
                                                 }

                                                 if (new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                                                 {
                                                     //審核log
                                                     db2.InsertAutoKey<LogCk>(new LogCk() { LC_level = ((mb.MB_level == (int)MBLevel.Edit) ? (int)logTp.repar : ((!obj.MB_appCK) ? (int)logTp.sysModify : (int)logTp.sysRepar)), LC_log = $"商品-刪除{pobj.P_key},signature:{mb.MB_Account}", LC_id = pobj.P_key, LC_tb = (int)logDocTB.Product, LC_date = new pub().unixDateTimeNow() });
                                                     if (!obj.MB_appCK && (int)MBLevel.systemMG == mb.MB_level)
                                                     {
                                                         //排除無需審核任務
                                                         var delObj = cn.Query<LogCk>("SELECT LC_key FROM LogCk WHERE LC_id = @getid AND LC_tb = @tb AND LC_Tag = FALSE AND LC_del = FALSE AND LC_ck = FALSE AND LC_level = 0", new { getid = obj.P_key, tb = (int)logDocTB.Product }).ToList();
                                                         if (delObj.Count() > 0)
                                                         {
                                                             cn2.Execute("UPDATE LogCk SET LC_ck=TRUE,LC_ckDate=@date,LC_ac=@ac WHERE LC_key IN @ary", new { ary = delObj.Select(n => n.LC_key).ToList(), ac = mb.MB_Account, date = new pub().unixDateTimeNow() });
                                                         }
                                                     }
                                                 }
                                                 obj.MB_appCK = !((int)MBLevel.Edit == mb.MB_level);

                                                 if (mb.MB_level == (int)MBLevel.MG)
                                                 {//其它用戶還點
                                                     var getMB = cn.Query<Member>("SELECT * FROM Member WHERE MB_Account = @ac LIMIT 0,1", new { ac = mb.MB_Account }).FirstOrDefault();
                                                     getMB.MB_Count += 25000 + obj.imgAry.Count() * 6000;
                                                     getMB.MB_usCount -= (25000 + obj.imgAry.Count() * 6000);

                                                     db2.UpdateStr<Member>(new { mb.MB_Account, getMB.MB_Count, getMB.MB_usCount });

                                                     return new
                                                     {
                                                         error = 1,
                                                         type = obj.P_type,
                                                         key = obj.P_key,
                                                         point = getMB.MB_Count,
                                                         repoint = getMB.MB_usCount
                                                     };
                                                 }
                                                 else
                                                 {
                                                     return new { error = (int)SystemErrorCode.Null, type = obj.P_type, key = obj.P_key };
                                                 }
                                             });
                                         }
                                         else
                                         {//被使用擇不能刪除
                                             return JFormat.returnFail((int)SystemErrorCode.prdocutUS);
                                         }

                                     }
                                     else
                                     {
                                         return JFormat.returnFail((int)SystemErrorCode.prdocutNotExist);
                                     }
                                 }
                                 else
                                 {

                                     return JFormat.returnFail((int)SystemErrorCode.limit);
                                 }
                             }
                         }
                         else
                         {
                             return JFormat.returnFail((int)SystemErrorCode.limit);
                         }
                     }
                     else
                     {//店主時間阻擋
                         return JFormat.returnFail((int)SystemErrorCode.paytimeError);
                     }
                 }
                 else
                 {
                     return JFormat.returnFail((int)SystemErrorCode.prdocutNotExist);
                 }

             }
         }, e => JFormat.returnFail());


        /// <summary>
        /// Product 審核異動通過
        /// </summary>
        /// <param name="key">log key</param>
        /// <returns></returns>
        public object ProductAppck(Member MB, String key) =>
         new DBServer().open((cn, db) =>
         {
             return new DBServer(write: true).open((cn2, db2) =>
             {
                 var logObj = cn.Query<LogCk>("SELECT * FROM LogCk WHERE LC_key = @key  LIMIT 0,1", new { key }).FirstOrDefault();
                 if (logObj.LC_ckDate == 0 && !logObj.LC_del)
                 {
                     logObj.LC_Tag = true;
                     logObj.LC_ac = MB.MB_Account;
                     logObj.LC_ckDate = new pub().unixDateTimeNow();
                     db2.UpdateStr<LogCk>(new { logObj.LC_key, logObj.LC_Tag, logObj.LC_ac, logObj.LC_ckDate });

                     //排除無需審核任務
                     var delObj = cn.Query<LogCk>("SELECT LC_key FROM LogCk WHERE LC_id = @getid AND LC_tb = @tb AND LC_Tag = FALSE AND LC_del = FALSE AND LC_ck = FALSE AND LC_level = 0", new { getid = logObj.LC_id, tb = logObj.LC_tb }).Select(n => n.LC_key).ToList();
                     if (delObj.Count() > 0)
                     {
                         cn2.Execute("UPDATE LogCk SET LC_del=TRUE WHERE LC_key IN @ary", new { ary = delObj });
                     }


                     var getP = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1",
                        new
                        {
                            key = logObj.LC_id
                        }).FirstOrDefault();

                     getP.MB_appCK = true;

                     var getEvent = db2.InsertAutoKey<LogCk>(new LogCk() { LC_level = (int)logTp.sys, LC_log = $"Product:{getP.P_key},審核/釋放,signature:{MB.MB_Account}", LC_id = getP.P_key, LC_tb = (int)logDocTB.Product, LC_date = new pub().unixDateTimeNow() });
                     db2.UpdateStr<Product>(new
                     {
                         getP.P_key,
                         getP.MB_appCK
                     });
                     return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson(logObj), newdata = JObj.DBJson(getEvent), del = delObj };

                 }
                 else
                 {
                     return JFormat.returnFail((int)SystemErrorCode.ExistData);
                 }
             });
         }, e => JFormat.returnFail());
    }
}
