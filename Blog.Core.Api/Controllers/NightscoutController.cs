﻿using Blog.Core.Common;
using Blog.Core.Common.Extensions;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.IServices.BASE;
using Blog.Core.Model;
using Blog.Core.Model.Models;
using Blog.Core.Model.ViewModels;
using Blog.Core.Repository.UnitOfWorks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Serilog;

using SqlSugar;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http.Headers;

namespace Blog.Core.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Permissions.Name)]
    public class NightscoutController : ControllerBase
    {
        /// <summary>
        /// 服务器接口，因为是模板生成，所以首字母是大写的，自己可以重构下
        /// </summary>
        private readonly INightscoutServices _nightscoutServices;
        private readonly IWeChatConfigServices _weChatConfigServices;
        private readonly IBaseServices<NightscoutLog> _nightscoutLogServices;
        private readonly IBaseServices<NightscoutServer> _nightscoutServerServices;
        private readonly IBaseServices<WeChatSub> _wechatsubServices;
        private readonly IUnitOfWorkManage _unitOfWorkManage;

        public NightscoutController(INightscoutServices nightscoutServices
            , IWeChatConfigServices weChatConfigServices
            , IBaseServices<NightscoutLog> nightscoutLogServices
            , IBaseServices<NightscoutServer> nightscoutServerServices
            , IUnitOfWorkManage unitOfWorkManage
            , IBaseServices<WeChatSub> wechatsubServices)
        {
            _nightscoutServices = nightscoutServices;
            _weChatConfigServices = weChatConfigServices;
            _nightscoutLogServices = nightscoutLogServices;
            _nightscoutServerServices = nightscoutServerServices;
            _unitOfWorkManage = unitOfWorkManage;
            _wechatsubServices = wechatsubServices;
        }

        [HttpGet]
        public async Task<MessageModel<PageModel<Nightscout>>> Get(int page = 1, string key = "", int pageSize = 50)
        {


            Expression<Func<Nightscout, bool>> whereExpression = a => true;

            if (!(string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key)))
            {
                key = key.Trim();
                whereExpression = whereExpression.And(t => t.name.Contains(key) || t.url.Contains(key) || t.passwd.Contains(key) || t.status.Contains(key) || t.resource.Contains(key));
            }
            var data = await _nightscoutServices.QueryPage(whereExpression, page, pageSize);

            var pushWechatID = AppSettings.app(new string[] { "nightscout", "pushWechatID" }).ObjToString();
            var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
            var pushTemplateID_Exception = AppSettings.app(new string[] { "nightscout", "pushTemplateID_Exception" }).ObjToString();
            var pushTemplateID_Keep = AppSettings.app(new string[] { "nightscout", "pushTemplateID_Keep" }).ObjToString();

            var bindData = await _wechatsubServices.Query(t => t.SubFromPublicAccount == pushWechatID && t.IsUnBind == false && t.CompanyID == pushCompanyCode && data.data.Select(i => i.Id.ToString()).ToList().Contains(t.SubJobID));
            //isBindWeChat
            foreach (var item in data.data)
            {
                var find = bindData.Find(t => t.SubJobID.Equals(item.Id.ToString()));
                if (find != null)
                {
                    item.isBindWeChat = true;
                }
            }


            var miniAppid = AppSettings.app(new string[] { "miniProgram", "appid" }).ObjToString();
            var bindMini = await _wechatsubServices.Query(t => t.SubFromPublicAccount == miniAppid && t.IsUnBind == false && t.CompanyID == pushCompanyCode && data.data.Select(i => i.Id.ToString()).ToList().Contains(t.SubJobID));
            //isBindWeChat
            foreach (var item in data.data)
            {
                var find = bindMini.Find(t => t.SubJobID.Equals(item.Id.ToString()));
                if (find != null)
                {
                    item.isBindMini = true;
                }
            }
            return new MessageModel<PageModel<Nightscout>>()
            {
                msg = "获取成功",
                success = true,
                response = data
            };

        }


        [HttpGet("{id}")]
        public async Task<MessageModel<Nightscout>> Get(long id)
        {
            return new MessageModel<Nightscout>()
            {
                msg = "获取成功",
                success = true,
                response = await _nightscoutServices.QueryById(id)
            };
        }
        private void FilterData(Nightscout request)
        {
            request.name = request.name.ObjToString().Trim();
            request.url = request.url.ObjToString().Trim();
            request.passwd = request.passwd.ObjToString().Trim();
            request.tel = request.tel.ObjToString().Trim();
            request.instanceIP = request.instanceIP.ObjToString().Trim();
            request.serviceName = request.serviceName.ObjToString().Trim();
            if (string.IsNullOrEmpty(request.resource)) request.resource = "未确认";
            if (string.IsNullOrEmpty(request.status)) request.resource = "未启用";
        }
        private async Task<MessageModel<string>> CheckData(Nightscout request)
        {
            if(string.IsNullOrEmpty(request.name))
                return MessageModel<string>.Fail($"名称不能为空");

            if (string.IsNullOrEmpty(request.passwd))
                return MessageModel<string>.Fail($"密码不能为空");

            if (string.IsNullOrEmpty(request.url))
                return MessageModel<string>.Fail($"域名不能为空");

            Expression<Func<Nightscout, bool>> whereExpression = a => true;

            whereExpression = whereExpression.And(t => t.Id != request.Id);
            whereExpression = whereExpression.And(t => t.url == request.url);
            var data = await _nightscoutServices.Query(whereExpression);
            if (data != null && data.Count > 0)
                return MessageModel<string>.Fail($"当前操作的[{request.name}]与[{data[0].name}]的网址配置有冲突,请检查");

            whereExpression.And(t => t.Id != request.Id);
            whereExpression = whereExpression.And(t => t.exposedPort == 0 && t.instanceIP == request.instanceIP && t.serverId == request.serverId);
            data = await _nightscoutServices.Query(whereExpression);
            if (data != null && data.Count > 0)
                return MessageModel<string>.Fail($"当前操作的[{request.name}]与[{data[0].name}]的IP配置有冲突,请检查");

            whereExpression.And(t => t.Id != request.Id);
            whereExpression = whereExpression.And(t => t.exposedPort > 0 && t.exposedPort == request.exposedPort && t.serverId == request.serverId);
            data = await _nightscoutServices.Query(whereExpression);
            if (data != null && data.Count > 0)
                return MessageModel<string>.Fail($"当前操作的[{request.name}]与[{data[0].name}]的端口配置有冲突,请检查");

            whereExpression.And(t => t.Id != request.Id);
            whereExpression = whereExpression.And(t => t.serviceName == request.serviceName);
            data = await _nightscoutServices.Query(whereExpression);
            if (data != null && data.Count > 0)
                return MessageModel<string>.Fail($"当前操作的[{request.name}]与[{data[0].name}]的服务名称配置有冲突,请检查");


            return MessageModel<string>.Success("");

        }
        /// <summary>
        /// 随机生成几位数
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private string GenerateNumber(int count)
        {
            Random random = new Random();
            string r = "";
            int i;
            for (i = 1; i <= count; i++)
            {
                r += random.Next(0, 9).ToString();
            }
            return r;
        }
        [HttpPost]
        public async Task<MessageModel<string>> Post([FromBody] Nightscout request)
        {
            FilterData(request);

            var data = new MessageModel<string>();

            if (request.serverId > 0)
            {
                var nsserver = await _nightscoutServerServices.QueryById(request.serverId);
                if (request.serviceSerial == 0)
                {
                    //新增实例

                    //设置服务名称
                    nsserver.curServiceSerial += 1;
                    request.serviceName = $"nightscout-template{nsserver.curServiceSerial}";
                    request.serviceSerial = nsserver.curServiceSerial;

                    //设置访问IP
                    if (nsserver.curExposedPort > 0)
                    {
                        //通过暴露端口访问
                        nsserver.curExposedPort += 1;
                        request.exposedPort = nsserver.curExposedPort;
                        request.instanceIP = nsserver.serverIp;
                    }
                    else
                    {
                        //通过实例DockerIP访问
                        nsserver.curInstanceIpSerial += 1;
                        //192.168.0.{}
                        nsserver.curInstanceIp = string.Format(nsserver.instanceIpTemplate, nsserver.curInstanceIpSerial);
                        request.instanceIP = nsserver.curInstanceIp;
                    }
                    //不填写自动生成
                    var padName = nsserver.curServiceSerial.ObjToString().PadLeft(3, '0');
                    if (string.IsNullOrEmpty(request.name))
                    {
                        request.name = padName;
                    }
                    if (string.IsNullOrEmpty(request.passwd))
                    {
                        request.passwd = GenerateNumber(5);
                    }
                    if (string.IsNullOrEmpty(request.url))
                    {
                        var templateUrl = AppSettings.app(new string[] { "nightscout", "TemplateUrl" }).ObjToString();
                        request.url = string.Format(templateUrl, GenerateNumber(3) + padName);
                    }

                    
                    
                   
                }
                
                var check = await CheckData(request);
                if (!check.success) return check;
                
               
                try
                {
                    //开启事务
                    _unitOfWorkManage.BeginTran();
                    var id = await _nightscoutServices.Add(request);
                    await _nightscoutServerServices.Update(nsserver);
                    await _nightscoutServerServices.Db.Updateable<NightscoutServer>().SetColumns("curServiceSerial", nsserver.curServiceSerial).Where(t => t.Id > 0).ExecuteCommandAsync();
                    _unitOfWorkManage.CommitTran();

                    data.success = id > 0;
                    if (data.success)
                    {
                        request.Id = id;
                        data.response = id.ObjToString();
                        data.msg = "添加成功";
                        //第一次默认就启动
                        await _nightscoutServices.RunDocker(request);
                    }
                    else
                    {
                        data.msg = "添加失败";
                    }
                }
                catch (Exception)
                {
                    _unitOfWorkManage.RollbackTran();
                    throw;
                }
            }
            else
            {
                return MessageModel<string>.Fail("请选选择一个服务器");
            }
            return data;
        }

        [HttpPut]
        public async Task<MessageModel<string>> Put([FromBody] Nightscout request)
        {
            FilterData(request);
            var check = await CheckData(request);
            if (!check.success) return check;
            var data = new MessageModel<string>();
            data.success = await _nightscoutServices.Update(request);
            if (data.success)
            {
                data.msg = "更新成功";
                data.response = request?.Id.ObjToString();
            }
            if (request.isRefresh)
            {
                await _nightscoutServices.RunDocker(request);
            }
            return data;
        }

        [HttpDelete]
        public async Task<MessageModel<string>> Delete(long id)
        {
            var data = new MessageModel<string>();
            var model = await _nightscoutServices.QueryById(id);
            model.IsDeleted = true;
            data.success = await _nightscoutServices.Update(model);
            if (data.success)
            {
                data.msg = "删除成功";
                data.response = model?.Id.ObjToString();
            }
            await _nightscoutServices.RunDocker(model, true);
            return data;
        }
        /// <summary>
        /// 重置数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<string>> Reset(long id)
        {
            var data = await _nightscoutServices.QueryById(id);
            if (data == null || data.IsDeleted) return MessageModel<string>.Fail("实例不存在");
            await _nightscoutServices.RunDocker(data, true);
            await _nightscoutServices.RunDocker(data, false);
            return MessageModel<string>.Success("刷新成功");
        }
        /// <summary>
        /// 刷新实例
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<string>> Refresh(long id)
        {
            var data = await _nightscoutServices.QueryById(id);
            if (data == null || data.IsDeleted) return MessageModel<string>.Fail("实例不存在");
            await _nightscoutServices.RunDocker(data);
            return MessageModel<string>.Success("刷新成功");
        }
        /// <summary>
        /// nightscout微信绑定二维码
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<WeChatResponseUserInfo>> GetWeChatCode(long id)
        {
            var data = await _nightscoutServices.QueryById(id);
            if (!data.isConnection) return MessageModel<WeChatResponseUserInfo>.Fail("实例还未接入微信");
            if (data == null || data.IsDeleted) return MessageModel<WeChatResponseUserInfo>.Fail("实例不存在");
            string pushWechatID = AppSettings.app(new string[] { "nightscout", "pushWechatID" }).ObjToString();
            string pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
            return await _weChatConfigServices.GetQRBind(new WeChatUserInfo { userID = id.ToString(), companyCode = pushCompanyCode, id = pushWechatID, userNick = data.name });
        }
        /// <summary>
        /// nightscout取消绑定
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<WeChatResponseUserInfo>> UnbindWeChat(long id)
        {
            var data = await _nightscoutServices.QueryById(id);
            if (data == null || data.IsDeleted) return MessageModel<WeChatResponseUserInfo>.Fail("实例不存在");
            string pushWechatID = AppSettings.app(new string[] { "nightscout", "pushWechatID" }).ObjToString();
            string pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
            return await _weChatConfigServices.UnBind(new WeChatUserInfo { userID = id.ToString(), companyCode = pushCompanyCode, id = pushWechatID });
        }

        /// <summary>
        /// 获取实例运行日志
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<PageModel<NightscoutLog>>> GetLog(long id, int page = 1, int pageSize = 50)
        {

            var data = await _nightscoutServices.QueryById(id);
            if (data == null || data.IsDeleted) return MessageModel<PageModel<NightscoutLog>>.Fail("实例不存在");
            Expression<Func<NightscoutLog, bool>> whereExpression = a => true;
            whereExpression = whereExpression.And(t => t.pid == id);
            return new MessageModel<PageModel<NightscoutLog>>()
            {
                msg = "获取成功",
                success = true,
                response = await _nightscoutLogServices.QueryPage(whereExpression, page, pageSize, "Id desc")
            };
        }
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="Value1">名称</param>
        ///// <param name="Value2">血糖值和预测值</param>
        ///// <param name="Value3">类型</param>
        ///// <param name="Value4">用户id</param>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">数据</param>
        [HttpGet]
        [AllowAnonymous]
        public async void Push([FromQuery] IFTTT data)
        {
            Log.Logger.Information($"进入nightscout");
            Log.Logger.Information($"nightscout原始数据:{JsonConvert.SerializeObject(data)}");


            if ("bwp".Equals(data.Value3))
            {
                Log.Logger.Information("bwp跳过");
                return;
            }
            if ("ns-allclear".Equals(data.Value5))
            {
                Log.Logger.Information("All Clear跳过");
                return;
            }




            var pushWechatID = AppSettings.app(new string[] { "nightscout", "pushWechatID" }).ObjToString();
            var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
            var pushTemplateID_Exception = AppSettings.app(new string[] { "nightscout", "pushTemplateID_Exception" }).ObjToString();
            var pushTemplateID_Keep = AppSettings.app(new string[] { "nightscout", "pushTemplateID_Keep" }).ObjToString();
            var pushTemplateID = string.Empty;
            var pushData = new WeChatCardMsgDataDto();
            pushData.cardMsg = new WeChatCardMsgDetailDto();
            Nightscout nightscout;

            if ("ns-keep".Equals(data.Value5))
            {
                nightscout = await _nightscoutServices.QueryById(data.Value4);
                if (nightscout == null || nightscout.IsDeleted)
                {
                    Log.Logger.Information("nightscout用户未找到");
                    return;
                }
                pushTemplateID = pushTemplateID_Keep;
                data.Value1 = "血糖持续监测";
                data.Value2_1 = data.Value2;


                pushData.cardMsg.first = $"您好！{nightscout.name}，为您推送实时血糖监测";
                pushData.cardMsg.keyword1 = $"实时血糖：{data.Value2_1}";
                pushData.cardMsg.keyword2 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if ("ns-event".Equals(data.Value5))
            {
                nightscout = await _nightscoutServices.QueryById(data.Value4);
                if (nightscout == null || nightscout.IsDeleted)
                {
                    Log.Logger.Information("nightscout用户未找到");
                    return;
                }
                pushTemplateID = pushTemplateID_Exception;
                data.Value1 = data.Value1.Replace(",", " -");
                var ls = data.Value2.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                if (ls.Length > 1)
                {
                    data.Value2_1 = ls[0].Replace("BG Now: ", "");
                    data.Value2_2 = ls[1].Replace("BG 15m: ", "");
                }
                pushData.cardMsg.first = $"您好！{nightscout.name}，当前血糖异常";
                pushData.cardMsg.keyword1 = data.Value2_1;
                pushData.cardMsg.keyword2 = data.Value1;
                pushData.cardMsg.remark = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                Log.Logger.Information("暂时跳过其他情况");
                return;
            }

            pushData.cardMsg.url = $"https://{nightscout.url}";
            pushData.cardMsg.template_id = pushTemplateID;


            pushData.info = new WeChatUserInfo();
            pushData.info.id = pushWechatID;
            pushData.info.companyCode = pushCompanyCode;
            pushData.info.userID = nightscout.Id.ToString();
            Log.Logger.Information($"nightscout处理数据:{JsonConvert.SerializeObject(data)}");
            await _weChatConfigServices.PushCardMsg(pushData, "");
        }
        [HttpGet]
        public async Task<MessageModel<object>> GetSummary()
        {
            var status = await _nightscoutServices.Db.Queryable<Nightscout>()
             .GroupBy(it => it.status)
             .Select(it => new
             {
                 count = SqlFunc.AggregateCount(it.Id),
                 name = it.status
             })
             .ToListAsync();
            var resource = await _nightscoutServices.Db.Queryable<Nightscout>()
             .GroupBy(it => it.resource)
             .Select(it => new
             {
                 count = SqlFunc.AggregateCount(it.Id),
                 name = it.resource
             })
             .ToListAsync();

            return MessageModel<object>.Success("获取成功", new { status, resource });
        }
        [HttpGet]
        public MessageModel<List<NSPlugin>> GetPlugins()
        {
            var sp = AppSettings.app<NSPlugin>(new string[] { "nightscout", "plugins" });

            return MessageModel<List<NSPlugin>>.Success("成功", sp);
        }



        [HttpGet]
        public async Task<MessageModel<PageModel<NightscoutServer>>> getNsServer(int page = 1, string key = "", int pageSize = 50)
        {
            Expression<Func<NightscoutServer, bool>> whereExpression = a => true;

            if (!(string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key)))
            {
                key = key.Trim();
                whereExpression = whereExpression.And(t => t.serverName.Contains(key));
            }
            var data = await _nightscoutServerServices.QueryPage(whereExpression, page, pageSize);
            return MessageModel<PageModel<NightscoutServer>>.Success("获取成功", data);
        }
        [HttpDelete]
        public async Task<MessageModel<string>> delNsServer(long id)
        {
            var data = new MessageModel<string>();
            var model = await _nightscoutServerServices.QueryById(id);
            model.IsDeleted = true;
            data.success = await _nightscoutServerServices.Update(model);
            if (data.success)
            {
                data.msg = "删除成功";
                data.response = model?.Id.ObjToString();
            }
            return data;
        }
        [HttpPut]
        public async Task<MessageModel<string>> updateNsServer([FromBody] NightscoutServer request)
        {
            var data = await _nightscoutServerServices.QueryById(request.Id);
            if(data ==null) MessageModel<string>.Fail("要更新的数据不存在");
            var id = await _nightscoutServerServices.Update(request);
            return MessageModel<string>.Success("添加成功", id.ObjToString());
        }
        [HttpPost]
        public async Task<MessageModel<string>> addNsServer([FromBody] NightscoutServer request)
        {
            var id = await _nightscoutServerServices.Add(request);
            return MessageModel<string>.Success("添加成功", id.ObjToString());
        }
        [HttpGet]
        public async Task<MessageModel<List<NightscoutServer>>> getAllNsServer()
        {
            var data = await _nightscoutServerServices.Db.Queryable<NightscoutServer>().Select(t => new NightscoutServer { Id = t.Id, serverName = t.serverName }).ToListAsync();

            var ids = data.Select(tt => tt.Id).ToList();
            var ls = await _nightscoutServerServices.Db.Queryable<Nightscout>()
            .GroupBy(t => t.serverId)
                .Where(t => ids.Contains(t.serverId)).Select(t => new { serverId = t.serverId ,count =SqlFunc.AggregateCount (t.serverId)}).ToListAsync();

            foreach (var item in data)
            {
                var row = ls.Find(t => t.serverId == item.Id);
                item.count = row.count;
            }
            return MessageModel<List<NightscoutServer>>.Success("获取成功", data);
        }


        /// <summary>
        /// 获取小程序绑定二维码
        /// </summary>
        /// <param name="nsid"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<string>> GetBindQR(long nsid)
        {
            var appid = AppSettings.app(new string[] { "miniProgram", "appid" }).ObjToString();
            var secret = AppSettings.app(new string[] { "miniProgram", "secret" }).ObjToString();
            var appEnv = AppSettings.app(new string[] { "miniProgram", "env" }).ObjToString();
            var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
            string url = $"https://api.weixin.qq.com/cgi-bin/stable_token";

            var weChatToken = new { appid = appid, secret = secret, grant_type = "client_credential" };

            var nsInfo =  await _nightscoutServices.QueryById(nsid);



            WeChatQR weChatQR = new WeChatQR
            {
                QRbindCompanyID = pushCompanyCode,
                QRbindJobID = nsInfo.Id.ObjToString(),
                QRbindJobNick = nsInfo.name,
                QRcrateTime = DateTime.Now,
                QRpublicAccount = appid,
                QRticket = StringHelper.GetGUID()
            };

            await _weChatConfigServices.Db.Insertable<WeChatQR>(weChatQR).ExecuteCommandAsync();

            string result;
            using (HttpContent httpContent = new StringContent(JsonHelper.ObjToJson(weChatToken)))
            {
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = new TimeSpan(0, 0, 60);
                    result = await httpClient.PostAsync(url, httpContent).Result.Content.ReadAsStringAsync();
                    AccessTokenDto accessTokenDto = JsonHelper.JsonToObj<AccessTokenDto>(result);

                    //正式版为 "release"，体验版为 "trial"，开发版为 "develop"
                    var ticket = weChatQR.QRticket;
                    var jsonBind = JsonHelper.ObjToJson(new { path = $"pages/index/index?ticket={ticket}", env_version = appEnv, width = 128 });
                    using (HttpContent httpContentBind = new StringContent(jsonBind))
                    {
                        var urlBind = $"https://api.weixin.qq.com/wxa/getwxacode?access_token={accessTokenDto.access_token}";
                        var bindstream = await httpClient.PostAsync(urlBind, httpContentBind).Result.Content.ReadAsByteArrayAsync();
                        //return File(bindstream, "image/jpeg");
                        return MessageModel<string>.Success("成功", Convert.ToBase64String(bindstream));
                    }
                }
            }
        }
        /// <summary>
        /// 绑定小程序
        /// <param name="ticket"></param>
        /// <param name="openid"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<MessageModel<string>> BindQR(string ticket,string openid)
        {

            string appid = AppSettings.app(new string[] { "miniProgram", "appid" }).ObjToString();
            var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
            var ticketInfo = await _weChatConfigServices.Db.Queryable<WeChatQR>().Where(t => t.QRpublicAccount == appid && t.QRticket == ticket && t.QRisUsed == false).FirstAsync();
            if (ticketInfo == null)
               return MessageModel<string>.Fail($"没有找到ticket:{ticket},请勿重复使用");
            var bindInfo = await _weChatConfigServices.Db.Queryable<WeChatSub>().Where(t => t.SubFromPublicAccount == appid && t.CompanyID == pushCompanyCode && t.SubJobID == ticketInfo.QRbindJobID).FirstAsync();

            _unitOfWorkManage.BeginTran();
           try
            {
                if (bindInfo == null)
                {
                    bindInfo = new WeChatSub
                    {
                        SubFromPublicAccount = appid,
                        SubJobID = ticketInfo.QRbindJobID,
                        SubUserOpenID = openid,
                        CompanyID = ticketInfo.QRbindCompanyID,
                        SubUserRegTime = DateTime.Now,
                        IsUnBind = false
                    };

                    await _weChatConfigServices.Db.Insertable<WeChatSub>(bindInfo).ExecuteCommandAsync();
                }
                else
                {

                    bindInfo.LastSubUserOpenID = bindInfo.SubUserOpenID;
                    //bindInfo.SubFromPublicAccount = appid;
                    bindInfo.SubJobID = ticketInfo.QRbindJobID;
                    //bindInfo.SubUserOpenID = openid;
                    //bindInfo.CompanyID = ticketInfo.QRbindCompanyID;
                    bindInfo.SubUserRefTime = DateTime.Now;
                    bindInfo.IsUnBind = false;
                    await _weChatConfigServices.Db.Updateable<WeChatSub>(bindInfo).UpdateColumns(t => new {t.LastSubUserOpenID ,t.SubJobID ,t.SubUserRefTime ,t.IsUnBind }).ExecuteCommandAsync();
                }
                ticketInfo.QRisUsed = true;
                ticketInfo.QRuseTime = DateTime.Now;
                ticketInfo.QRuseOpenid = openid;
                await _weChatConfigServices.Db.Updateable<WeChatQR>(ticketInfo).UpdateColumns(t=>new {t.QRisUsed,t.QRuseOpenid,t.QRuseTime }).ExecuteCommandAsync();
                _unitOfWorkManage.CommitTran();
            }
            catch (Exception)
            {
                _unitOfWorkManage.RollbackTran();
                throw;
            }
            return MessageModel<string>.Success("绑定成功");
        }
        /// <summary>
        /// 取消绑定小程序
        /// </summary>
        /// <param name="nsid"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<MessageModel<string>> UnBindQR(string nsid)
        {
            var appid = AppSettings.app(new string[] { "miniProgram", "appid" }).ObjToString();
            var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();
            var bindInfo = await _weChatConfigServices.Db.Queryable<WeChatSub>().Where(t => t.SubFromPublicAccount == appid && t.SubJobID == nsid && t.CompanyID == pushCompanyCode && t.IsUnBind == false).ToListAsync();
            if(bindInfo == null || bindInfo.Count== 0)
                return MessageModel<string>.Fail("没有找到解绑信息");
            foreach (var item in bindInfo)
            {
                item.IsUnBind = true;
                item.SubUserRefTime = DateTime.Now;
            }
            await  _weChatConfigServices.Db.Updateable< WeChatSub>(bindInfo).UpdateColumns(t => new {t.IsUnBind,t.SubUserRefTime}).ExecuteCommandAsync();
            return MessageModel<string>.Success("解绑成功");
        }
        /// <summary>
        /// 小程code换openid
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<MessageModel<WeChatModel>> CodeLogin(string code)
        {
            var appid = AppSettings.app(new string[] { "miniProgram", "appid" }).ObjToString();
            var secret = AppSettings.app(new string[] { "miniProgram", "secret" }).ObjToString();
            HttpClient httpClient = new HttpClient();
            var res = await httpClient.GetStringAsync($"https://api.weixin.qq.com/sns/jscode2session?appid={appid}&secret={secret}&js_code={code}&grant_type=authorization_code");

            var data = JsonHelper.JsonToObj<WeChatModel>(res);
            if (data.errcode.Equals(0))
            {
                return MessageModel<WeChatModel>.Success(string.Empty, data);
            }
            else
            {
                return MessageModel<WeChatModel>.Fail(string.Empty, data);
            }
        }
        /// <summary>
        /// 小程序获取血糖信息
        /// </summary>
        /// <param name="openid"></param>
        /// <returns></returns>

        [HttpGet]
        [AllowAnonymous]
        public async Task<MessageModel<SugarDTO>> GetCurBloodSugar(string openid)
        {
            SugarDTO sugarDTO = new SugarDTO();
            var appid = AppSettings.app(new string[] { "miniProgram", "appid" }).ObjToString();
            var Host = AppSettings.app(new string[] { "miniProgram", "Host" }).ObjToString();
            var Port = AppSettings.app(new string[] { "miniProgram", "Port" }).ObjToString();
            var LoginName = AppSettings.app(new string[] { "miniProgram", "LoginName" }).ObjToString();
            var LoginPasswd = AppSettings.app(new string[] { "miniProgram", "LoginPasswd" }).ObjToString();
            var pushCompanyCode = AppSettings.app(new string[] { "nightscout", "pushCompanyCode" }).ObjToString();

            //configuration.GetSection("sayings").Get<string[]>();
            List<string> sayings = AppSettings.app<string>(new string[] { "miniProgram", "sayings" });
            var title = AppSettings.app(new string[] { "miniProgram", "title" }).ObjToString();

            var nsid = await _weChatConfigServices.Db.Queryable<WeChatSub>().Where(t=>t.SubFromPublicAccount == appid && t.CompanyID == pushCompanyCode && t.SubUserOpenID == openid && t.IsUnBind == false).Select(t=>t.SubJobID).FirstAsync();

            if (string.IsNullOrEmpty(nsid))
                return MessageModel<SugarDTO>.Fail("无绑定信息,无法查看血糖");
            var longNsid = nsid.ObjToLong();
            var serviceName = await _weChatConfigServices.Db.Queryable<Nightscout>().Where(t => t.Id == longNsid).Select(t=>t.serviceName).FirstAsync();
            if (string.IsNullOrEmpty(serviceName))
                return MessageModel<SugarDTO>.Fail("无血糖信息可供查看,请检查是否绑定NS");

            var grantConnectionMongoString = $"mongodb://{LoginName}:{LoginPasswd}@{Host}:{Port}";
            var client = new MongoClient(grantConnectionMongoString);
            var database = client.GetDatabase(serviceName);
            var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("entries"); // 替换为你的集合名称

            var filter = Builders<MongoDB.Bson.BsonDocument>.Filter.Empty; // 获取所有数据
            var projection = Builders<MongoDB.Bson.BsonDocument>.Projection.Include("date").Include("sgv").Include("direction").Exclude("_id");


            var ls = await collection.Find(filter).Sort(Builders<MongoDB.Bson.BsonDocument>.Sort.Descending("_id")).Limit(900).Project(projection).ToListAsync();

            
            var sugers = JsonHelper.JsonToObj<List<EntriesEntity>>(ls.ToJson());
            if (sugers == null) sugers = new List<EntriesEntity>();
            foreach (var item in sugers)
            {
                FormatDate(item);
            }

            sugarDTO.curBlood = sugers.Count > 0 ? sugers[0] : new EntriesEntity { date_str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            sugarDTO.curBlood.title = title;
            Random rd = new Random();

            if (sayings != null && sayings.Count > 0)
            {
                sugarDTO.curBlood.saying = sayings[rd.Next(0, sayings.Count)];
            }
            else
            {
                sugarDTO.curBlood.saying = "每一次在控制血糖上的成功都是向自己付出的最好回报。";
            }

            {
                //今天
                var flagDate = DateTime.Now.Date;
                sugarDTO.day0 = HandleSugarList(sugers, flagDate);

            }
            {
                //昨天
                var flagDate = DateTime.Now.Date.AddDays(-1);
                sugarDTO.day1 = HandleSugarList(sugers, flagDate);

            }
            {
                //前天
                var flagDate = DateTime.Now.Date.AddDays(-2);
                sugarDTO.day2 = HandleSugarList(sugers, flagDate);

            }
            return MessageModel<SugarDTO>.Success("", sugarDTO);

        }

        private List<EntriesEntity> HandleSugarList(List<EntriesEntity> sugers, DateTime flagDate)
        {
            List<EntriesEntity> flagList = sugers.Where(t => t.date_now.Date == flagDate).ToList();
            //flagList.Reverse();

            var ls = new List<EntriesEntity>();
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(0).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(3).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(6).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(9).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(12).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(15).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(18).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(21).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(23).AddMinutes(59).AddSeconds(59).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            foreach (var item in ls)
            {
                FormatDate(item);
            }
            flagList.AddRange(ls);
            return flagList.AsEnumerable().OrderBy(s => s.date_now).ToList();
        }


        private void FormatDate(EntriesEntity dd)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)dd.date);
            DateTime dateTime = dateTimeOffset.UtcDateTime.AddHours(8);
            dd.date_now = dateTime;
            dd.date_str = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            dd.date_time = dateTime.ToString("HH:mm:ss");

            dd.date_step = (int)(DateTime.Now - dateTime).TotalMinutes;

            if (dd.sgv != null)
                dd.sgv_str = Math.Round(1.0 * dd.sgv.Value / 18, 1);


            dd.direction_str = GetFlag(dd.direction);
        }
        private string GetFlag(string flag)
        {
            switch (flag)
            {
                case "NONE":
                    return "⇼";
                case "TripleUp":
                    return "⤊";
                case "DoubleUp":
                    return "⇈";
                case "SingleUp":
                    return "↑";
                case "FortyFiveUp":
                    return "↗";
                case "Flat":
                    return "→";
                case "FortyFiveDown":
                    return "↘";
                case "SingleDown":
                    return "↓";
                case "DoubleDown":
                    return "⇊";
                case "TripleDown":
                    return "⤋";
                case "NOT COMPUTABLE":
                    return "-";
                case "RATE OUT OF RANGE":
                    return "⇕";
                default:
                    return "未知";
            }
        }


    }

    public class SugarDTO
    {
        public List<EntriesEntity> day0 { get; set; }

        public List<EntriesEntity> day1 { get; set; }

        public List<EntriesEntity> day2 { get; set; }

        public EntriesEntity curBlood { get; set; }

    }

    public class WeChatModel
    {
        public string openid { get; set; }
        public string session_key { get; set; }
        public string unionid { get; set; }
        public int errcode { get; set; }
        public string errmsg { get; set; }
    }

    public class EntriesEntity
    {
        public decimal date { get; set; }

        public DateTime date_now { get; set; }
        public string date_str { get; set; }
        public string date_time { get; set; }
        public int? date_step { get; set; }
        public int? sgv { get; set; }
        public double? sgv_str { get; set; }
        public string direction { get; set; }
        public string direction_str { get; set; }
        public bool isMask { get; set; }
        public string title { get; set; }
        public string saying { get; set; }
    }
    public class AccessTokenDto
    {
        public string access_token { get; set; }
    }

}
