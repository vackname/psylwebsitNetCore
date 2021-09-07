using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dapper;
using DBConfig;
using pubclass;

namespace productCar.Models
{
    /// <summary>
    /// 商品圖片
    /// </summary>
    public class MGProductImg
    {
        /// <summary>
        /// 讀取商品陣列
        /// </summary>
        /// <param name="pkey"></param>
        /// <returns></returns>
        public object ImgRead(String pkey)
        {
            string bestPath = Path.Combine(MemoryModel.filePath, $"{pkey}.json");
            List<String> data = new List<string>();
            if (File.Exists(bestPath))
            {
                data = JObj.readFile<List<string>>(bestPath, false);
            }
            return new { error = (int)SystemErrorCode.Null, data = data };
        }
        /// <summary>
        /// 移除圖片
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="imgAry"></param>
        /// <returns></returns>
        public object RemovePhoto(Member mb, String pkey, List<String> imgAry) =>
        new DBServer().open((cn, db) =>
        {
            var getData = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1", new { key = pkey }).FirstOrDefault();
            if (getData != null)
            {
                if ((getData.P_ck == "chiefadmin" && new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                //系統管理者建立才可以編緝
                || (getData.P_ck == "" && new List<int>() { (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1)
                //非系統管理者建立 需取消送審才可以編緝
                )
                {

                    string bestPath = MemoryModel.filePath;
                    if (Directory.Exists(bestPath))
                    {//圖片陣列
                        if (File.Exists(Path.Combine(bestPath, $"{pkey}.json")))
                        {
                            var imgAryDB = JObj.readFile<List<string>>(Path.Combine(bestPath, $"{pkey}.json"), false);
                            foreach (var img in imgAry)
                            {
                                if (imgAryDB.IndexOf(img) > -1)
                                {
                                    if (File.Exists(Path.Combine(bestPath, img)))
                                    {
                                        File.Delete(Path.Combine(bestPath, img));
                                    }
                                }
                            }
                            imgAryDB = imgAryDB.Where(n => imgAry.IndexOf(n) == -1).ToList();

                            JObj.writeFile<List<string>>(imgAryDB, Path.Combine(bestPath, $"{pkey}.json"), root: false); ;
                            new DBServer(write: true).open((cn2, db2) =>
                            {
                                db2.UpdateStr<Product>(new
                                {
                                    P_key = pkey,
                                    P_mark = $"[{mb.MB_Account}]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")} Remove img)"
                                });
                            });

                            int usCount = 0;
                            if (mb.MB_level == (int)MBLevel.MG)
                            {//還點

                                var getMB = cn.Query<Member>("SELECT * FROM Member WHERE MB_Account = @ac LIMIT 0,1", new { ac = mb.MB_Account }).FirstOrDefault();
                                new DBServer(write: true).open((cn2, db2) =>
                                {
                                    getMB.MB_Count += 6000;
                                    getMB.MB_usCount -= 6000;
                                    db2.UpdateStr<Member>(new { mb.MB_Account, getMB.MB_Count, getMB.MB_usCount });
                                    usCount = getMB.MB_usCount;
                                });
                                return new { error = (int)SystemErrorCode.Null, data = imgAryDB, point = getMB.MB_Count, repoint = getMB.MB_usCount };

                            }
                            else
                            {

                                return new { error = (int)SystemErrorCode.Null, data = imgAryDB };
                            }
                        }
                    }
                    return new { error = (int)SystemErrorCode.Null, data = new List<string>() };
                }

                return JFormat.returnFail((int)SystemErrorCode.limit);

            }

            return JFormat.returnFail((int)SystemErrorCode.limit);
        }, e => JFormat.returnFail());

        /// <summary>
        /// 圖片上移
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="key"></param>
        /// <param name="imgPath">圖片path</param>
        /// <returns></returns>
        public object docImgPre(Member mb, string key, string imgPath) =>
        new DBServer().open((cn, db) =>
        {
            var getData = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1", new { key }).FirstOrDefault();
            if (getData != null)
            {
                if ((getData.P_ck == "chiefadmin" && new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                //系統管理者建立才可以編緝
                || (getData.P_ck == "" && new List<int>() { (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1 && getData.MB_uid == mb.MB_uid)
                //非系統管理者建立 需取消送審才可以編緝
                )
                {
                    string bestPath = MemoryModel.filePath;
                    List<string> imgAry = JObj.readFile<List<string>>(Path.Combine(bestPath, $"{getData.P_key}.json"), root: false);

                    if (imgAry.IndexOf(imgPath) > 0)
                    {
                        getData.P_mark = $"[{mb.MB_Account}:pre img]{key}-{imgPath}-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                        List<string> reOder = new List<string>();
                        foreach (var n in imgAry)
                        {
                            if (imgPath != n)
                            {
                                if (((reOder.IndexOf(imgPath) == -1) ? imgAry.IndexOf(imgPath) - 1 == reOder.Count() : false))
                                {
                                    reOder.Add(imgPath);
                                }
                                reOder.Add(n);
                            }
                        }
                        if (imgAry.Count() == reOder.Count())
                        {
                            imgAry = reOder;
                            JObj.writeFile<List<string>>(imgAry, Path.Combine(bestPath, $"{getData.P_key}.json"), root: false);
                            new DBServer(write: true).open((cn2, db2) =>
                            {
                                db2.UpdateStr<Product>(new { getData.P_key, getData.P_mark });
                            });
                        }
                    }
                    return new { error = (int)SystemErrorCode.Null, data = imgAry };
                }
                else
                {
                    return JFormat.returnFail((int)SystemErrorCode.limit);
                }
            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.limit);
            }
        }, e => JFormat.returnFail());

        /// <summary>
        /// 圖片下移
        /// </summary>
        /// <param name="mb"></param>
        /// <param name="key"></param>
        /// <param name="imgPath">圖片path</param>
        /// <returns></returns>
        public object docImgNext(Member mb, string key, string imgPath) =>
        new DBServer().open((cn, db) =>
        {
            var getData = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1", new { key }).FirstOrDefault();
            if (getData != null)
            {
                if ((getData.P_ck == "chiefadmin" && new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                //系統管理者建立才可以編緝
                || (getData.P_ck == "" && new List<int>() { (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1 && getData.MB_uid == mb.MB_uid)
                //非系統管理者建立 需取消送審才可以編緝
                )
                {
                    string bestPath = MemoryModel.filePath;
                    List<string> imgAry = JObj.readFile<List<string>>(Path.Combine(bestPath, $"{getData.P_key}.json"), root: false);

                    if (imgAry.IndexOf(imgPath) > -1)
                    {
                        if (imgAry.IndexOf(imgPath) != imgAry.Count() - 1)
                        {
                            getData.P_mark = $"[{mb.MB_Account}:next img]{key}-{imgPath}-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                            List<string> reOder = new List<string>();
                            foreach (var n in imgAry)
                            {
                                if (imgPath != n)
                                {
                                    reOder.Add(n);
                                    if (((reOder.IndexOf(imgPath) == -1) ? imgAry.IndexOf(imgPath) + 1 == reOder.Count() : false))
                                    {
                                        reOder.Add(imgPath);
                                    }
                                }
                            }
                            if (imgAry.Count() == reOder.Count())
                            {
                                imgAry = reOder;
                                JObj.writeFile<List<string>>(imgAry, Path.Combine(bestPath, $"{getData.P_key}.json"), root: false);
                                new DBServer(write: true).open((cn2, db2) =>
                                {
                                    db2.UpdateStr<Product>(new { getData.P_key, getData.P_mark });
                                });
                            }
                        }
                        return new { error = (int)SystemErrorCode.Null, data = imgAry };
                    }
                    return JFormat.returnFail((int)SystemErrorCode.limit);
                }
                else
                {
                    return JFormat.returnFail((int)SystemErrorCode.limit);
                }

            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.limit);
            }
        }, e => JFormat.returnFail());

        /// <summary>
        /// 商品 圖片上傳 匹次
        /// </summary>
        /// <param name="mb">使用者</param>
        /// <param name="content">upload</param>
        /// <returns></returns>
        public object imgUploadData(Member mb, uploadImg content) =>
        new DBServer().open((cn, db) =>
        {//會員照片謹一張 但仍以陣列儲存
            //圖片base 64
            string imgdata = content.extend.Where(n => n.Key == "imgdata").First().Value.ToString();

            //片段編號
            string tp = content.extend.Where(n => n.Key == "tp").First().Value.ToString();
            var getP = cn.Query<Product>("SELECT * FROM Product WHERE P_key=@key LIMIT 0,1", new { content.key }).FirstOrDefault();
            if (getP != null)
            {
                if ((getP.P_ck == "chiefadmin" && new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.Edit }.IndexOf(mb.MB_level) > -1)
                //系統管理者建立才可以上傳
                || (getP.P_ck == "" && new List<int>() { (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1 && getP.MB_uid == getP.MB_uid)
                //非系統管理者建立 需取消送審才可以上傳
                )
                {
                    uploadImg catchMemory = null;
                    bool ckMemory = true;
                    while (ckMemory)
                    {
                        try
                        {//modify 排除器
                            catchMemory = MemoryModel.uploadImg.Where(n => n.key == content.key && n.keyid == content.keyid).FirstOrDefault();
                            ckMemory = false;
                        }
                        catch
                        {
                            Thread.Sleep(200);
                        }
                    }

                    if (tp.IndexOf("complete") == 0)
                    {//已完整上傳
                        int nu_tp = Convert.ToInt32(tp.Replace("complete", ""));
                        if (catchMemory == null)
                        {//小於於上傳容量-建置
                            catchMemory = new uploadImg()
                            {
                                key = content.key,
                                keyid = content.keyid,
                                exist = new pub().unixDateTimeNow()
                            };
                        }
                        catchMemory.data.Add(imgdata);//final 碎塊
                        Thread.Sleep(50);//等候碎塊add
                                         //圖片格式確認是否異常
                        bool foramtCK = catchMemory.data.Count() - 1 == nu_tp && catchMemory.data.Where(n => n == "").Count() == 0;
                        if (foramtCK)
                        {
                            if (String.Join("", catchMemory.data).Split(',')[1].Trim().Length <= 3000000)
                            {//阻擋異常進入
                                int usCount = 0;
                                int point = 0;
                                if (mb.MB_level == (int)MBLevel.MG)
                                {//扣點
                                    bool pointLimit = false;
                                    new DBServer(write: true).open((cn2, db2) =>
                                    {
                                        var getMB = cn.Query<Member>("SELECT * FROM Member WHERE MB_Account = @ac LIMIT 0,1", new { ac = mb.MB_Account }).FirstOrDefault();
                                        if (getMB.MB_Count >= 8000)
                                        {
                                            getMB.MB_Count -= 8000;
                                            getMB.MB_usCount += 8000;
                                            db2.UpdateStr<Member>(new { mb.MB_Account, getMB.MB_Count, getMB.MB_usCount });
                                            usCount = getMB.MB_usCount;
                                            point = getMB.MB_Count;
                                        }
                                        else
                                        {//無額度清除
                                            pointLimit = true;
                                            catchMemory.data = new List<string>();//清除memory wait remove

                                        }
                                    });

                                    if (pointLimit)
                                    {//額度不足
                                        return JFormat.returnFail((int)SystemErrorCode.notpointError);
                                    }
                                }

                                string bestPath = MemoryModel.filePath;

                                if (!Directory.Exists(bestPath))
                                {//建置圖片放置地
                                    Directory.CreateDirectory(bestPath);
                                    Thread.Sleep(50);
                                }

                                #region save new img
                                MemoryStream memoryStream = new MemoryStream();
                                var bytes = Convert.FromBase64String(String.Join("", catchMemory.data).Split(',')[1].Trim());
                                string FileName = $"{content.key}{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}_{catchMemory.keyid.Split('_')[1]}.{catchMemory.data[0].Split(',')[0].Trim().Split(':')[1].Split('/')[1].Split(';')[0].Replace("jpeg", "jpg")}";

                                using (Stream fileStream = new FileStream(Path.Combine(bestPath, FileName), FileMode.Create))
                                {
                                    new MemoryStream(bytes).CopyTo(fileStream);
                                }

                                List<String> catchImg = new List<string>() { FileName };
                                if (File.Exists(Path.Combine(bestPath, $"{content.key}.json")))
                                {//舊圖片
                                    var getData = JObj.readFile<List<string>>(Path.Combine(bestPath, $"{content.key}.json"), false);
                                    foreach (var n in getData)
                                    {
                                        catchImg.Add(n);
                                    }

                                }
                                JObj.writeFile<List<string>>(catchImg, Path.Combine(bestPath, $"{content.key}.json"), root: false);
                                #endregion

                                getP.P_mark = $"[{mb.MB_Account}:Save img Product]{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
                                new DBServer(write: true).open((cn2, db2) =>
                                {
                                    getP.MB_appCK = (new List<int>() { (int)MBLevel.systemMG, (int)MBLevel.pay, (int)MBLevel.MG }.IndexOf(mb.MB_level) > -1);
                                    db2.UpdateStr<Product>(new { getP.P_key, getP.MB_appCK, getP.P_mark });
                                });
                                catchMemory.data = new List<string>();//清除memory wait remove

                                if (mb.MB_level == (int)MBLevel.MG)
                                {//有扣點
                                    return new { error = (int)SystemErrorCode.Null, data = catchImg, newImg = FileName, point = point, repoint = usCount };
                                }
                                else
                                {
                                    return new { error = (int)SystemErrorCode.Null, data = catchImg, newImg = FileName };
                                }
                            }
                            catchMemory.data = new List<string>();//清除memory wait remove
                            return JFormat.returnFail((int)SystemErrorCode.limit);
                        }
                        else
                        {
                            return JFormat.returnFail((int)SystemErrorCode.fileForamtError);
                        }
                    }
                    else
                    {//儲存繼傳-回補
                        if (catchMemory == null)
                        {//fisrt 進入
                            var insert = new uploadImg()
                            {
                                key = content.key,
                                keyid = content.keyid,
                                exist = new pub().unixDateTimeNow()
                            };

                            int nu_tp = Convert.ToInt32(tp);
                            while (insert.data.Count() < nu_tp)
                            {//補區段
                                insert.data.Add("");
                            }
                            insert.data.Add(imgdata);
                            MemoryModel.uploadImg.Add(insert);
                        }
                        else
                        {
                            int nu_tp = Convert.ToInt32(tp);
                            if (catchMemory.data.Count() < nu_tp)
                            {
                                while (catchMemory.data.Count() < nu_tp)
                                {//補區段
                                    catchMemory.data.Add("");
                                }
                                catchMemory.data.Add(imgdata);
                            }
                            else if (catchMemory.data.Count() == nu_tp)
                            {//往後新增
                                catchMemory.data.Add(imgdata);
                            }
                            else
                            {//區段位差異位補位-重建
                                int rownu = 0;
                                List<string> createData = new List<string>();
                                foreach (var n in catchMemory.data)
                                {
                                    if (rownu == nu_tp)
                                    {
                                        createData.Add(imgdata);
                                    }
                                    else
                                    {
                                        createData.Add(n);
                                    }
                                    rownu++;
                                }
                                catchMemory.data = createData;
                            }

                            catchMemory.exist = new pub().unixDateTimeNow();
                        }


                        return JFormat.returnOk();
                    }
                }
                else
                {

                    return JFormat.returnFail((int)SystemErrorCode.limit);
                }
            }
            else
            {
                return JFormat.returnFail((int)SystemErrorCode.notExistID);
            }
        }, e => JFormat.returnFail());
    }
}
