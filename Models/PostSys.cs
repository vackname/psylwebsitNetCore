using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// post 系統
    /// </summary>
    public class MGPostSys
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
        public object ProductList(String selfClass,string ser,bool filter,int giftModel, int page) => new MGProductSearch().ProductList(null,type:999,classVal: (int)ProductClass.product,
            selfClass: selfClass,ser:ser,
            filter: ((filter) ? new List<String>() { "yes" } : new List<String>() { "no" }),// yes =套餐,no 單點
            display:true,
            giftModel: giftModel,
            page:page,ps:(int)ProductStore.store);

        /// <summary>
        /// 取得套餐目前設定
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object setProductClass(String key)
        => new DBServer().open((cn, db) =>
        {
            List<string> sql = new List<string>() { "SELECT a.POS_Count,b.P_cash,a.P_key,b.P_name,b.P_unit FROM ProductOptionSet as `a` INNER JOIN Product as `b` ON a.P_key=b.P_key WHERE a.P_set_key=@pkey",
            "SELECT P_cash FROM Product WHERE P_key = @pkey LIMIT 0,1"};
            var getTB = cn.QueryMultiple(String.Join(';', sql), new { pkey = key });
            var getData = getTB.Read<payItem>().ToList();//取得套餐實際內容
            var getProductSum = getTB.Read<Double>().FirstOrDefault();//目前套餐總額
            double sumBest = getData.Select(n => (double)n.POS_Count * n.P_cash).Sum();//套餐實際內容比重分母

            foreach (var n in getData)
            {//重新定義套餐金額
                n.P_cash = Convert.ToDouble(((n.P_cash * (double)n.POS_Count!=0) ?(((n.P_cash * (double)n.POS_Count) / sumBest) * getProductSum) / (double)n.POS_Count:0).ToString("f2"));
            }



            return new
            {
                error = (int)SystemErrorCode.Null,
                data = JObj.DBJson(getData)
            };
        });
    }
}
