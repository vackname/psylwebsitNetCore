using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 商品套餐設定
    /// </summary>
    public class MGProductSet
    {
        /// <summary>
        /// product set option list
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="pkey"></param>
        /// <returns></returns>
        public object ProductSetOptionList(String pkey)
        => new DBServer().open((cn, db) =>
        {
            var obj = cn.Query<ProductOptionSet>("SELECT a.*,b.P_Name,b.P_img,b.P_unit FROM ProductOptionSet as a INNER JOIN Product as b ON a.P_key = b.P_key WHERE a.P_set_key = @pkey ORDER BY b.PC_key ASC", new { pkey }).ToList();
            return new { error = 1, data = JObj.DBJson(obj) };
        }, e => JFormat.returnFail());

        /// <summary>
        /// 套餐 新增 options
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="aryKey">product key</param>
        /// <returns></returns>
        public object ProductSetOptionAdd(Member mb, List<String> aryKey, String pkey)
          => new DBServer().open((cn, db) =>
          {
              var obj = cn.Query<Product>("SELECT * FROM Product WHERE P_key IN @ary AND P_set = false", new { ary = aryKey }).ToList();//防止偽裝攻擊 options (非套餐)
              var setProduct = cn.Query<Product>("SELECT * FROM Product WHERE P_key = @key AND P_set = true  LIMIT 0,1", new { key = pkey }).FirstOrDefault();//檢測真實屬性(套餐)
              if (setProduct != null && obj.Count() > 0)
              {
                  var existPOS = cn.Query<ProductOptionSet>("SELECT * FROM ProductOptionSet WHERE P_set_key = @key", new { key = pkey }).ToList();
                  List<ProductOptionSet> SetAdd = new List<ProductOptionSet>();
                  long nowDate = new pub().unixDateTimeNow();

                  String mark = $"[{mb.MB_Account}:Inset]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";

                  foreach (var n in obj)
                  {
                      if (existPOS.Where(a => a.P_key == n.P_key).FirstOrDefault() == null)
                      {//取不存在 setting
                          SetAdd.Add(new ProductOptionSet()
                          {
                              P_set_key = pkey,
                              POS_Count = 0,
                              POS_mark = mark,
                              P_key = n.P_key,
                              POS_Date = nowDate
                          });
                      }
                  }

                  if (SetAdd.Count > 0)
                  {//回補補格式(product)
                      new DBServer(write: true).open((cn2, db2) =>
                      {
                          SetAdd = db.InsertAutoKeyList(SetAdd);
                      });
                      var getProductName = cn.Query<Product>("SELECT * FROM Product WHERE P_key IN @ary", new { ary = SetAdd.Select(n => n.P_key).ToList() }).ToList();
                      foreach (var n in SetAdd)
                      {
                          var getdata = getProductName.Where(a => a.P_key == n.P_key).FirstOrDefault();
                          n.P_Name = getdata.P_Name;
                          n.P_unit = getdata.P_unit;
                      }
                  }

                  return new { error = 1, data = JObj.DBJson(SetAdd) };

              }
              else
              {
                  return JFormat.returnFail((int)SystemErrorCode.inputFormat);
              }

          }, e => JFormat.returnFail());

        /// <summary>
        /// 套餐edit options
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="getObj"></param>
        /// <param name="del"></param>
        /// <returns></returns>
        public object ProductSetOptionEdit(Member mb, ProductOptionSet getObj, bool del = false)
        => new DBServer().open((cn, db) =>
        {
            var poObj = cn.Query<ProductOptionSet>("SELECT * FROM ProductOptionSet WHERE POS_key = @key LIMIT 0,1", new
            {
                key = getObj.POS_key
            }).FirstOrDefault();
            if (poObj != null)
            {
                if (!del)
                {
                    poObj.POS_Count = getObj.POS_Count;
                    poObj.POS_mark = $"[{mb.MB_Account}:edit]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                    new DBServer(write: true).open((cn2, db2) =>
                    {
                        db2.UpdateStr<ProductOptionSet>(new
                        {
                            poObj.POS_key,
                            poObj.POS_mark,
                            poObj.POS_Count
                        });
                    });
                    return new { error = 1, mark = poObj.POS_mark };
                }
                else
                {
                    new DBServer(write: true).open((cn2, db2) =>
                    {
                        db2.DeleteStr(poObj);
                    });
                    return JFormat.returnOk();
                }
            }
            else
            {

                return JFormat.returnFail((int)SystemErrorCode.notExistID);
            }
        }, e => JFormat.returnFail());
    }
}
