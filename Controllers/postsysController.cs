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
    /// <summary>
    /// post system
    /// </summary>
    [Route("psys/")]
    public class postsysController : Controller
    {
        IHttpClientFactory _hcf;
        public postsysController(IHttpClientFactory hcf)
        {
            _hcf = hcf;
        }

        
        [HttpPost("mg/{id1?}/{id2?}")]
        public IActionResult MemberMG(String id1, String id2)
        {

            return Json(new pubcolApp(this.HttpContext).actionApp(n =>
            {
                n.limit = new List<MBLevel> { MBLevel.systemMG,MBLevel.Edit};
                n.mobileType = false;
            }, HttpGetType.SuccessfullySin, n =>
            {
                switch (id1 ?? "")
                {
                    case "pset"://取得套餐目前內容清單
                        return new MGPostSys().setProductClass(n.http.Request.Form["key"].ToString());
                    case "productcflist"://product分類 first list
                        return new MGProductSelect().ProductCNTList(display:true,ps: ProductStore.store);
                    case "productcslist"://product分類 第二層 list
                        return new MGProductSelect().ProductCNList(n.http.Request.Form["key"].ToString(), display: true, ps: ProductStore.store);
                    case "productlist"://目前已設定商品
                        return new MGPostSys().ProductList(n.http.Request.Form["selfclass"].ToString(), n.http.Request.Form["ser"].ToString(), filter: Convert.ToBoolean(n.http.Request.Form["filter"]), JObj.EnumList<giftOptin>().Where(a => a.value == Convert.ToInt32(n.http.Request.Form["gift"])).FirstOrDefault().value, Convert.ToInt32(n.http.Request.Form["page"]));
                    case "productchoose"://client購物車內容
                        return new ProductContainer().PCCarList(JObj.des<List<string>>(Request.Form["ary"].ToString()),mg:true);

                }
                return JFormat.nullApi();
            }));
        }
    }
}
                    
