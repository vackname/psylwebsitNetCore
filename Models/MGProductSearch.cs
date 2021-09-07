using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 商品搜尋
    /// </summary>
    public class MGProductSearch
    {
        /// <summary>
        /// 商品單載
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public object ProductDoc(string key,bool mg=false)=>
        new DBServer().open((cn, db) =>
        {
           string mgModel = ((!mg) ? "AND P_type > -1 AND MB_appCK = TRUE  AND P_ck <> '' AND P_ck <> 'run' AND P_ck <> 'runfail'" : "");
            var obj =  cn.Query<Product>($"SELECT * FROM Product WHERE P_key=@key {mgModel} LIMIT 0,1",
                new { key=key }
                ).FirstOrDefault();
            if (obj != null)
            { 

                var catchVisibleClassTitle = MemoryModel.pc.Where(n => n.PC_key == obj.PC_key).FirstOrDefault();
                if (catchVisibleClassTitle != null)
                {
                    //放入pct_key
                    obj.pctkey = catchVisibleClassTitle.PCT_key;
                   
                    //圖片單張注入
                    string bestPath = Path.Combine(MemoryModel.filePath, $"{obj.P_key}.json");
                    if (File.Exists(bestPath))
                    {//取圖片資訊
                        obj.imgAry = JObj.readFile<List<string>>(bestPath, false);
                    }

                    var P_Discount = cn.Query<ProductDiscount>("SELECT * FROM ProductDiscount USE INDEX(P_key) WHERE P_key = @pkey AND PD_start < @date AND PD_end>=@date AND PD_display = TRUE;", new
                    {//取出折扣設定
                        pkey = obj.P_key,
                        date = new pub().unixDateTimeNow()
                    }).ToList();

                    return new
                    {
                        error = (int)SystemErrorCode.Null,
                        data = JObj.DBJson(obj, new List<string> { "mark" }),
                        discount = ((P_Discount.Count() > 0) ? JObj.DBJson(P_Discount) : new List<string>())//正在起動折扣功能
                    };
                }
            }
            return JFormat.returnFail((int)SystemErrorCode.limit);
        });

        /// <summary>
        /// 商品(推薦、一般商品)list
        /// </summary>
        /// <param name="type">999=所有庫存狀態LIST enum ProductType (enum ProductType)</param>
        /// <param name="classVal">999=所有類別LIST enum ProductClass (enum ProductClass)</param>
        /// <param name="selfClass">999=所有商品顯示類別LIST</param>
        /// <param name="ser">搜尋</param>
        /// <param name="page">分頁</param>
        /// <param name="filter">過濾套餐=true</param>
        /// <param name="display">是否判斷顯不顯示(display = true=判斷)</param>
        /// <param name="pageModel">分頁模式=true,全顯次序下拉式分頁=false</param>
        /// <param name="giftModel">商品推薦模式 1= only 推薦模式條件,2=不包涵推薦模式,3=包涵推薦模式排序</param>
        /// <param name="ps">product store 判斷 -1=不判斷</param>
        /// <param name="mg">管理模式</param>
        /// <param name="appck">條件申請審核/異動商品 </param>
        /// <returns></returns>
        public object ProductList(Member mb, int type, int classVal, String selfClass, String ser, int page, List<String> filter = null, bool display = false, bool pageModel = false, int giftModel = 0, int ps = -1, bool mg = false, bool appck = false)
        {
            try
            {
                var productList = JObj.EnumList<ProductType>();//過濾庫存狀態是否異常
                var productClassList = JObj.EnumList<ProductClass>();//產品庫存屬性=classVal=實體、app life、虛擬
                if (productList.Where(n => n.value == type).FirstOrDefault() != null || type == 999)
                {
                    String giftModelStr = ((giftModel == (int)giftOptin.only) ? "AND P_GiftOrder>-1" : ((giftModel == (int)giftOptin.not) ? "AND P_GiftOrder=-1" : ""));//推薦搜尋模式
                    String giftOrder = ((new List<int>() { (int)giftOptin.only, (int)giftOptin.gift }.IndexOf(giftModel) > -1) ? $"IF(P_GiftOrder=-1, 999999999, P_GiftOrder){((giftModel == (int)giftOptin.not) ? ",P_order" : "")}" : "P_order");//搜尋模式 order
                    String pstore = ((ps > -1) ? $" AND P_store IN @storeAry" : "");//商店、線上 分屬判斷
                    String owner = ((mb != null) ? ((new List<int>() { (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1) ? " AND MB_uid = @uid" : "") : "");//持有者/通過審核

                    if (!mg)
                    {//一般狀況取商品資訊(需有審核代號)
                        owner += " AND MB_appCK = TRUE AND P_ck <> '' AND P_ck <> 'run' AND P_ck <> 'runfail'";
                    }
                    else if (appck)
                    {//僅搜尋出異動、申請審核
                        owner += " AND (MB_appCK = FALSE OR P_ck = 'run' OR P_ck = 'runfail')";
                    }

                    if (mb != null && !appck)
                    {//系統管理者 只能看商家已申請送審之商品
                        if (new List<int>() { (int)MBLevel.Edit, (int)MBLevel.systemMG }.IndexOf(mb.MB_level) > -1)
                        {
                            owner += " AND P_ck <> ''";

                        }
                    }

                    List<int> storeAry = new List<int>();
                    switch (ps)
                    {
                        case (int)ProductStore.all:
                            storeAry.Add((int)ProductStore.all);
                            storeAry.Add((int)ProductStore.line);
                            storeAry.Add((int)ProductStore.store);
                            break;
                        case (int)ProductStore.line:
                            storeAry.Add((int)ProductStore.all);
                            storeAry.Add((int)ProductStore.line);
                            break;
                        case (int)ProductStore.store:
                            storeAry.Add((int)ProductStore.all);
                            storeAry.Add((int)ProductStore.store);
                            break;
                    }

                    int pageListCount = 20;//筆數
                    return new DBServer().open((cn, db) =>
                    {
                        //商品搜尋條件
                        String Where = $"FROM Product WHERE (P_name LIKE @ser OR P_key = @ser2) AND P_type IN @typeAry AND P_class IN @classAry AND (PC_key IN @selfClassAry OR @selfclassall) AND P_set IN @setAry AND P_key NOT IN @notInAry {(giftModelStr + pstore + owner)}";
                        List<string> sql = new List<string>() {
                            $"SELECT * {Where} ORDER BY {giftOrder},P_class ASC LIMIT @page,20",//商品輸出框架
                            $"SELECT COUNT('1') {Where}"
                        };
                        List<string> selectClass = new List<string>();//取得條件分類商品
                        if (!mg && selfClass == "333")
                        {//全部分類(first)
                            selectClass = MemoryModel.pct.Join(MemoryModel.pc, a => a.PCT_key, b => b.PCT_key, (a, b) => b.PC_key).ToList();
                        }
                        else
                        {//二次分類
                            selectClass = ((selfClass.IndexOf("999") == 0) ?
                                ((!mg) ? MemoryModel.pc.Where(n => n.PCT_key == selfClass.Replace("999", "")).Select(n => n.PC_key).ToList() : cn.Query<String>($"SELECT PC_key FROM ProductClassName WHERE PCT_key = @key AND PC_display = TRUE", new { key = selfClass.Replace("999", "") }).ToList()) :
                                new List<String>() { ((!mg) ? ((MemoryModel.pc.Where(n => n.PC_key == selfClass).Count() > 0) ? selfClass : "empty") : selfClass) });
                        }

                        using (var objData = cn.QueryMultiple(String.Join(";", sql),
                        new
                        {
                            selfclassall = ((!mg) ? false : selfClass == "333"),//是否為未選屬性 全部分類(first)
                            selfClassAry = selectClass,//因類別不顯示此商品
                            ser = $"%{ser}%",
                            ser2 =ser,//搜尋絕對 key
                            typeAry = ((type == 999 || display) ? productList.Where(n => ((display) ? n.value > -1 : true)).Select(n => n.value).ToList() : productList.Where(n=> ((type==0)?new List<int>() {(int)ProductType.NotLimit,(int)ProductType.StopNotLimit } : new List<int>() { (int)ProductType.Limit, (int)ProductType.StopLimit }).IndexOf(n.value)>-1).Select(n=>n.value).ToList()),
                            page = page * pageListCount,
                            classAry = ((classVal == 999) ? productClassList.Select(n => n.value).ToList() : new List<int>() { classVal }),
                            setAry = ((filter == null) ? new List<bool>() { true, false } : new List<bool>() { ((filter.Count() == 0) ? false : filter[0] == "yes") }),//是否為套餐
                            notInAry = ((filter == null) ? new List<String>() { "empty" } : ((filter.Count() == 0) ? new List<String>() { "empty" } : filter)), //排除 pkey + 套餐false=取非套餐排除
                            storeAry,//分屬開放權限
                            uid = ((mb != null) ? mb.MB_uid : "")
                        }))
                        {
                            var obj = objData.Read<Product>().ToList();
                            var CountObj = objData.Read<int>().FirstOrDefault();
                            //-----read complete

                            #region 取得顯示商品title
                            List<String> getpcnAry = obj.Where(n => n.PC_key != null && n.PC_key != "").Select(n => n.PC_key).Distinct().ToList();
                            if (getpcnAry.Count() > 0)
                            {
                                var catchVisibleClassTitle = ((!mg) ? MemoryModel.pc.Where(n => getpcnAry.ToList().IndexOf(n.PC_key) > -1).ToList() : cn.Query<ProductClassName>("SELECT * FROM ProductClassName WHERE PC_key IN @ary", new { ary = getpcnAry.ToList() }).ToList());
                                foreach (var n in obj)
                                {
                                    var getpctkey = catchVisibleClassTitle.Where(a => a.PC_key == n.PC_key).FirstOrDefault();
                                    if (getpctkey != null)
                                    {//放入pct_key
                                        n.pctkey = catchVisibleClassTitle.Where(a => a.PC_key == n.PC_key).FirstOrDefault().PCT_key;
                                    }
                                }
                            }
                            #endregion

                            foreach (var n in obj)
                            {
                                //圖片單張注入
                                string bestPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "product", $"{n.P_key}.json");
                                if (File.Exists(bestPath))
                                {//取圖片資訊
                                    if (!mg)
                                    {//僅單張
                                        n.imgAry = JObj.readFile<List<string>>(bestPath, false).Take(1).ToList();
                                    }
                                    else
                                    {//全部
                                        n.imgAry = JObj.readFile<List<string>>(bestPath, false);
                                    }
                                }
                            }

                            var P_Discount = cn.Query<ProductDiscount>("SELECT * FROM ProductDiscount USE INDEX(P_key) WHERE P_key IN @pkeyAry AND PD_start < @date AND PD_end>=@date AND PD_display = TRUE;", new
                            {//取出折扣設定
                                pkeyAry = ((obj.Count() > 0) ? obj.Select(n => n.P_key).ToList() : new List<String>() { "empty" }),
                                date = new pub().unixDateTimeNow()
                            }).ToList();
                            return new
                            {
                                error = 1,
                                data = JObj.DBJson(obj, ((!mg) ? new List<string> { "mark" } : null)),
                                pageCount = ((pageModel) ? (Int32)Math.Ceiling((Double)CountObj / (Double)pageListCount) : CountObj),
                                discount = ((P_Discount.Count() > 0) ? JObj.DBJson(P_Discount) : new List<string>())//正在起動折扣功能
                            };
                        }
                    });
                }
                else
                {
                    return JFormat.returnFail((int)SystemErrorCode.inputFormat);
                }
            }
            catch (Exception e)
            {
                return JFormat.returnFail();
            }
        }
    }
}
