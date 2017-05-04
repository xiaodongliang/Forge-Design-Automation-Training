using AIO.ACES.Models;
using AIO.Operations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MyTestDesignAutomation
{
    class ApppackTest
    {
        static public void ApppackTestMain()
        {
            //获取Token
            Container container = new Container(new Uri("https://developer.api.autodesk.com/autocad.io/us-east/v2/"));
            var token = GetToken();

            //将口令设置到随后所有Design Automation 相关请求的HTTP头
            container.SendingRequest2 += (sender, e) => e.RequestMessage.SetHeader(
              "Authorization",
              token);

            //接下来可以进行Design Automation 的其它操作
            //.....


            //删除某AppPackage
            string myAppPackName = "MyTestAppPackName";
            string myAct_with_Pack_Name = "MyActivityWithAppPack";
            string myAppPackBundleName = "MyTest.zip";

            //将AutoCAD插件打包为bundle
            CreateZip(myAppPackBundleName);

            //删除某AppPackage
            DeletePackage(container, myAppPackName);
            //创建某AppPackage
            AppPackage oAppPack = null;
            CreatePackage(container,
                myAppPackBundleName,
                myAppPackName, oAppPack);
            //删除某Activity
            DeleteActivity(container, myAct_with_Pack_Name);

            //创建某普通Activity，和AppPackage关联
            CreateOneActivityWithPackage(container, myAct_with_Pack_Name, myAppPackName);
            //执行任务 （ WorkItem）
            CreateWorkItem(container, myAct_with_Pack_Name);
        }


        static string GetToken()
        {
            using (var client = new HttpClient())
            {
                //配置HTTP请求。其中有两个参数是开发者的API key和 secret
                var values = new List<KeyValuePair<string, string>>();

                values.Add(new KeyValuePair<string, string>(
                    "client_id",
                    Credentials.ConsumerKey));

                values.Add(new KeyValuePair<string, string>(
                    "client_secret",
                    Credentials.ConsumerSecret));

                values.Add(new KeyValuePair<string, string>(
                    "grant_type",
                    "client_credentials"));

                var requestContent = new FormUrlEncodedContent(values);

                //向AutoCADI/O发出认证请求
                //服务端点地址为: https://developer.api.autodesk.com/authentication/v1/authenticate

                var response = client.PostAsync(
                    "https://developer.api.autodesk.com/authentication/v1/authenticate",
                    requestContent).Result;

                //得到响应，查看返回结果
                var responseContent = response.Content.ReadAsStringAsync().Result;

                //解析返回的Json字串
                var resValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    responseContent);
                //提取出口令（token）
                return resValues["token_type"] + " " + resValues["access_token"];
            }
        }

        // 删除某Activity        
        private static void DeleteActivity(Container container, string actId)
        {
            Console.WriteLine("正在删除某Activity...");

            Activity activity = null;
            try
            {
                activity = container.Activities.ByKey(actId).GetValue();
            }
            catch { }

            if (activity != null)
            {
                container.DeleteObject(activity);
                container.SaveChanges();
                activity = null;
            }
        }

        //将AutoCAD插件打包为bundle
        static void CreateZip(string zipname)
        {
            Console.WriteLine("正在将AutoCAD插件打包为bundle...");

            if (System.IO.File.Exists(zipname))
                System.IO.File.Delete(zipname);
            using (var archive = ZipFile.Open(zipname, ZipArchiveMode.Create))
            {
                string bundle = zipname + ".bundle";
                string name = "PackageContents.xml";
                archive.CreateEntryFromFile(name, System.IO.Path.Combine(bundle, name));
                name = "PackageNetPlugin.dll";
                archive.CreateEntryFromFile(name, System.IO.Path.Combine(bundle, "Contents", name));
                name = "Newtonsoft.Json.dll";
                archive.CreateEntryFromFile(name, System.IO.Path.Combine(bundle, "Contents", name));
                // name = "RestSharp.dll";
                // archive.CreateEntryFromFile(name, System.IO.Path.Combine(bundle, "Contents", name));
            }
        }

        //上载AppPackage的文件包到AWS
        static void UploadObject(string url, string filePath)
        {
            Console.WriteLine("正在上载AppPackage的文件包到AWS...");

            using (var client = new HttpClient())
            {
                client.PutAsync(
                  url,
                  new StreamContent(File.OpenRead(filePath))
                ).Result.EnsureSuccessStatusCode();
            }
        }

        //删除AppPackage
        static void DeletePackage(Container container, string appPackName)
        {
            Console.WriteLine("正在删除AppPackage...");

            AppPackage package = null;
            try
            {
                package =
                  container.AppPackages.Where(
                    a => a.Id == appPackName
                  ).FirstOrDefault();


            }
            catch { }

            if (package != null)
            {
                container.DeleteObject(package);
                container.SaveChanges();
                package = null;
            }

            Console.WriteLine("已删除AppPackage!");

        }

        //创建AppPackage
        static AppPackage CreatePackage(Container container,
                                                string zip,
                                                string appPackName,
                                                AppPackage package)
        {
            Console.WriteLine("正在创建AppPackage...");

            // 第一步：获取上载AppPackage的URL

            var url = container.AppPackages.GetUploadUrl().GetValue();

            // 第二步：上载AppPackage到云存储

            Console.WriteLine("正在上载AppPackage的zip...");
            UploadObject(url, zip);

            if (package == null)
            {
                // 第三步，创建一个AppPackage对象

                package = new AppPackage()
                {
                    Id = appPackName,
                    Version = 1,
                    RequiredEngineVersion = "21.0",
                    Resource = url
                };
                container.AddToAppPackages(package);
            }

            container.SaveChanges();

            Console.WriteLine("创建AppPackage成功");

            return package;
        }

        //创建某关联AppPackage的Activity
        static Activity CreateOneActivityWithPackage(Container container,
                                                    string actId,
                                                    string appPackName)
        {
            Console.WriteLine("正在创建某关联AppPackage的Activity...");

            var activity = new Activity()
            {
                Id = actId,
                Version = 1,
                Instruction = new Instruction()
                {
                    //普通测试
                    Script = "MyPluginCommand _save result.dwg\n"

                },
                Parameters = new Parameters()
                {
                    InputParameters =
                    {
                        new Parameter()
                        {
                          Name = "HostDwg", LocalFileName = "$(HostDwg)"
                        }
                     },
                    OutputParameters = {
                    new Parameter()
                    {
                      //普通测试
                      Name = "Result", LocalFileName = "Result.dwg"

                    }
                  }
                },
                RequiredEngineVersion = "21.0"
            };

            //关联某AppPackage
            activity.AppPackages.Add(appPackName);

            container.AddToActivities(activity);
            container.SaveChanges();

            Console.WriteLine("正在创建某关联AppPackage的Activity成功！");


            return activity;
        }

        //创建Work Item
        static void CreateWorkItem(Container container, string actId)
        {
            Console.WriteLine("正在创建和启动Work Item...");


            //新建WorkItem
            var wi = new WorkItem()
            {
                Id = "", //必须为空，Design Automation 会自动分配Guid
                Arguments = new Arguments(),
                //用哪个Activity
                ActivityId = actId
            };
            //设置输入参数
            wi.Arguments.InputArguments.Add(new Argument()
            {
                //对应Activity哪个参数 
                Name = "HostDwg",
                //来源。      
                Resource = "http://forge-xd-generic-test.herokuapp.com/downloadDADrawing/TestDemo1.dwg",
                //Resource = "http://forge-xd-generic-test.herokuapp.com/downloadDADrawing/visualization_-_aerial.dwg",
                //非A360数据源
                StorageProvider = StorageProvider.Generic
            });
            //设置输出参数
            wi.Arguments.OutputArguments.Add(new Argument()
            {
                //对应Activity哪个参数 
                Name = "Result",
                //非A360数据源
                StorageProvider = StorageProvider.Generic,
                //HTTP动作 - 将结果放到对应的云存储
                HttpVerb = HttpVerbType.POST,
                //如果设置为null，则缺省放到Design Automation 存储空间里
                Resource = null 
            });

            //添加此WorkItem, 也就是开始启动任务
            container.AddToWorkItems(wi);
            container.SaveChanges();

            //看看AutoCAD I/O为WorkItem分配了什么Guid
            Console.WriteLine("Id= {0}", wi.Id);

            container.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;

            //等待，看看该任务WorkItem是否执行完毕
            do
            {
                System.Threading.Thread.Sleep(10000);
                wi = container.WorkItems.Where(p => p.Id == wi.Id).SingleOrDefault();
            }
            while (wi.Status == ExecutionStatus.Pending || wi.Status == ExecutionStatus.InProgress);


            //下载report，无论成功与否

            Console.WriteLine("The report is downloadable at {0}", wi.StatusDetails.Report);
            Until.DownloadToDocs(wi.StatusDetails.Report);

            if (wi.Status == ExecutionStatus.Succeeded)
            {
                //若成功结束，看看转换后的文件url
                Console.WriteLine("The result is downloadable at {0}", wi.Arguments.OutputArguments.First().Resource);
                //下载该文件 
                Until.DownloadToDocs(wi.Arguments.OutputArguments.First().Resource);
            }

        }


    }
}
