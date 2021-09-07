using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 商品分類設定
    /// </summary>
    public class MGProductSelect
    {
     
        /// <summary>
        /// 商品分類設定(first分類)排序往上移
        /// </summary>
        /// <returns></returns>
        public object ProductCNTPre(Member mb, ProductClassNameTitle pobj) =>
        new DBServer().open((cn, db) =>
        {
            var obj = cn.Query<ProductClassNameTitle>("SELECT * FROM ProductClassNameTitle WHERE PCT_key = @PCT_key  LIMIT 0,1", new
            {
                pobj.PCT_key
            }).FirstOrDefault();//判斷是否真實存在

            var minOder = cn.Query<Int32>("SELECT PCT_Order FROM ProductClassNameTitle ORDER BY PCT_order ASC LIMIT 0,1").FirstOrDefault();
            if (obj.PCT_Order > minOder)
            {
                new DBServer(write: true).open((cn2, db2) =>
                {
                    cn2.Execute("UPDATE ProductClassNameTitle SET PCT_Order = PCT_Order + 1 WHERE PCT_key != 'empty' AND PCT_Order = @order;", new { order = obj.PCT_Order - 1 });//修改商品排序
                    obj.PCT_Order -= 1;
                    obj.PCT_mark = $"[{mb.MB_Account}:oder pre]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                    db2.UpdateStr<ProductClassNameTitle>(new { obj.PCT_key, obj.PCT_Order, obj.PCT_mark });
                });
            }
            return new { error = 1, mark = obj.PCT_mark, order = obj.PCT_Order };
        }, e => JFormat.returnFail());

        /// <summary>
        /// 商品分類設定(first分類)排序往下移
        /// </summary>
        /// <returns></returns>
        public object ProductCNTNext(Member mb, ProductClassNameTitle pobj) =>
        new DBServer(write: true).open((cn, db) =>
        {
            var obj = cn.Query<ProductClassNameTitle>("SELECT * FROM ProductClassNameTitle WHERE PCT_key = @PCT_key  LIMIT 0,1", new
            {
                pobj.PCT_key
            }).FirstOrDefault();//判斷是否真實存在

            var MaxOder = cn.Query<Int32>("SELECT PCT_Order FROM ProductClassNameTitle ORDER BY PCT_Order DESC LIMIT 0,1").FirstOrDefault();
            if (obj.PCT_Order < MaxOder)
            {
                new DBServer(write: true).open((cn2, db2) =>
                {
                    cn2.Execute("UPDATE ProductClassNameTitle SET PCT_Order = PCT_Order - 1 WHERE PCT_key != 'empty' AND PCT_Order = @order;", new { order = obj.PCT_Order + 1 });//修改商品排序
                    obj.PCT_Order += 1;
                    obj.PCT_mark = $"[{mb.MB_Account}:oder next]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                    db2.UpdateStr<ProductClassNameTitle>(new { obj.PCT_key, obj.PCT_Order, obj.PCT_mark });
                });
            }
            return new { error = 1, mark = obj.PCT_mark, order = obj.PCT_Order };
        }, e => JFormat.returnFail());

        /// <summary>
        /// 商品分類設定(first分類)
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public object ProductCNTEdit(Member mb, ProductClassNameTitle obj, bool del = false, bool display = false) =>
        new DBServer(write: true).open((cn, db) =>
        {

            var getPCNT = cn.Query<ProductClassNameTitle>("SELECT * FROM ProductClassNameTitle WHERE PCT_key=@key  LIMIT 0,1", new { key = obj.PCT_key }).FirstOrDefault();
            if (getPCNT == null)
            {//insert
                obj.PCT_Order = 1;
                cn.Execute("UPDATE ProductClassNameTitle SET PCT_Order = PCT_Order + 1 WHERE PCT_key != ''");
                return new { error = 1, data = JObj.DBJson(db.InsertAutoKey(obj)) };
            }
            else
            {
                if (!del)
                {//編緝
                    if (!display)
                    {
                        getPCNT.PCT_Name = obj.PCT_Name;
                        getPCNT.PCT_store = obj.PCT_store;
                    }
                    else
                    {//顯示設定

                        getPCNT.PCT_display = !getPCNT.PCT_display;
                    }
                    getPCNT.PCT_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(rename)";
                    db.UpdateStr<ProductClassNameTitle>(new
                    {
                        getPCNT.PCT_store,
                        getPCNT.PCT_Name,
                        getPCNT.PCT_key,
                        getPCNT.PCT_display,
                        getPCNT.PCT_mark
                    });
                    return new { error = 1, mark = getPCNT.PCT_mark, display = getPCNT.PCT_display };
                }
                else
                {//刪除
                    var PCTkeyList = cn.Query<ProductClassName>("SELECT * FROM ProductClassName WHERE PCT_key = @PCTkey", new { PCTkey = getPCNT.PCT_key }).ToList();
                    var sql = new List<string>()
                     {
                        "UPDATE Product SET PC_key = null WHERE PC_key IN @Ary",
                        "UPDATE ProductClassNameTitle SET PCT_Order = PCT_Order - 1 WHERE PCT_key != @PCTkey AND PCT_Order > @order",
                        "DELETE FROM ProductClassName WHERE PCT_key = @PCTkey",
                        "DELETE FROM ProductClassNameTitle WHERE PCT_key = @PCTkey"
                     };
                    cn.Execute(String.Join(';', sql), new { PCTkey = getPCNT.PCT_key, order = getPCNT.PCT_Order, Ary = ((PCTkeyList.Count > 0) ? PCTkeyList.Select(n => n.PC_key).ToList() : new List<String>() { "empty" }) });
                    return new { error = 1, PCTkey = getPCNT.PCT_key, order = obj.PCT_Order };
                }
            }
        }, e => JFormat.returnFail());

        /// <summary>
        /// 商品分類search List(first分類)
        /// </summary>
        /// <param name="display">是否只顯示 未隱藏部份=false</param>
        /// <param name="ps">顯示 商品目錄</param>
        /// <param name="mg">管理者模式</param>
        /// <returns></returns>
        public object ProductCNTList(bool display = false, ProductStore ps = ProductStore.all, bool mg = false) =>
        new DBServer().open((cn, db) =>
        {
            List<int> storeAry = new List<int>();
            switch (ps)
            {
                case ProductStore.all:
                    storeAry.Add((int)ProductStore.all);
                    storeAry.Add((int)ProductStore.line);
                    storeAry.Add((int)ProductStore.store);
                    break;
                case ProductStore.line:
                    storeAry.Add((int)ProductStore.all);
                    storeAry.Add((int)ProductStore.line);
                    break;
                case ProductStore.store:
                    storeAry.Add((int)ProductStore.all);
                    storeAry.Add((int)ProductStore.store);
                    break;

            }
            return new
            {
                error = 1,
                data =
                    ((!mg) ? JObj.DBJson(MemoryModel.pct.Where(n => storeAry.IndexOf(n.PCT_store) > -1).OrderBy(n => n.PCT_Order).ToList())
                    :
                    JObj.DBJson(cn.Query<ProductClassNameTitle>($"SELECT * FROM ProductClassNameTitle WHERE {((display) ? "PCT_display = true AND" : "")} PCT_store IN @storeAry ORDER BY PCT_Order ASC", new { storeAry }).ToList(), ((!mg) ? new List<string>() { "mark" } : null)))
            };
        }, e => JFormat.returnFail());


        /// <summary>
        /// 商品分類設定(二次分類)排序往上移
        /// </summary>
        /// <returns></returns>
        public object ProductCNPre(Member mb, ProductClassName pobj) =>
        new DBServer(write: true).open((cn, db) =>
        {
            var obj = cn.Query<ProductClassName>("SELECT * FROM ProductClassName WHERE PC_key = @PC_key  LIMIT 0,1", new
            {
                pobj.PC_key
            }).FirstOrDefault();//判斷是否真實存在

            var minOder = cn.Query<Int32>("SELECT PC_Order FROM ProductClassName ORDER BY PC_order ASC LIMIT 0,1").FirstOrDefault();
            if (obj.PC_Order > minOder)
            {
                cn.Execute("UPDATE ProductClassName SET PC_Order = PC_Order + 1 WHERE PC_key != 'empty' AND PC_Order = @order AND PCT_key = @pctkey;", new { order = obj.PC_Order - 1, pctkey = obj.PCT_key });//修改商品排序
                obj.PC_Order -= 1;
                obj.PC_mark = $"[{mb.MB_Account}:oder pre]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                db.UpdateStr<ProductClassName>(new { obj.PC_key, obj.PC_Order, obj.PC_mark });
            }
            return new { error = 1, mark = obj.PC_mark, order = obj.PC_Order };
        }, e => JFormat.returnFail());


        /// <summary>
        /// 商品分類設定(二次分類)排序往下移
        /// </summary>
        /// <returns></returns>
        public object ProductCNNext(Member mb, ProductClassName pobj) =>
        new DBServer(write: true).open((cn, db) =>
        {
            var obj = cn.Query<ProductClassName>("SELECT * FROM ProductClassName WHERE PC_key = @PC_key  LIMIT 0,1", new
            {
                pobj.PC_key
            }).FirstOrDefault();//判斷是否真實存在

            var MaxOder = cn.Query<Int32>("SELECT PC_Order FROM ProductClassName ORDER BY PC_Order DESC LIMIT 0,1").FirstOrDefault();
            if (obj.PC_Order < MaxOder)
            {
                cn.Execute("UPDATE ProductClassName SET PC_Order = PC_Order - 1 WHERE PC_key != 'empty' AND PC_Order = @order AND PCT_key = @pctkey;", new { order = obj.PC_Order + 1, pctkey = obj.PCT_key });//修改商品排序
                obj.PC_Order += 1;
                obj.PC_mark = $"[{mb.MB_Account}:oder next]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                db.UpdateStr<ProductClassName>(new { obj.PC_key, obj.PC_Order, obj.PC_mark });
            }
            return new { error = 1, mark = obj.PC_mark, order = obj.PC_Order };
        }, e => JFormat.returnFail());


        /// <summary>
        /// 商品分類設定(二次分類)
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public object ProductCNEdit(Member mb, ProductClassName obj, bool del = false, bool display = false) =>
        new DBServer(write: true).open((cn, db) =>
        {
            var getPCN = cn.Query<ProductClassName>("SELECT * FROM ProductClassName WHERE PC_key=@key  LIMIT 0,1", new { key = obj.PC_key }).FirstOrDefault();
            var getPCNT = cn.Query<ProductClassNameTitle>("SELECT * FROM ProductClassNameTitle WHERE PCT_key=@key  LIMIT 0,1", new { key = obj.PCT_key }).FirstOrDefault();
            if (getPCNT != null)
            {
                if (getPCN == null)
                {//insert
                    obj.PC_Order = 1;
                    cn.Execute("UPDATE ProductClassName SET PC_Order = PC_Order + 1 WHERE PC_key != '' AND PCT_key = @fkey", new { fkey = getPCNT.PCT_key });
                    return new { error = 1, data = JObj.DBJson(db.InsertAutoKey(obj)) };
                }
                else
                {
                    if (!del)
                    {//編緝
                        if (!display)
                        {
                            getPCN.PC_Name = obj.PC_Name;
                            getPCN.PC_store = obj.PC_store;
                        }
                        else
                        {//顯示設定
                            getPCN.PC_display = !getPCN.PC_display;
                        }
                        getPCN.PC_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(rename)";
                        db.UpdateStr<ProductClassName>(new
                        {
                            getPCN.PC_store,
                            getPCN.PC_Name,
                            getPCN.PC_key,
                            getPCN.PC_display,
                            getPCN.PC_mark
                        });
                        return new { error = 1, mark = getPCN.PC_mark, display = getPCN.PC_display };
                    }
                    else
                    {//刪除
                        if (!cn.Query<bool>("SELECT EXISTS(SELECT * FROM Product WHERE PC_key = @pckey  LIMIT 0,1)", new { pckey = getPCN.PC_key }).FirstOrDefault())
                        {
                            cn.Execute(@"UPDATE ProductClassName SET PC_Order = PC_Order - 1 WHERE PC_key != @key AND PC_Order > @order AND PCT_key = @fkey;
DELETE FROM ProductClassName WHERE PC_key = @key;
", new { order = obj.PC_Order, fkey = getPCN.PCT_key, key = getPCN.PC_key });//修改排序
                            return new { error = 1, key = getPCN.PC_key, order = getPCN.PC_Order, display = getPCN.PC_display };
                        }
                        else
                        {
                            return JFormat.returnFail((int)SystemErrorCode.ExistData);
                        }
                    }
                }
            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.notExistID);
            }
        }, e => JFormat.returnFail());

        /// <summary>
        /// 商品分類search List(二次分類)
        /// </summary>
        /// <param name="serKey">分類first key</param>
        /// <param name="display">是否只顯示 未隱藏部份=false</param>
        /// <param name="ps">顯示 商品目錄</param>
        /// <param name="mg">管理模式</param>
        /// <returns></returns>
        public object ProductCNList(String serKey, bool display = false, ProductStore ps = ProductStore.all, bool mg = false)
        {
            List<int> storeAry = new List<int>();
            switch (ps)
            {
                case ProductStore.all:
                    storeAry.Add((int)ProductStore.all);
                    storeAry.Add((int)ProductStore.line);
                    storeAry.Add((int)ProductStore.store);
                    break;
                case ProductStore.line:
                    storeAry.Add((int)ProductStore.all);
                    storeAry.Add((int)ProductStore.line);
                    break;
                case ProductStore.store:
                    storeAry.Add((int)ProductStore.all);
                    storeAry.Add((int)ProductStore.store);
                    break;

            }
            return new DBServer().open((cn, db)

               => new {
                   error = 1,
                   data =
               ((!mg) ? JObj.DBJson(MemoryModel.pc.Where(n => storeAry.IndexOf(n.PC_store) > -1 && n.PCT_key == serKey).OrderBy(n => n.PC_Order).ToList()) :
               JObj.DBJson(cn.Query<ProductClassName>($"SELECT * FROM ProductClassName WHERE PCT_key = @key AND PC_store IN @storeAry {((display) ? "AND PC_display = true" : "")} ORDER BY PC_Order ASC", new { key = serKey, storeAry }).ToList(), ((!mg) ? new List<string>() { "mark" } : null)))
               }
                , e => JFormat.returnFail());
        }
    }
}
