using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DBConfig;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using productCar.Models;
using pubclass;

namespace productCar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            enviModel.PJ.process.Add(new processMes { name = "productCar", order = (int)processType.app });
            enviModel.PJ.same = "pc";
            enviModel.PJ.title = "Procut City";

            #region catchDB config
            try
            {
                string root = System.AppDomain.CurrentDomain.BaseDirectory;

                #region file 放置地
                if (File.Exists(Path.Combine(root, "sysctr", "filepath.txt")))
                {
                    JObj.ReadTxt(Path.Combine("sysctr", "filepath.json"));
                    MemoryModel.filePath = JObj.ReadTxt(Path.Combine("sysctr", "filepath.txt"));
                }
                else
                {
                    MemoryModel.filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "product");
                }
                #endregion

                #region write DB
                if (File.Exists(Path.Combine(root, "sysctr", "ex_cf_write.json")))
                {
                    var getConfig = JObj.doc(JObj.ReadTxt(Path.Combine("sysctr", "ex_cf_write.json")));
                    try
                    {
                        pubConfigDB.setWrite.DBName = getConfig.getKey<String>("DBName").val;

                        pubConfigDB.setWrite.port = getConfig.getKey<String>("port").val;

                        pubConfigDB.setWrite.password = Encoding.UTF8.GetString(Convert.FromBase64String(getConfig.getKey<String>("pw").val));

                        pubConfigDB.setWrite.user = getConfig.getKey<String>("user").val;

                        pubConfigDB.setWrite.server = getConfig.getKey<String>("ip").val;

                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"error format DB config(ex_cf.json)");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    string posts = Path.Combine(root, "sysctr");

                    if (!Directory.Exists(posts))
                    {//資料夾不存在
                     //新增資料夾
                        Directory.CreateDirectory(posts);
                    }

                    var fileCf = Path.Combine(posts, "ex_cf_write.json");
                    if (!System.IO.File.Exists(fileCf))
                    {//DB config 未存在
                        pubConfigDB.setWrite.DBName = "psylDB";
                        pubConfigDB.setWrite.user = "ppguser";
                        pubConfigDB.setWrite.password = "qwe1234Q!";
                        pubConfigDB.setWrite.port = "3306";
                        pubConfigDB.setWrite.server = "192.168.33.12";
                        JObj.writeFile(new
                        {
                            DBName = pubConfigDB.setWrite.DBName,
                            port = pubConfigDB.setWrite.port,
                            pw = Convert.ToBase64String(Encoding.UTF8.GetBytes(pubConfigDB.setWrite.password)),
                            user = pubConfigDB.setWrite.user,
                            ip = pubConfigDB.setWrite.server,
                        }, fileCf, false, true);
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"not exist DB config");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                #endregion
                if (File.Exists(Path.Combine(root, "sysctr", "ex_cf.json")))
                {
                    var getConfig = JObj.doc(JObj.ReadTxt(Path.Combine("sysctr", "ex_cf.json")));
                    try
                    {
                        pubConfigDB.set.DBName = getConfig.getKey<String>("DBName").val;

                        pubConfigDB.set.port = getConfig.getKey<String>("port").val;

                        pubConfigDB.set.password = Encoding.UTF8.GetString(Convert.FromBase64String(getConfig.getKey<String>("pw").val));

                        pubConfigDB.set.user = getConfig.getKey<String>("user").val;

                        pubConfigDB.set.server = getConfig.getKey<String>("ip").val;

                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"error format DB config(ex_cf.json)");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    string posts = Path.Combine(root, "sysctr");

                    if (!Directory.Exists(posts))
                    {//資料夾不存在
                     //新增資料夾
                        Directory.CreateDirectory(posts);
                    }


                    var fileCf = Path.Combine(posts, "ex_cf.json");
                    if (!System.IO.File.Exists(fileCf))
                    {//DB config 未存在
                        pubConfigDB.set.DBName = "psylDB";
                        pubConfigDB.set.user = "ppguser";
                        pubConfigDB.set.password = "qwe1234Q!";
                        pubConfigDB.set.port = "3306";
                        pubConfigDB.set.server = "192.168.33.12";
                        JObj.writeFile(new
                        {
                            DBName = pubConfigDB.set.DBName,
                            port = pubConfigDB.set.port,
                            pw = Convert.ToBase64String(Encoding.UTF8.GetBytes(pubConfigDB.set.password)),
                            user = pubConfigDB.set.user,
                            ip = pubConfigDB.set.server,
                        }, fileCf, false, true);
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"not exist DB config");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"catch DB config fail");
                Console.ForegroundColor = ConsoleColor.White;
            }

            #endregion

            try
            {
                if (new DBServer(write: true).dbConnectionCK().Item1)
                {
                    new DBMigration(pubConfigDB.setWrite).CreateTB<Product>();
                    new DBMigration(pubConfigDB.setWrite).CreateTB<ProductDiscount>();
                    new DBMigration(pubConfigDB.setWrite).CreateTB<usApp>();
                    new DBMigration(pubConfigDB.setWrite).CreateTB<usAppBindAccount>();
                    new DBMigration(pubConfigDB.setWrite).CreateTB<ProductClassNameTitle>();
                    new DBMigration(pubConfigDB.setWrite).CreateTB<ProductClassName>();
                    new DBMigration(pubConfigDB.setWrite).CreateTB<ProductOptionSet>();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("DB not connection!!!");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }

            try
            {
                if (new DBServer(write: true).dbConnectionCK().Item1)
                {
                    new DBServer(write: true).open((cn, db) =>
                    {
                        var mb = cn.Query<Member>("SELECT * FROM Member WHERE MB_Account = @code", new { code = "chiefAdmin" }).FirstOrDefault();
                        if (mb != null)
                        {
                                #region create Product
                                var pt = cn.Query<Product>("SELECT * FROM Product WHERE P_key in @ary", new
                            {
                                ary = new List<String>() {
                                "a",//product count bind
                                "b",//product date
                                "c",
                                "d"
                                }
                            });//catch product

                                if (pt.Where(n => n.P_key == "a").FirstOrDefault() == null)
                            {
                                db.InsertStr<Product>(new Product()
                                {
                                    P_key = "a",
                                    P_type = (int)ProductType.NotLimit,
                                    P_Name = "[\"app 點數\"]",
                                    MB_appCK = true,
                                    P_ck = "chiefadmin",
                                    P_cash = 0.001,
                                    P_Count = 0,
                                    P_unit = "[\"個\"]",
                                    P_date = new pub().unixDateTimeNow(),
                                    P_order = 1,
                                    MB_uid = mb.MB_uid,
                                });
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"create product successfully!(App點數)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"exist product bind (app 點數)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            if (pt.Where(n => n.P_key == "b").FirstOrDefault() == null)
                            {
                                new DB(cn).InsertStr<Product>(new Product()
                                {
                                    P_key = "b",
                                    P_type = (int)ProductType.NotLimit,
                                    P_Name = "[\"App期限\"]",
                                    MB_appCK = true,
                                    P_ck = "chiefadmin",
                                    P_cash = 30,//單位天
                                        P_Count = 0,
                                    P_unit = "[\"天\"]",
                                    P_date = new pub().unixDateTimeNow(),
                                    P_order = 2,
                                    MB_uid = mb.MB_uid,
                                });
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"create product successfully!(app 期限)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"exist product (app 期限)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            if (pt.Where(n => n.P_key == "c").FirstOrDefault() == null)
                            {
                                db.InsertStr<Product>(new Product()
                                {
                                    P_key = "c",
                                    P_type = (int)ProductType.NotLimit,
                                    MB_appCK = true,
                                    P_ck = "chiefadmin",
                                    P_Name = "[\"post System\"]",
                                    P_class = (int)ProductClass.pApp,
                                    P_cash = 300,
                                    P_Count = 0,
                                    P_unit = "[\"app\"]",
                                    P_date = new pub().unixDateTimeNow(),
                                    P_order = 3,
                                    MB_uid = mb.MB_uid,
                                }); ; ;
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"create product successfully!(post System)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"exist product (post System)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            if (pt.Where(n => n.P_key == "d").FirstOrDefault() == null)
                            {
                                db.InsertStr<Product>(new Product()
                                {
                                    P_key = "d",
                                    P_type = (int)ProductType.NotLimit,
                                    MB_appCK = true,
                                    P_ck = "chiefadmin",
                                    P_Name = "[\"期貨\"]",
                                    P_class = (int)ProductClass.pApp,
                                    P_cash = 300,//單位
                                        P_Count = 0,
                                    P_unit = "[\"app\"]",
                                    P_date = new pub().unixDateTimeNow(),
                                    P_order = 4,
                                    MB_uid = mb.MB_uid,
                                });
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"create product successfully!(futures");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"exist product (futures)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                                #endregion
                            }
                    }, e =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"catch product create fail");
                        Console.ForegroundColor = ConsoleColor.White;
                    });
                }
            }
            catch
            {

            }
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    long nowTime = new pub().unixDateTimeNow();
                    try
                    {//移除續傳圖片
                        if (MemoryModel.uploadImg.Where(n => nowTime - n.exist > 600).Count() > 0)
                        {
                            MemoryModel.uploadImg = MemoryModel.uploadImg.Where(n => nowTime - n.exist <= 600).ToList();
                        }
                    }
                    catch
                    {

                    }

                    try
                    {

                        if (new DBServer().dbConnectionCK().Item1)
                        {//同步資料庫
                            new DBServer().open((cn, db) =>
                            {//取得記憶體分類(product)
                                MemoryModel.pc = cn.Query<ProductClassName>($"SELECT * FROM ProductClassName WHERE PC_display = TRUE").Select(n => { n.PC_mark = ""; return n; }).ToList();
                                MemoryModel.pct = cn.Query<ProductClassNameTitle>($"SELECT * FROM ProductClassNameTitle WHERE PCT_display = TRUE").Select(n => { n.PCT_mark = ""; return n; }).ToList();
                            });
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("DB not connection!!!");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                    catch
                    {

                    }

                    Thread.Sleep(3699);
                }
            });
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}