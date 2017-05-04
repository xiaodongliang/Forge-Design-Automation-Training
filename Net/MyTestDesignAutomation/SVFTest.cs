using AIO.ACES.Models;
using AIO.Operations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MyTestDesignAutomation
{
    class SVFTest
    {

        static readonly string Script2d =
            "_prepareforpropertyextraction index.json\r\n_indexextractor index.json\r\n_publishtof2d ./output/result.svf\r\n_createbubblepackage ./output ./result \r\n\n";


        static public void SVFTestMain()
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

            //删除某Activity
            string myActName = "CreateSVFAct";
            DeleteActivity(container, myActName);
            //创建某Activity
            CreateOneActivity(container, myActName);

            int index = 0;
           // while (index < 100)
            {
             //   System.Threading.Thread.Sleep(30000);

                //执行任务 （ WorkItem） - CreateALineCircle
                CreateWorkItem(container, myActName);

                index++;

                Console.WriteLine("The index 0 {0}", index);
            }



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


                values.Add(new KeyValuePair<string, string>(
                    "scope",
                    "code:all"));

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

        /// 创建某Activity
        static Activity CreateOneActivity(Container container, string actId)
        {
            Console.WriteLine("正在创建某Activity...");

            var activity = new Activity()
            {
                Id = actId,
                Version = 1,
                Instruction = new Instruction()
                {
                    //Script = "_tilemode 1 _line 0,0 100,100\n\n_circle 0,0 500 _save result.dwg\n"
                    Script = Script2d
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
                      Name = "Results", LocalFileName = "result"
                    }
                  }
                },
                RequiredEngineVersion = "21.0"
            };

            container.AddToActivities(activity);
            activity.AppPackages.Add("Publish2View21"); // reference the custom AppPackage
            container.SaveChanges();

            Console.WriteLine("创建某Activity成功！");


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
                //Resource = "http://forge-xd-generic-test.herokuapp.com/downloadDADrawing/TestDemo1.dwg",
                //Resource = "http://download.autodesk.com/us/samplefiles/acad/visualization_-_aerial.dwg",
                Resource = "http://forgettest.herokuapp.com/getdwgfile/Gatehouse.dwg",
                //非A360数据源
                StorageProvider = StorageProvider.Generic
            });
            //设置输出参数
            wi.Arguments.OutputArguments.Add(new Argument()
            {
                //对应Activity哪个参数 
                Name = "Results",
                //非A360数据源
                StorageProvider = StorageProvider.Generic,
                //HTTP动作 - 将结果放到对应的云存储
                HttpVerb = HttpVerbType.POST,
                //如果设置为null，则缺省放到Design Automation 存储空间里
                Resource = null,
                ResourceKind = ResourceKind.ZipPackage
            });

            DateTime startTime = DateTime.Now;


            //添加此WorkItem, 也就是开始启动任务
            container.AddToWorkItems(wi);
            container.SaveChanges();

            //看看AutoCAD I/O为WorkItem分配了什么Guid
            Console.WriteLine("Id= {0}", wi.Id);

            container.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;

            string timeStr0 = (DateTime.Now - startTime).Seconds.ToString();
            Console.WriteLine("The time 0 {0}", timeStr0);
            //等待，看看该任务WorkItem是否执行完毕
            do
            {
                //System.Threading.Thread.Sleep(10000);
                wi = container.WorkItems.Where(p => p.Id == wi.Id).SingleOrDefault();
            }
            while (wi.Status == ExecutionStatus.Pending || wi.Status == ExecutionStatus.InProgress);

            string timeStr1 = (DateTime.Now - startTime).Seconds.ToString();
            Console.WriteLine("The time 1 {0}", timeStr0);


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
            //string timeStr2 = (DateTime.Now - startTime).Seconds.ToString();
            //Console.WriteLine("The time 2{0}", timeStr1);



        }

    }
}
