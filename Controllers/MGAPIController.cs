using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using DBConfig;
using Microsoft.AspNetCore.Mvc;
using productCar.Models;
using pubclass;

namespace productCar.Controllers
{
    [Route("mg/")]
    public class MGAPIController : Controller
    {
        IHttpClientFactory _hcf;
        public MGAPIController(IHttpClientFactory hcf)
        {
            _hcf = hcf;
        }

        [HttpPost("chief/{id1?}/{id2?}")]
        public IActionResult chiefAdminMG(String id1, String id2)
       => Json(new pubcolApp(this.HttpContext).actionApp(n =>
       {
           n.limit = new List<MBLevel> { MBLevel.superSystemMG };
           n.mobileType = false;
       }, HttpGetType.SuccessfullySin, n =>
       {
           switch (id1 ?? "")
           {
                #region 偵聽-系統環境
                case "watchdata"://監聽-系統環境資料
                    return new { error = (int)SystemErrorCode.Null, data = enviModel.PJ };
               case "watchsysrun"://起動監聽-系統環境
                    return new enviModel().runCatch();
               case "watchclose"://關閉監聽-系統環境
                    enviModel.PJ.run = false;
                   return JFormat.returnOk();
                #endregion
               
            }
           return JFormat.nullApi();
       }));

        [HttpPost("sys/{id1?}/{id2?}")]
        public IActionResult AdminMG(String id1, String id2)
        => Json(new pubcolApp(this.HttpContext).actionApp(n =>
        {
            n.limit = new List<MBLevel> { MBLevel.Edit,MBLevel.systemMG };
            n.mobileType = false;
        }, HttpGetType.SuccessfullySin, n =>
        {
            switch (id1 ?? "")
            {
                case "lang"://支持語系
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), lang: true);
                case "productpre"://商品oder 往上
                    return new MGProduct().ProductPre(n.mb, n.http.Request.Form["key"].ToString(), n.http.Request.Form["key2"].ToString());
                case "productnext"://商品oder 往下
                    return new MGProduct().ProductNext(n.mb, n.http.Request.Form["key"].ToString(), n.http.Request.Form["key2"].ToString());
                case "productlist"://目前已設定商品
                    return new MGProductSearch().ProductList(n.mb, Convert.ToInt32(n.http.Request.Form["type"]), Convert.ToInt32(n.http.Request.Form["class"]), n.http.Request.Form["selfclass"].ToString(), n.http.Request.Form["ser"].ToString(), Convert.ToInt32(n.http.Request.Form["page"]), mg: true, appck: n.http.Request.Form["appck"].ToString() == "1");
                case "productedit"://商品設定
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http));
                case "productinsert"://商品insert
                    var insertP = JObj.DBPost<Product>(n.http);
                    insertP.P_key = "-insert-";
                    return new MGProduct().ProdcutEdit(n.mb, insertP);
                case "productonoff"://起用禁用購買(使用者)
                    return new MGProduct().ProductOnOff(n.mb, n.http.Request.Form["key"].ToString());
                case "pincount"://增加庫存
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), inCount: true);
                case "pdecount"://減少庫存
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), deCount: true);
                case "limitcount"://購買數量限制
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), countLimit: true);
                case "pstore"://通貨設定
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), pStore:true);
                case "productremove"://刪除商品
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), del: true);
                #region 折設定
                case "clist"://折list
                    return new MGProductDiscount().DiscountList(n.http.Request.Form["key"].ToString());
                case "addcobj"://折add
                    return new MGProductDiscount().addDiscount(n.mb, JObj.DBPost<ProductDiscount>(n.http));
                case "removecobj"://折remove
                    return new MGProductDiscount().editDiscount(n.mb, n.http.Request.Form["key"].ToString());
                case "dispalycobj"://折編緝
                    return new MGProductDiscount().editDiscount(n.mb, n.http.Request.Form["key"].ToString(), true);
                #endregion
                case "ybe"://video youtube
                    return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), ybe: true);
                #region 商品圖片
                case "delimg"://刪除商品圖片
                    return new MGProductImg().RemovePhoto(n.mb, n.http.Request.Form["key"].ToString(), JObj.des<List<string>>(n.http.Request.Form["imgary"].ToString()));
                case "imgupload"://圖片碎片上傳
                    return new MGProductImg().imgUploadData(n.mb, JObj.DBPost<uploadImg>(n.http));
                case "imgpre"://圖片 上移
                    return new MGProductImg().docImgPre(n.mb, key: n.http.Request.Form["key"].ToString(), imgPath: n.http.Request.Form["imgpath"].ToString());
                case "imgnext"://圖片 下移
                    return new MGProductImg().docImgNext(n.mb, key: n.http.Request.Form["key"].ToString(), imgPath: n.http.Request.Form["imgpath"].ToString());
                #endregion
                case "doc"://商品簡述
                    return new MGProduct().PDEdit(n.mb, pkey: n.http.Request.Form["key"].ToString(), n.http.Request.Form["content"].ToString(), Convert.ToInt32(n.http.Request.Form["langnu"]));
                #region 商品分類
                case "productcfpre"://product分類 first order 上移
                    return new MGProductSelect().ProductCNTPre(n.mb, JObj.DBPost<ProductClassNameTitle>(n.http));
                case "productcfnext"://product分類 first order 下移
                    return new MGProductSelect().ProductCNTNext(n.mb, JObj.DBPost<ProductClassNameTitle>(n.http));
                case "productcfdel"://product分類 first del
                    return new MGProductSelect().ProductCNTEdit(n.mb, JObj.DBPost<ProductClassNameTitle>(n.http), del: true);
                case "productcfed"://product分類 first(edit、insert)
                    var firstClass = JObj.DBPost<ProductClassNameTitle>(n.http);
                    if (JObj.EnumList<ProductStore>().Where(n => n.value == firstClass.PCT_store).Count() > 0)
                    {
                        return new MGProductSelect().ProductCNTEdit(n.mb, firstClass);
                    }
                    else
                    {
                        return JFormat.returnFail((int)SystemErrorCode.notExistID);
                    }
                case "productcfdisplay"://first顯示
                    return new MGProductSelect().ProductCNTEdit(n.mb, JObj.DBPost<ProductClassNameTitle>(n.http), display: true);
                case "productcflist"://product分類 first list
                    return new MGProductSelect().ProductCNTList(mg: true);
                case "productcspre"://product分類 第二層 order 上移
                    return new MGProductSelect().ProductCNPre(n.mb, JObj.DBPost<ProductClassName>(n.http));
                case "productcsnext"://product分類 第二層 order 下移
                    return new MGProductSelect().ProductCNNext(n.mb, JObj.DBPost<ProductClassName>(n.http));
                case "productcsdel"://product分類 第二層 del
                    return new MGProductSelect().ProductCNEdit(n.mb, JObj.DBPost<ProductClassName>(n.http), del: true);
                case "productcsdisplay"://product分類 第二層 display
                    return new MGProductSelect().ProductCNEdit(n.mb, JObj.DBPost<ProductClassName>(n.http), display: true);
                case "productcsed"://product分類 第二層 (edit、insert)
                    var secClass = JObj.DBPost<ProductClassName>(n.http);
                    if (JObj.EnumList<ProductStore>().Where(n => n.value == secClass.PC_store).Count() > 0)
                    {
                        return new MGProductSelect().ProductCNEdit(n.mb, JObj.DBPost<ProductClassName>(n.http));
                    }
                    else
                    {
                        return JFormat.returnFail((int)SystemErrorCode.notExistID);
                    }
                case "productcslist"://product分類 第二層 list
                    return new MGProductSelect().ProductCNList(n.http.Request.Form["key"].ToString(), mg: true);
                #endregion

                #region 商品套餐
                case "productolist"://套餐List
                    return new MGProductSet().ProductSetOptionList(n.http.Request.Form["pkey"].ToString());
                case "productoadd"://套餐 add
                    return new MGProductSet().ProductSetOptionAdd(n.mb, JObj.des<List<String>>(n.http.Request.Form["ary"].ToString()), n.http.Request.Form["pkey"].ToString());
                case "productoedit"://套餐 edit
                    return new MGProductSet().ProductSetOptionEdit(n.mb, JObj.DBPost<ProductOptionSet>(n.http));
                case "productodel"://套餐 刪除
                    return new MGProductSet().ProductSetOptionEdit(n.mb, JObj.DBPost<ProductOptionSet>(n.http), del: true);
                case "productolistset"://套餐選擇商品
                    return new MGProductSearch().ProductList(n.mb, 999, 999, "333", n.http.Request.Form["ser"].ToString(), Convert.ToInt32(n.http.Request.Form["page"]), JObj.des<List<string>>(n.http.Request.Form["ary"].ToString()), mg: true);
                #endregion

                #region 商品推薦
                case "productgtlistset"://推薦選擇商品
                    return new MGProductSearch().ProductList(n.mb, 999, 999, "333", n.http.Request.Form["ser"].ToString(), Convert.ToInt32(n.http.Request.Form["page"]), giftModel: 3, mg: true);
                case "productgfck"://商品推薦或取消
                    return new MGProductGift().ProductGiftCk(n.mb, JObj.des<List<String>>(n.http.Request.Form["ary"].ToString()));
                case "productgfnext"://商品推薦往下移
                    return new MGProductGift().ProductGiftNext(n.mb, n.http.Request.Form["key"].ToString(), n.http.Request.Form["key2"].ToString());
                case "productgprv"://商品推薦往上移
                    return new MGProductGift().ProductGiftPre(n.mb, n.http.Request.Form["key"].ToString(), n.http.Request.Form["key2"].ToString());
                case "productgtlist"://目前已設定商品推薦
                    return new MGProductSearch().ProductList(n.mb, 999, Convert.ToInt32(n.http.Request.Form["class"]), n.http.Request.Form["selfclass"].ToString(), n.http.Request.Form["ser"].ToString(), Convert.ToInt32(n.http.Request.Form["page"]), giftModel: 1, mg: true);
                #endregion
                case "pread"://單載商品
                    return new MGProductSearch().ProductDoc(Request.Form["key"].ToString(),mg:true);
                case "papprove"://審核通過
                    return new MGProductCodeCK().PApprove(n.mb, n.http.Request.Form["key"].ToString());
                case "papprovefail"://審核失敗
                    return new MGProductCodeCK().PApprove(n.mb, n.http.Request.Form["key"].ToString(), true, n.http.Request.Form["mark"].ToString());
                case "productappck"://審核異動通過
                    if (n.mb.MB_level == (int)MBLevel.systemMG)
                    {
                        return new MGProduct().ProductAppck(n.mb, n.http.Request.Form["key"].ToString());
                    }
                    return JFormat.nullApi();
            }
    
            return JFormat.nullApi();
        }));


        /// <summary>
        /// 商店主API
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        [HttpPost("mb/{id1?}/{id2?}")]
        public IActionResult MemberMG(String id1, String id2)
        => Json(new pubcolApp(this.HttpContext).actionApp(n =>
            {//會員商品上架
                n.limit = new List<MBLevel> { MBLevel.pay,MBLevel.MG };
                n.mobileType = false;
            }, HttpGetType.SuccessfullySin, n =>
            {
                switch (id1 ?? "")
                {
                    case "lang"://支持語系
                        return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), lang: true);
                    case "productcflist"://product分類 first list
                        return new MGProductSelect().ProductCNTList(display: true, ps: ProductStore.line,mg:true);
                    case "productcslist"://product分類 第二層 list
                        return new MGProductSelect().ProductCNList(Request.Form["key"].ToString(), display: true, ps: ProductStore.line, mg: true);
                    case "papprove"://申請商品審核
                        return new MGProductCodeCK().PApproveRun(n.mb, n.http.Request.Form["key"].ToString());
                    case "cacelpapprove"://取消商品審核
                        return new MGProductCodeCK().PApproveRunCancel(n.mb, n.http.Request.Form["key"].ToString());
                    case "productpre"://商品oder 往上
                        return new MGProduct().ProductPre(n.mb, n.http.Request.Form["key"].ToString(), n.http.Request.Form["key2"].ToString());
                    case "productnext"://商品oder 往下
                        return new MGProduct().ProductNext(n.mb, n.http.Request.Form["key"].ToString(), n.http.Request.Form["key2"].ToString());
                    case "productlist"://目前已設定商品
                        return new MGProductSearch().ProductList(n.mb, Convert.ToInt32(n.http.Request.Form["type"]), (int)ProductClass.product, n.http.Request.Form["selfclass"].ToString(), n.http.Request.Form["ser"].ToString(), Convert.ToInt32(n.http.Request.Form["page"]), mg: true);
                    case "productedit"://商品設定
                        return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http));
                    case "productinsert"://商品insert
                        var insertP = JObj.DBPost<Product>(n.http);
                        insertP.P_set = false;//店主無此功能阻擋
                        insertP.P_key = "-insert-";
                        return new MGProduct().ProdcutEdit(n.mb, insertP);
                    case "productonoff"://起用禁用購買(使用者)
                        return new MGProduct().ProductOnOff(n.mb, n.http.Request.Form["key"].ToString());
                    case "productremove"://刪除商品
                        return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), del: true);
                    case "pincount"://增加庫存
                        return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), inCount: true);
                    case "pdecount"://減少庫存
                        return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), deCount: true);
                    case "limitcount"://購買數量限制
                        return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http),countLimit:true);
                    case "ybe"://video youtube
                        return new MGProduct().ProdcutEdit(n.mb, JObj.DBPost<Product>(n.http), ybe:true);
                    #region 商品圖片
                    case "delimg"://刪除商品圖片
                        return new MGProductImg().RemovePhoto(n.mb, n.http.Request.Form["key"].ToString(), JObj.des<List<string>>(n.http.Request.Form["imgary"].ToString()));
                    case "imgupload"://圖片碎片上傳
                        return new MGProductImg().imgUploadData(n.mb, JObj.DBPost<uploadImg>(n.http));
                    case "imgpre"://圖片 上移
                        return new MGProductImg().docImgPre(n.mb, key: n.http.Request.Form["key"].ToString(), imgPath: n.http.Request.Form["imgpath"].ToString());
                    case "imgnext"://圖片 下移
                        return new MGProductImg().docImgNext(n.mb, key: n.http.Request.Form["key"].ToString(), imgPath: n.http.Request.Form["imgpath"].ToString());
                    #endregion
                    case "doc"://商品簡述
                        return new MGProduct().PDEdit(n.mb, pkey: n.http.Request.Form["key"].ToString(), n.http.Request.Form["content"].ToString(), Convert.ToInt32(n.http.Request.Form["langnu"]));
                }
                return JFormat.nullApi();
            }));
    }
}