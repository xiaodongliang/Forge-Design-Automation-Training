using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

using AIO.Operations;
using AIO.ACES.Models;
using System.IO;
using System.IO.Compression;

namespace MyTestDesignAutomation
{
    class PDFTest
    {        
        static public void PDFTestMain()
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

            //遍历Activity
             GetActivities(container);

            //遍历WorkItem
             //GetWorkItems(container);

            //执行任务 （ WorkItem） - PDF
            CreateWorkItem(container,"PlotToPDF");
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
                    "scope",
                    "code:all"));

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

        static void GetActivities(Container container)
        {
            foreach (var act in container.Activities)
            {
                //打印UserId和Id
                Console.WriteLine("{0}", act.Id);
                if (act.Id.Contains("Adsk"))
                {
                    int xx = 0;
                }

                //打印输入参数
                Console.WriteLine(" Input Parameters:");
                foreach (var inputP in act.Parameters.InputParameters)
                {
                    Console.WriteLine("     {0}", inputP.Name);
                }
                //打印输出参数
                Console.WriteLine(" Output Parameters:");
                foreach (var outputP in act.Parameters.OutputParameters)
                {
                    Console.WriteLine("     {0}", outputP.Name);
                }
            }

        }


        static void GetWorkItems(Container container)
        {

            foreach (var wi in container.WorkItems)
            {
                //打印UserId和Id

                Console.WriteLine("{0}", wi.Id);
                Console.WriteLine("{0} ", wi.ActivityId);
                Console.WriteLine("{0} ", wi.Status);
                //打印输入参数
                Console.WriteLine(" workitem StatusDetails:{0}", wi.StatusDetails);

            }

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
                //Resource = "http://forge-xd-generic-test.herokuapp.com/downloadDADrawing/TestDemo1.dwg",
                Resource = "http://download.autodesk.com/us/samplefiles/acad/visualization_-_aerial.dwg",
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
