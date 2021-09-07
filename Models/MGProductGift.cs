using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 商品推薦
    /// </summary>
    public class MGProductGift
    {
        /// <summary>
        /// 放入商品推薦或取消
        /// </summary>
        /// <param name="pkeyAry">欲加入 key</param>
        /// <returns></returns>
        public object ProductGiftCk(Member mb, List<String> pkeyAry) =>
      new DBServer(write: true).open((cn, db) =>
      {
          var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key IN @ary", new
          {
              ary = pkeyAry
          }).ToList();//真實存在
          var minOder = cn.Query<Int32>("SELECT P_GiftOrder FROM Product WHERE P_GiftOrder > -1  ORDER BY P_GiftOrder ASC LIMIT 0,1").FirstOrDefault();//目前儲存內容物

          var objIncrease = obj.Where(n => n.P_GiftOrder == -1).ToList();
          if (objIncrease.Count > 0)
          {//新增
              if (minOder > 0)
              {//取目前非分類最大編號
                  cn.Execute("UPDATE Product SET P_GiftOrder = P_GiftOrder + @newOrder WHERE P_key != 'empty' AND P_GiftOrder >-1;", new { newOrder = objIncrease.Count() });//修改商品排序
              }

              Int32 catchOder = 0;
              foreach (var n in objIncrease)
              {
                  catchOder++;
                  n.P_GiftOrder = catchOder;//取編號
                  n.P_mark = $"[{mb.MB_Account}:check true gift oder]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
              }
          }
          else
          {
              var objDecrease = obj.Where(n => n.P_GiftOrder > -1).FirstOrDefault();
              if (objDecrease != null)
              {//移除
                  if (minOder > 0)
                  {//取目前非分類最大編號
                      cn.Execute("UPDATE Product SET P_GiftOrder = P_GiftOrder - 1 WHERE P_key != 'empty' AND P_GiftOrder > @order;", new { order = objDecrease.P_GiftOrder });//修改商品排序
                  }
                  objDecrease.P_GiftOrder = -1;
                  objDecrease.P_mark = $"[{mb.MB_Account}:check false gift oder]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
              }
          }

          db.UpdateList(obj);

          return new { error = 1, data = JObj.DBJson(obj) };
      }, e => JFormat.returnFail());

        /// <summary>
        /// 商品排序往上移
        /// </summary>
        /// <param name="key">主要切換order對像</param>
        /// <param name="key2">對換對像</param>
        /// <returns></returns>
        public object ProductGiftPre(Member mb, String key, String key2) =>
       new DBServer().open((cn, db) =>
       {
           var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key AND P_GiftOrder >-1  LIMIT 0,1", new
           {
               P_key = key
           }).FirstOrDefault();//判斷是否真實存在

           var minOder = cn.Query<Int32>("SELECT P_GiftOrder FROM Product WHERE P_GiftOrder >-1  ORDER BY P_GiftOrder ASC LIMIT 0,1").FirstOrDefault();
           if (obj.P_GiftOrder > minOder)
           {
               var obj2 = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key AND P_GiftOrder >-1  LIMIT 0,1 ", new
               {
                   P_key = key2
               }).FirstOrDefault();//取得上一層oder 號

               if (obj2 != null)
               {
                   new DBServer(write: true).open((cn2, db2) =>
                   {
                       cn2.Execute("UPDATE Product SET P_GiftOrder = @order2 WHERE P_key != 'empty' AND P_GiftOrder = @order;", new { order = obj2.P_GiftOrder, order2 = obj.P_GiftOrder });//修改商品排序
                       obj.P_GiftOrder = obj2.P_GiftOrder;
                       obj.P_mark = $"[{mb.MB_Account}:gift oder pre]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                       db2.UpdateStr<Product>(new { obj.P_key, obj.P_GiftOrder, obj.P_mark });
                   });
               }
           }
           return new { error = 1, data = JObj.DBJson(obj) };
       }, e => JFormat.returnFail());


        /// <summary>
        /// 商品排序往下移
        /// </summary>
        /// <param name="key">主要切換order對像</param>
        /// <param name="key2">對換對像</param>
        /// <returns></returns>
        public object ProductGiftNext(Member mb, String key, String key2) =>
       new DBServer().open((cn, db) =>
       {
           var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key AND P_GiftOrder >-1 LIMIT 0,1", new
           {
               P_key = key
           }).FirstOrDefault();//判斷是否真實存在

           var minOder = cn.Query<Int32>("SELECT P_GiftOrder FROM Product WHERE P_GiftOrder >-1  ORDER BY P_GiftOrder DESC LIMIT 0,1").FirstOrDefault();
           if (obj.P_GiftOrder < minOder)
           {
               var obj2 = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key AND P_GiftOrder >-1  LIMIT 0,1", new
               {
                   P_key = key2
               }).FirstOrDefault();//取得上一層oder 號

               if (obj2 != null)
               {
                   new DBServer(write: true).open((cn2, db2) =>
                   {
                       cn2.Execute("UPDATE Product SET P_GiftOrder = @order2 WHERE P_key != 'empty' AND P_GiftOrder = @order;", new { order = obj2.P_GiftOrder, order2 = obj.P_GiftOrder });//修改商品排序
                       obj.P_GiftOrder = obj2.P_GiftOrder;
                       obj.P_mark = $"[{mb.MB_Account}:gift oder next]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                       db2.UpdateStr<Product>(new { obj.P_key, obj.P_GiftOrder, obj.P_mark });
                   });
               }
           }
           return new { error = 1, data = JObj.DBJson(obj) };
       }, e => JFormat.returnFail());
    }
}
