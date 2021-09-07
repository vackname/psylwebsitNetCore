using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 購物車容器
    /// </summary>
    public class ProductContainer
    {
        /// <summary>
        /// 輸出商品(實體)-由類別搜尋
        /// </summary>
        /// <param name="selfClass">999=所有商品顯示類別LIST</param>
        /// <param name="ser">搜尋</param>
        /// <param name="page">分頁</param>
        /// <param name="filter">過濾套餐=true</param>
        /// <param name="giftModel">商品推薦模式 1= only 推薦模式條件,2=不包涵推薦模式,3=包涵推薦模式排序</param>
        /// <returns></returns>
        public object ProductList(String selfClass, string ser, bool filter, int page) => new MGProductSearch().ProductList(null, type: 999, classVal: 999,
            selfClass: selfClass, ser: ser,
            filter: ((filter) ? new List<String>() { "yes" } : new List<String>() { "no" }),// yes =套餐,no 單點
            display: true,
            giftModel: (int)giftOptin.gift,
            pageModel:true,
            page: page, ps: (int)ProductStore.line);

        /// <summary>
        /// 輸出商品購物車名單
        /// </summary>
        /// <param name="car">client 購物車記錄</param>
        /// <returns></returns>
        public object PCCarList(List<string> car,bool mg=false) =>
        new DBServer().open((cn, db) =>
        {
            var pc = MemoryModel.pct.Join(MemoryModel.pc, a => a.PCT_key, b => b.PCT_key, (a, b) => b).ToList();
            string mgModel = ((!mg) ? "AND P_type > -1 AND PC_key IN @pcary AND MB_appCK = TRUE  AND P_ck <> '' AND P_ck <> 'run' AND P_ck <> 'runfail'" : "");
            var data = cn.Query<Product>($"SELECT * FROM Product WHERE P_key IN @ary {mgModel}", new { ary = car ,pcary=pc.Select(n=>n.PC_key).ToList()}).ToList();
            //取得顯示商品title
            foreach (var n in data)
            {
                var pct = pc.Where(a => a.PC_key == n.PC_key).FirstOrDefault();
                if (pct != null)
                {
                    n.pctkey = pct.PCT_key;
                }
            }

            var P_Discount = cn.Query<ProductDiscount>("SELECT * FROM ProductDiscount USE INDEX(P_key) WHERE P_key IN @pkeyAry AND PD_start < @date AND PD_end>=@date AND PD_display = TRUE;", new
            {//取出折扣設定
                pkeyAry = ((data.Count() > 0) ? data.Select(n => n.P_key).ToList() : new List<String>() { "empty" }),
                date = new pub().unixDateTimeNow()
            }).ToList();

            foreach (var n in data)
            {
                //圖片單張注入
                string bestPath = Path.Combine(MemoryModel.filePath, $"{n.P_key}.json");
                if (File.Exists(bestPath))
                {//取圖片資訊
                     n.imgAry = JObj.readFile<List<string>>(bestPath, false).Take(1).ToList();
                }
            }
            return new { error = (int)SystemErrorCode.Null, data = JObj.DBJson(data,new List<string>() {"mark", "date" }),discount=JObj.DBJson(P_Discount)};
        }, e => JFormat.returnFail());
    }
}
