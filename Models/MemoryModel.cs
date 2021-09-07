using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 上傳碎檔
    /// </summary>
    public class uploadImg
    {
        /// <summary>
        /// 商品key
        /// </summary>
        [DBAPI]
        public string key { set; get; }
        /// <summary>
        /// 已上傳圖片 base64 片段
        /// </summary>
        public List<string> data { set; get; } = new List<string>();
        /// <summary>
        /// 圖片key id
        /// </summary>
        [DBAPI]
        public string keyid { set; get; }
        /// <summary>
        /// 圖片開始上傳
        /// </summary>
        public long exist { set; get; }

        [JsonExtensionData]
        public Dictionary<String, Object> extend { set; get; } = new Dictionary<String, Object>();
    }

    public static class MemoryModel
    {
        /// <summary>
        /// 檔案放置地
        /// </summary>
        public static string filePath = "";
        /// <summary>
        /// 碎檔上傳
        /// </summary>
        public static List<uploadImg> uploadImg = new List<uploadImg>();
        /// <summary>
        /// 商品first分類
        /// </summary>
        public static List<ProductClassNameTitle> pct = new List<ProductClassNameTitle>();
        /// <summary>
        /// 商品分類二次
        /// </summary>
        public static List<ProductClassName> pc = new List<ProductClassName>();

    }
}
