using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using productCar.Models;
using pubclass;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace productCar.Controllers
{
    [Route("pb/")]
    public class publicController : Controller
    {
        IHttpClientFactory _hcf;
        public publicController(IHttpClientFactory hcf)
        {
            _hcf = hcf;
        }
        [HttpPost("product/{id1?}/{id2?}")]//取商品描述
        public IActionResult pb(String id1, String id2)
        {
            try
            {
                switch (id1 ?? "")
                {
                    case "read"://單載商品
                        return Json(new MGProductSearch().ProductDoc(Request.Form["key"].ToString()));
                    case "doc"://取商品描述
                        return Json(new MGProduct().PDRead(Request.Form["key"].ToString(), Convert.ToInt32(Request.Form["nu"])));
                    case "img"://讀取所有圖片陣列
                        return Json(new MGProductImg().ImgRead(Request.Form["key"].ToString()));
                }
                return Json(JFormat.nullApi());
            }
            catch
            {
                return Json(JFormat.nullApi());
            }
        }
    }
}
