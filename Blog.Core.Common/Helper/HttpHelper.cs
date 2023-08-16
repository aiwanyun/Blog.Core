using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Blog.Core.Common.Helper
{
    /// <summary>
    /// httpclinet请求方式，请尽量使用IHttpClientFactory方式
    /// </summary>
    public class HttpHelper
    {
        public static async Task<string> GetAsync(string serviceAddress)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    return await httpClient.GetStringAsync(serviceAddress);
                }
            }
            catch (Exception e)
            {
                LogHelper.Information("get请求失败", e);
            }
            return null;
        }

        public static async Task<string> PostAsync(string serviceAddress, string requestJson = null)
        {
            try
            {
                string result = string.Empty;
                using (HttpContent httpContent = new StringContent(requestJson))
                {
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    using (var httpClient = new HttpClient())
                    {
                        using (var response = await httpClient.PostAsync(serviceAddress, httpContent))
                        {
                            return await response.Content.ReadAsStringAsync();
                        }
                        
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Information("post请求失败", e);
            }
            return null;
        }
    }


}
