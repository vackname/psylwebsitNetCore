using System;
using System.Collections.Generic;
using System.Net.Http;
using DBConfig;
using Microsoft.AspNetCore.Mvc;
using productCar.Models;
using pubclass;

namespace productCar.Controllers
{
    /// <summary>
    /// 商城
    /// </summary>
    [Route("pcar/")]
    public class pcarController : Controller
    {
        IHttpClientFactory _hcf;
        public pcarController(IHttpClientFactory hcf)
        {
            _hcf = hcf;
        }


        [HttpPost("mg/{id1?}/{id2?}")]
        public IActionResult MemberMG(String id1, String id2)
        {
            try
            {
                switch (id1 ?? "")
                {
                    case "productcflist"://product分類 first list
                        return Json(new MGProductSelect().ProductCNTList(display: true, ps: ProductStore.line));
                    case "productcslist"://product分類 第二層 list
                        return Json(new MGProductSelect().ProductCNList(Request.Form["key"].ToString(), display: true, ps: ProductStore.line));
                    case "productlist"://目前已設定商品
                        return Json(new ProductContainer().ProductList(Request.Form["selfclass"].ToString(), Request.Form["ser"].ToString(), filter: Convert.ToBoolean(Request.Form["filter"]), Convert.ToInt32(Request.Form["page"])));
                    case "productchoose"://client購物車內容
                        return Json(new ProductContainer().PCCarList(JObj.des<List<string>>(Request.Form["ary"].ToString())));
                    default:
                        return Json(JFormat.nullApi());
                }
            }
            catch
            {
                return Json(JFormat.returnFail((int)SystemErrorCode.limit));
            }
        }
    }
}
