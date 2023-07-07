using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.Model.Models;
using Blog.Core.Services.BASE;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using Serilog;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using log4net.Plugin;
using Blog.Core.Model.ViewModels;
using System.Linq;
using Blog.Core.IServices.BASE;
using Renci.SshNet;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;

namespace Blog.Core.Services
{
    /// <summary>
    /// NightscoutServices
    /// </summary>	
    public class NightscoutServices : BaseServices<Nightscout>, INightscoutServices
    {

        private readonly IBaseServices<NightscoutLog> _nightscoutLogServices;
        private readonly IBaseServices<NightscoutServer> _nightscoutServerServices;
        public NightscoutServices(IBaseServices<NightscoutLog> nightscoutLogServices, IBaseServices<NightscoutServer> nightscoutServerServices)
        {
            _nightscoutLogServices = nightscoutLogServices;
            _nightscoutServerServices = nightscoutServerServices;
        }
        public async Task RunDocker(Nightscout nightscout, bool isDelete = false)
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
                    var nsserver = await _nightscoutServerServices.QueryById(nightscout.serverId);
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
        proxy_set_header X-Forwarded-Proto $scheme;
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
                    if (isDelete)
                    {
                        FileHelper.FileDel($"/etc/nginx/conf.d/nightscout/{nightscout.Id}.conf");
                    }
                    else
                    {
                        FileHelper.WriteFile($"/etc/nginx/conf.d/nightscout/{nightscout.Id}.conf", webConfig);
                    }



                    //这儿会有两种安装方式
                    //一是按服务器的IP+端口部署
                    //二是按本机实例IP+1337端口部署



                    using (var sshClient = new SshClient(nsserver.serverIp, nsserver.serverPort, nsserver.serverLoginName, nsserver.serverLoginPassword))
                    {
                        //创建SSH
                        sshClient.Connect();

                        using (var cmd = sshClient.CreateCommand(""))
                        {
                            //刷新nginx
                            
                            var master = (await _nightscoutServerServices.Query(t => t.isMaster == true)).FirstOrDefault();
                            if(master != null)
                            {
                                using (var sshMasterClient = new SshClient(master.serverIp, master.serverPort, master.serverLoginName, master.serverLoginPassword))
                                {
                                    sshMasterClient.Connect();
                                    using (var cmdMaster = sshMasterClient.CreateCommand(""))
                                    {
                                        var resMaster = cmdMaster.Execute("docker exec -t nginxserver nginx -s reload");
                                        sb.AppendLine(resMaster);
                                    }
                                }
                            }
                            else
                            {
                                sb.AppendLine("NGINX刷新失败");
                            }

                            //停止实例
                            var res = cmd.Execute($"docker stop {nightscout.serviceName}");
                            sb.AppendLine(res);

                            //删除实例
                            res = cmd.Execute($"docker rm {nightscout.serviceName}");
                            sb.AppendLine(res);


                            if (isDelete)
                            {
                                //删除mongo
                                var connectionMongoString = "";
                                if (!string.IsNullOrEmpty(nsserver.mongoLoginName))
                                {
                                    connectionMongoString = $"mongodb://{nsserver.mongoLoginName}:{nsserver.mongoLoginPassword}@{nsserver.mongoIp}:{nsserver.mongoPort}";
                                }
                                else
                                {
                                    connectionMongoString = $"mongodb://{nsserver.mongoIp}:{nsserver.mongoPort}";
                                }
                                var client = new MongoClient(connectionMongoString);
                                //client.Settings.UseTls = false;
                                //client.Settings.RetryWrites = false;
                                client.DropDatabase(nightscout.serviceName);

                                //var mongiSetting = new MongoClientSettings()
                                //{
                                //    UseTls = false,
                                //    RetryWrites = false,
                                //    Server = new MongoServerAddress(nsserver.mongoIp, nsserver.mongoPort),
                                //};
                                //var mongoClient = new MongoClient(mongiSetting);


                            }
                            else
                            {

                                //数据库授权
                                if (!string.IsNullOrEmpty(nsserver.mongoLoginName))
                                {
                                    var grantConnectionMongoString = $"mongodb://{nsserver.mongoLoginName}:{nsserver.mongoLoginPassword}@{nsserver.mongoIp}:{nsserver.mongoPort}";
                                    var client = new MongoClient(grantConnectionMongoString);
                                    var data = client.GetDatabase(nightscout.serviceName);

                                    var command = new BsonDocument
                                    {
                                        { "createUser", $"{nsserver.mongoLoginName}" },
                                        { "pwd" ,$"{nsserver.mongoLoginPassword}" },

                                        { "roles", new BsonArray
                                            {
                                                {"readWrite"}
                                            }
                                        }
                                    };
                                    try
                                    {
                                        var result = data.RunCommand<BsonDocument>(command);
                                        sb.AppendLine(result.ToString());
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Information(ex.Message);
                                    }
                                }
                                //启动实例
                                List<string> args = new List<string>();
                                if (nightscout.exposedPort > 0)
                                {
                                    args.Add($"docker run -m 100m --cpus=1 --restart=always --net mynet -p {nightscout.exposedPort}:1337 --name {nightscout.serviceName}");
                                }
                                else
                                {
                                    args.Add($"docker run -m 100m --cpus=1 --restart=always --net mynet --ip {nightscout.instanceIP} --name {nightscout.serviceName}");
                                }
                                args.Add($"-e TZ=Asia/Shanghai");
                                args.Add($"-e NODE_ENV=production");
                                args.Add($"-e INSECURE_USE_HTTP='true'");

                                //数据库链接 默认都是内部链接
                                var connectionMongoString = "";
                                if (!string.IsNullOrEmpty(nsserver.mongoLoginName))
                                {
                                    connectionMongoString = $"mongodb://{nsserver.mongoLoginName}:{nsserver.mongoLoginPassword}@{nsserver.mongoIp}:{nsserver.mongoPort}/{nightscout.serviceName}";
                                }
                                else
                                {
                                    connectionMongoString = $"mongodb://{nsserver.mongoIp}:27017/{nightscout.serviceName}";
                                }

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
                                    var pluginsNights = JsonConvert.DeserializeObject<List<string>>(nightscout.plugins.ObjToString());
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
                                //args.Add($"-d nightscout/cgm-remote-monitor:latest");
                                args.Add($"-d mynightscout:latest");

                                var cmdStr = string.Join(" ", args);

                                res = cmd.Execute(cmdStr);
                                sb.AppendLine(res);
                            }
                        }
                    }
                    log.success = true;
                }
                catch (Exception ex)
                {
                    log.success = false;
                    sb.AppendLine(ex.Message);
                    Log.Information(ex.Message);
                    Log.Information(ex.StackTrace);
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
                Log.Information(ex.Message);
                Log.Information(ex.StackTrace);
            }
        }
    }
}