﻿using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.Model.Models;
using Blog.Core.Services.BASE;
using MongoDB.Driver;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Blog.Core.Model.ViewModels;
using System.Linq;
using Blog.Core.IServices.BASE;
using Renci.SshNet;
using System.Text;
using MongoDB.Bson;
using System.Net.Http;
using Blog.Core.Repository.UnitOfWorks;

namespace Blog.Core.Services
{
    /// <summary>
    /// NightscoutServices
    /// </summary>	
    public class NightscoutServices : BaseServices<Nightscout>, INightscoutServices
    {

        private readonly IBaseServices<NightscoutLog> _nightscoutLogServices;
        private readonly IBaseServices<NightscoutServer> _nightscoutServerServices;
        private readonly IUnitOfWorkManage _unitOfWorkManage;
        public NightscoutServices(IBaseServices<NightscoutLog> nightscoutLogServices, IBaseServices<NightscoutServer> nightscoutServerServices, IUnitOfWorkManage unitOfWorkManage)
        {
            _nightscoutLogServices = nightscoutLogServices;
            _nightscoutServerServices = nightscoutServerServices;
            _unitOfWorkManage = unitOfWorkManage;

        }
        public async Task<bool> ResolveDomain (Nightscout nightscout)
        {
            NightscoutLog log = new NightscoutLog();
            string cfKey = AppSettings.app(new string[] { "cf", "key" }).ObjToString();
            string cfZoomID = AppSettings.app(new string[] { "cf", "zoomID" }).ObjToString();
            string cfCDN = AppSettings.app(new string[] { "cf", "cdn" }).ObjToString();

            await  UnResolveDomain(nightscout);
            var client = new HttpClient();
            //添加
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.cloudflare.com/client/v4/zones/{cfZoomID}/dns_records");
            request.Headers.Add("Authorization", $"Bearer {cfKey}");
            CFAddMessageInfo cfAdd = new CFAddMessageInfo();
            cfAdd.content = cfCDN;
            cfAdd.name = nightscout.url;
            cfAdd.proxied = false;
            cfAdd.type = "CNAME";
            cfAdd.comment = "自动创建解析";
            cfAdd.ttl = 1;
            var content = new StringContent(JsonHelper.ObjToJson(cfAdd), null, "text/plain");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var txt = await response.Content.ReadAsStringAsync();
            var obj = JsonHelper.JsonToObj<CFMessageInfo>(txt);
            if (obj.success)
            {
                log.content = "添加解析成功";
            }
            else
            {
                log.content = "添加解析失败";
            }
            log.pid = nightscout.Id;
            log.success = obj.success;
            await this.Db.Insertable<NightscoutLog>(log).ExecuteCommandAsync();
            nightscout.isChina = true;
            await this.Db.Updateable<Nightscout>(nightscout).UpdateColumns(t => new { t.isChina }).ExecuteCommandAsync();
            client.Dispose();
            request.Dispose();
            response.Dispose();
            return obj.success;
        }
        public async Task<bool> UnResolveDomain(Nightscout nightscout)
        {
            NightscoutLog log = new NightscoutLog();
            string cfKey = AppSettings.app(new string[] { "cf", "key" }).ObjToString();
            string cfZoomID = AppSettings.app(new string[] { "cf", "zoomID" }).ObjToString();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{cfZoomID}/dns_records?type=CNAME&name={nightscout.url}");
            request.Headers.Add("Authorization", $"Bearer {cfKey}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var txt = await response.Content.ReadAsStringAsync();
            var obj = JsonHelper.JsonToObj<CFMessageListInfo>(txt);
            request.Dispose();
            response.Dispose();
            if (obj.success && obj.result != null && obj.result.Count == 1)
            {
                //删除
                request = new HttpRequestMessage(HttpMethod.Delete, $"https://api.cloudflare.com/client/v4/zones/{cfZoomID}/dns_records/{obj.result[0].id}");
                request.Headers.Add("Authorization", $"Bearer {cfKey}");
                response = await client.SendAsync(request);
                txt = await response.Content.ReadAsStringAsync();
                var obj2 = JsonHelper.JsonToObj<CFMessageInfo>(txt);
                if (obj2.success)
                {
                    log.content = "删除解析成功";
                }
                else
                {
                    log.content = "删除解析失败";
                }
                request.Dispose();
                response.Dispose();
            }
            else
            {
                log.content = "没有要删除的解析";
            }
            log.pid = nightscout.Id;
            log.success = obj.success;
            await this.Db.Insertable<NightscoutLog>(log).ExecuteCommandAsync();
            nightscout.isChina = false;
            await this.Db.Updateable<Nightscout>(nightscout).UpdateColumns(t => new { t.isChina }).ExecuteCommandAsync();
            client.Dispose();
            return obj.success;
        }

        public async Task InitData(Nightscout nightscout, NightscoutServer nsserver)
        {
            if (string.IsNullOrEmpty(nightscout.serviceName) || string.IsNullOrEmpty(nightscout.url)) return;
            var master = (await _nightscoutServerServices.Query(t => t.isMongo == true)).FirstOrDefault();
            NightscoutLog log = new NightscoutLog();
            StringBuilder sb = new StringBuilder();
            try
            {
                using (var sshClient = new SshClient(master.serverIp, master.serverPort, master.serverLoginName, master.serverLoginPassword))
                {
                    //创建SSH
                    sshClient.Connect();

                    using (var cmd = sshClient.CreateCommand(""))
                    {
                        //创建用户
                        var grantConnectionMongoString = $"mongodb://{nsserver.mongoLoginName}:{nsserver.mongoLoginPassword}@{nsserver.mongoIp}:{nsserver.mongoPort}";
                        var client = new MongoClient(grantConnectionMongoString);

                        //try
                        //{
                        //    client.DropDatabase(nightscout.serviceName);
                        //}
                        //catch (Exception ex)
                        //{
                        //    sb.Append($"删除数据库失败:{ex.Message}");
                        //}
                        var database = client.GetDatabase(nightscout.serviceName);

                        //try
                        //{
                        //    var deleteUserCommand = new BsonDocument
                        //    {
                        //        { "dropUser", nsserver.mongoLoginName },
                        //        { "writeConcern", new BsonDocument("w", 1) }
                        //    };
                        //    // 执行删除用户的命令
                        //    var result = database.RunCommand<BsonDocument>(deleteUserCommand);

                        //}
                        //catch (Exception ex)
                        //{
                        //    sb.Append($"删除用户失败:{ex.Message}");
                        //}
                        try
                        {
                            //创建用户
                            var command = new BsonDocument
                                    {
                                        { "createUser", nsserver.mongoLoginName },
                                        { "pwd" ,nsserver.mongoLoginPassword },
                                        { "roles", new BsonArray
                                            {
                                                {"readWrite"}
                                            }
                                        }
                                    };
                            var result = database.RunCommand<BsonDocument>(command);
                        }
                        catch (Exception ex)
                        {
                            sb.Append($"创建用户失败:{ex.Message}");
                        }

                        //初始化数据库
                        var res = cmd.Execute($"docker exec -t mongoserver mongorestore -u={nsserver.mongoLoginName} -p={nsserver.mongoLoginPassword} -d {nightscout.serviceName} /data/backup/template");

                        try
                        {
                            //修改参数
                            var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("profile"); // 集合
                            var filter = Builders<MongoDB.Bson.BsonDocument>.Filter.Empty; // 条件
                            DateTime date = DateTime.Now.ToUniversalTime(); // 获取当前日期和时间的DateTime对象
                            long timestamp = (long)(date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

                            var update = Builders<BsonDocument>.Update
                                .Set("created_at", date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
                                .Set("mills", timestamp)
                                .Set("startDate", date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                            var updateRes = collection.UpdateOne(filter, update);
                        }
                        catch (Exception ex)
                        {
                            sb.Append($"创建初始化数据失败:{ex.Message}");
                        }
                    }

                    sshClient.Disconnect();
                }

            }
            catch (Exception ex)
            {
                sb.AppendLine($"初始化失败:{ex.Message}");
                LogHelper.Error(ex.Message);
                LogHelper.Error(ex.StackTrace);
                log.success = false;
                throw;
            }
            finally
            {
                log.content = sb.ToString();
                log.pid = nightscout.Id;
                await this.Db.Insertable<NightscoutLog>(log).ExecuteCommandAsync();
            }
        }

        public async Task StopDocker(Nightscout nightscout, NightscoutServer nsserver)
        {
            if (string.IsNullOrEmpty(nightscout.serviceName) || string.IsNullOrEmpty(nightscout.url)) return;
            NightscoutLog log = new NightscoutLog();
            StringBuilder sb = new StringBuilder();


            try
            {
                FileHelper.FileDel($"/etc/nginx/conf.d/nightscout/{nightscout.Id}.conf");
                using (var sshClient = new SshClient(nsserver.serverIp, nsserver.serverPort, nsserver.serverLoginName, nsserver.serverLoginPassword))
                {
                    //创建SSH
                    sshClient.Connect();
                    using (var cmd = sshClient.CreateCommand(""))
                    {
                        //刷新nginx
                        var master = (await _nightscoutServerServices.Query(t => t.isNginx == true)).FirstOrDefault();
                        if (master != null)
                        {
                            using (var sshMasterClient = new SshClient(master.serverIp, master.serverPort, master.serverLoginName, master.serverLoginPassword))
                            {
                                sshMasterClient.Connect();
                                using (var cmdMaster = sshMasterClient.CreateCommand(""))
                                {
                                    var resMaster = cmdMaster.Execute("docker exec -t nginxserver nginx -s reload");
                                    sb.AppendLine($"刷新域名:{resMaster}");
                                }
                                sshMasterClient.Disconnect();
                            }
                        }
                        else
                        {
                            sb.AppendLine("没有找到nginx服务器");
                        }
                        //停止实例
                        var res = cmd.Execute($"docker stop {nightscout.serviceName}");
                        sb.AppendLine($"停止实例:{res}");

                        //删除实例
                        res = cmd.Execute($"docker rm {nightscout.serviceName}");
                        sb.AppendLine($"删除实例:{res}");
                    }
                    sshClient.Disconnect();
                }
                log.success = true;
            }
            catch (Exception ex)
            {
                sb.AppendLine($"停止实例错误:{ex.Message}");
                log.success = false;
                LogHelper.Error(ex.Message);
                LogHelper.Error(ex.StackTrace);
                throw;
            }
            finally
            {
                log.content = sb.ToString();
                log.pid = nightscout.Id;
                await this.Db.Insertable<NightscoutLog>(log).ExecuteCommandAsync();
            }
            

        }

        public async Task DeleteData(Nightscout nightscout, NightscoutServer nsserver)
        {
            if (string.IsNullOrEmpty(nightscout.serviceName) || string.IsNullOrEmpty(nightscout.url)) return;

            NightscoutLog log = new NightscoutLog();
            StringBuilder sb = new StringBuilder();


            try
            {
                FileHelper.FileDel($"/etc/nginx/conf.d/nightscout/{nightscout.Id}.conf");

                var connectionMongoString = $"mongodb://{nsserver.mongoLoginName}:{nsserver.mongoLoginPassword}@{nsserver.mongoIp}:{nsserver.mongoPort}";
                var client = new MongoClient(connectionMongoString);

                var database = client.GetDatabase(nightscout.serviceName);
                var deleteUserCommand = new BsonDocument
                    {
                        { "dropUser", nsserver.mongoLoginName },
                        { "writeConcern", new BsonDocument("w", 1) }
                    };
                // 执行删除用户的命令
                try
                {
                    var delUserInfo = database.RunCommand<BsonDocument>(deleteUserCommand);
                    sb.AppendLine((delUserInfo == null ? "" : delUserInfo.ToJson()));
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"删除mongo用户失败:{ex.Message}");
                }
                //删除mongo
                try
                {
                    client.DropDatabase(nightscout.serviceName);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"删除mongo数据库失败:{ex.Message}");
                }
                log.success = true;
            }
            catch (Exception ex)
            {
                sb.AppendLine($"删除数据错误:{ex.Message}");
                log.success = false;

                LogHelper.Error(ex.Message);
                LogHelper.Error(ex.StackTrace);
                throw;
            }
            finally
            {
                log.content = sb.ToString();
                log.pid = nightscout.Id;
                await this.Db.Insertable<NightscoutLog>(log).ExecuteCommandAsync();
            }
        }

        public async Task RunDocker(Nightscout nightscout, NightscoutServer nsserver)
        {
            try
            {

                if (string.IsNullOrEmpty(nightscout.serviceName) || string.IsNullOrEmpty(nightscout.url)) return;

                NightscoutLog log = new NightscoutLog();
                StringBuilder sb = new StringBuilder();
                try
                {
                    string mongoDB = AppSettings.app(new string[] { "nightscout", "mongoDB" }).ObjToString();
                    int mongoPort = AppSettings.app(new string[] { "nightscout", "mongoPort" }).ObjToInt();

                    string path = AppSettings.app(new string[] { "nightscout", "path" }).ObjToString();
                    string MAKER_KEY = AppSettings.app(new string[] { "nightscout", "MAKER_KEY" }).ObjToString();
                    string CUSTOM_TITLE = AppSettings.app(new string[] { "nightscout", "CUSTOM_TITLE" }).ObjToString();

                    string cer = AppSettings.app(new string[] { "nightscout", "cer" }).ObjToString();

                    string key = AppSettings.app(new string[] { "nightscout", "key" }).ObjToString();

                    string pushUrl = AppSettings.app(new string[] { "nightscout", "pushUrl" }).ObjToString();

                    var webConfig = @$"
server {{
    listen 443 ssl http2;    
    server_name {nightscout.url} {nightscout.backupurl};

    ssl_certificate ""/etc/nginx/conf.d/{cer}"";
    ssl_certificate_key ""/etc/nginx/conf.d/{key}"";

    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ALL:!DH:!EXPORT:!RC4:+HIGH:+MEDIUM:!eNULL;
    ssl_prefer_server_ciphers on;

    location / {{
        proxy_set_header   Host             $host;
        proxy_set_header   X-Real-IP        $remote_addr;
        proxy_set_header   X-Forwarded-For  $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 300s;
        proxy_send_timeout 300s;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
        proxy_redirect off;
        proxy_http_version 1.1;
        proxy_pass http://{(nightscout.exposedPort > 0 ? nsserver.serverIp : nightscout.instanceIP)}:{(nightscout.exposedPort > 0 ? nightscout.exposedPort : 1337)};
    }}
    
}}
";
                    FileHelper.WriteFile($"/etc/nginx/conf.d/nightscout/{nightscout.Id}.conf", webConfig);



                    //这儿会有两种安装方式
                    //一是按服务器的IP+端口部署(ns和nginx在同一服务器)
                    //二是按本机实例IP+1337端口部署



                    using (var sshClient = new SshClient(nsserver.serverIp, nsserver.serverPort, nsserver.serverLoginName, nsserver.serverLoginPassword))
                    {
                        //创建SSH
                        sshClient.Connect();

                        using (var cmd = sshClient.CreateCommand(""))
                        {
                            //刷新nginx
                            
                            var master = (await _nightscoutServerServices.Query(t => t.isNginx == true)).FirstOrDefault();
                            if(master != null)
                            {
                                using (var sshMasterClient = new SshClient(master.serverIp, master.serverPort, master.serverLoginName, master.serverLoginPassword))
                                {
                                    sshMasterClient.Connect();
                                    using (var cmdMaster = sshMasterClient.CreateCommand(""))
                                    {
                                        var resMaster = cmdMaster.Execute("docker exec -t nginxserver nginx -s reload");
                                        sb.AppendLine($"刷新域名:{resMaster}");
                                    }
                                    sshMasterClient.Disconnect();
                                }
                            }
                            else
                            {
                                sb.AppendLine("没有找到nginx服务器");
                            }

                            //停止实例
                            var res = cmd.Execute($"docker stop {nightscout.serviceName}");
                            sb.AppendLine($"停止实例:{res}");

                            //删除实例
                            res = cmd.Execute($"docker rm {nightscout.serviceName}");
                            sb.AppendLine($"删除实例:{res}");

                            //启动实例
                            List<string> args = new List<string>();
                            if (nightscout.exposedPort > 0)
                            {
                                //外网
                                args.Add($"docker run -m 100m --cpus=1 --restart=always --net mynet -p {nightscout.exposedPort}:1337 --name {nightscout.serviceName}");
                            }
                            else
                            {
                                //内网
                                args.Add($"docker run -m 100m --cpus=1 --restart=always --net mynet --ip {nightscout.instanceIP} --name {nightscout.serviceName}");
                            }
                            args.Add($"-e TZ=Asia/Shanghai");
                            args.Add($"-e NODE_ENV=production");
                            args.Add($"-e INSECURE_USE_HTTP='true'");

                            //数据库链接
                            var connectionMongoString = $"mongodb://{nsserver.mongoLoginName}:{nsserver.mongoLoginPassword}@{nsserver.mongoIp}:{nsserver.mongoPort}/{nightscout.serviceName}";

                            args.Add($"-e MONGO_CONNECTION={connectionMongoString}");
                            args.Add($"-e API_SECRET={nightscout.passwd}");
                            //args.Add($"-v {path}/logo2.png:/opt/app/static/images/logo2.png");
                            //args.Add($"-v {path}/boluswizardpreview.js:/opt/app/lib/plugins/boluswizardpreview.js");
                            //args.Add($"-v {path}/sandbox.js:/opt/app/lib/sandbox.js");
                            //args.Add($"-v {path}/constants.json:/opt/app/lib/constants.json");
                            //args.Add($"-v {path}/zh_CN.json:/opt/app/translations/zh_CN.json");
                            //args.Add($"-v {path}/maker.js:/opt/app/lib/plugins/maker.js");
                            //args.Add($"-v {path}/hashauth.js:/opt/app/lib/client/hashauth.js");
                            //args.Add($"-v {path}/enclave.js:/opt/app/lib/server/enclave.js");
                            if (nightscout.isConnection)
                            {
                                args.Add($"-e MAKER_KEY={MAKER_KEY}");
                                if (nightscout.isKeepPush)
                                {
                                    args.Add($"-e KEEP_PUSH='true'");
                                }
                                args.Add($"-e PUSH_URL='{pushUrl}'");
                            }
                            args.Add($"-e LANGUAGE=zh_cn");
                            args.Add($"-e DISPLAY_UNITS='mmol/L'");
                            args.Add($"-e TIME_FORMAT=24");
                            args.Add($"-e CUSTOM_TITLE='{CUSTOM_TITLE}'");
                            args.Add($"-e THEME=colors");


                            List<string> pluginsArr;
                            try
                            {
                                var pluginsNights = JsonHelper.JsonToObj<List<string>>(nightscout.plugins.ObjToString());
                                if (pluginsNights != null && pluginsNights.Count > 0)
                                {
                                    pluginsArr = pluginsNights;
                                }
                                else
                                {
                                    pluginsArr = AppSettings.app<NSPlugin>(new string[] { "nightscout", "plugins" }).Select(t => t.key).ToList();
                                }
                            }
                            catch (Exception)
                            {
                                pluginsArr = AppSettings.app<NSPlugin>(new string[] { "nightscout", "plugins" }).Select(t => t.key).ToList();
                            }
                            args.Add($"-e SHOW_PLUGINS='{string.Join(" ", pluginsArr)}'");
                            args.Add($"-e ENABLE='{string.Join(" ", pluginsArr)}'");

                            //args.Add($"-e SHOW_PLUGINS='careportal basal dbsize rawbg iob maker cob bridge bwp cage iage sage boluscalc pushover treatmentnotify mmconnect loop pump profile food openaps bage alexa override cors'");
                            //args.Add($"-e ENABLE='careportal basal dbsize rawbg iob maker cob bridge bwp cage iage sage boluscalc pushover treatmentnotify mmconnect loop pump profile food openaps bage alexa override cors'");

                            args.Add($"-e AUTH_DEFAULT_ROLES=readable");
                            args.Add($"-e uid={nightscout.Id}");

                            //苹果远程指令
                            string apKeyID = AppSettings.app(new string[] { "appleRemote", "apKeyID" }).ObjToString();
                            string apKey = AppSettings.app(new string[] { "appleRemote", "apKey" }).ObjToString();
                            string teamID = AppSettings.app(new string[] { "appleRemote", "teamID" }).ObjToString();
                            string env = AppSettings.app(new string[] { "appleRemote", "env" }).ObjToString();
                            args.Add($"-e LOOP_APNS_KEY_ID='{apKeyID}'");
                            args.Add($"-e LOOP_APNS_KEY='{apKey}'");
                            args.Add($"-e LOOP_DEVELOPER_TEAM_ID='{teamID}'");
                            args.Add($"-e LOOP_PUSH_SERVER_ENVIRONMENT='{env}'");

                            //args.Add($"-d nightscout/cgm-remote-monitor:latest");
                            args.Add($"-d mynightscout:latest");

                            var cmdStr = string.Join(" ", args);

                            res = cmd.Execute(cmdStr);
                            sb.AppendLine($"启动实例:{res}");
                        }

                        sshClient.Disconnect();
                    }
                    log.success = true;
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"启动实例错误:{ex.Message}");
                    log.success = false;
                    LogHelper.Error(ex.Message);
                    LogHelper.Error(ex.StackTrace);

                    throw;
                }
                finally
                {
                    log.content = sb.ToString();
                    log.pid = nightscout.Id;
                    await this.Db.Insertable<NightscoutLog>(log).ExecuteCommandAsync();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message);
                LogHelper.Error(ex.StackTrace);
                throw;
            }
        }
    }
}