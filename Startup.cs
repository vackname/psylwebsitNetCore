using System.Collections.Generic;
using System.IO;
using DBConfig;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using productCar.Models;
using pubclass;

namespace productCar
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddControllers().AddJsonOptions(o => {
                o.JsonSerializerOptions.PropertyNamingPolicy = null;//大小寫不設定(JSON)
                o.JsonSerializerOptions.WriteIndented = false;//json 列做縮排
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            VueBundleMiddleWare.FileStatic = (context, f, c) =>
            {
                string[] SplitStr = context.Request.Path.Value.Split('/');
                if (context.Request.Path.Value.IndexOf("/igptout") == 0)
                {//取真實圖片
                    return new List<VueFileStatic>() {
                        new VueFileStatic()
                        {
                            HttpPathList = new List<string>(){"/igptout" },//隱藏圖片真實地址
                            FilePath=Path.Combine(MemoryModel.filePath,SplitStr[2])//真實位置
                        }
                    };
                }

                return new List<VueFileStatic>()
                {//匹次載入
                    new VueFileStatic() {
                        MaxSize=1572864,//設限1 MB 大小(最大值3.5MB response)
                        HttpPathList = new List<string>(){"/igpt" },//隱藏圖片真實地址
                        FilePath=MemoryModel.filePath//真實位置
                    }
                };
            };

            app.UseMiddleware<VueBundleMiddleWare>();
            app.UseMiddleware<verPubDB>();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
