using System;
using System.Linq;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 商品折設定
    /// </summary>
    public class MGProductDiscount
    {
        /// <summary>
        /// 商品折設定
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="pkey"></param>
        /// <returns></returns>
        public object DiscountList(String pkey) =>
        new DBServer().open((cn, db) =>
        {
            var obj = cn.Query("SELECT * FROM ProductDiscount USE INDEX(P_key) WHERE P_key = @pkey ORDER BY PD_date DESC;", new
            {
                pkey
            }).ToList();
            return new { error = 1, data = JObj.DBJson<ProductDiscount>(obj), nowdate = new pub().unixDateTimeNow() };
        }, e => JFormat.returnFail());

        /// <summary>
        /// add折設定
        /// </summary>
        /// <param name="mb"></param>
        /// <param name=" P_key">商品</param>
        /// <param name="start">時間start</param>
        /// <param name="end">時間end</param>
        /// <returns></returns>
        public object addDiscount(Member mb, ProductDiscount obj) =>
        new DBServer().open((cn, db) =>
        {
            if (obj.PD_discount >= 0.7)
            {//防止折誤觸
                var product = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key  LIMIT 0,1", new
                {
                    obj.P_key
                }).FirstOrDefault();//商品

                if (product != null)
                {
                    product.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(add 折)";
                    obj.P_key = product.P_key;
                    obj.PD_date = new pub().unixDateTimeNow();
                    obj.PD_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(add)";


                    if (obj.PD_start > obj.PD_End)
                    {//自動交換由小到大日期容器
                        var getStart = obj.PD_start;
                        var getEnd = obj.PD_End;
                        obj.PD_start = getEnd;
                        obj.PD_End = getStart;
                    }
                    new DBServer(write: true).open((cn2, db2) =>
                    {
                        db2.UpdateStr<Product>(new
                        {
                            product.P_key,
                            product.P_mark,
                        });
                    });

                    return new
                    {
                        error = 1,
                        data = JObj.DBJson(db.InsertAutoKey(obj)),
                        productmark = product.P_mark//商品備註
                    };
                }
                else
                {
                    return JFormat.returnFail((int)SystemErrorCode.prdocutNotExist);
                }
            }
            else
            {
                return JFormat.returnFail();
            }
        }, e => JFormat.returnFail());


        /// <summary>
        /// edit折設定
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="PD_key"></param>
        /// <param name="display">起用 標用模式</param>
        /// <returns></returns>
        public object editDiscount(Member mb, String PD_key, bool display = false) =>
        new DBServer().open((cn, db) =>
        {
            var obj = cn.Query<ProductDiscount>("SELECT * FROM ProductDiscount WHERE PD_key = @PD_key  LIMIT 0,1", new
            {
                PD_key
            }).FirstOrDefault();//折

            var product = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @P_key  LIMIT 0,1", new
            {
                obj.P_key
            }).FirstOrDefault();//商品

            new DBServer(write: true).open((cn2, db2) =>
            {
                if (!display)
                {//remove
                    product.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(remove 折)";
                    db.DeleteStr<ProductDiscount>(obj);
                }
                else
                {//起動或關閉
                    product.P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(起用、停用 折)";
                    obj.PD_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}(起用、停用 折)";
                    obj.PD_display = !obj.PD_display;
                    db2.UpdateStr<ProductDiscount>(new
                    {
                        obj.PD_key,
                        obj.PD_display,
                        obj.PD_mark,
                    });
                }

                db2.UpdateStr<Product>(new
                {
                    product.P_key,
                    product.P_mark,
                });
            });
            return new
            {
                error = 1,
                data = JObj.DBJson(obj),
                productmark = product.P_mark//商品備註
            };
        }, e => JFormat.returnFail());
    }
}
