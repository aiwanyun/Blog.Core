using Blog.Core.Common.Helper;
using Blog.Core.IServices;
using Blog.Core.IServices.BASE;
using Blog.Core.Model;
using Blog.Core.Model.Models;
using Blog.Core.Model.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Core.Controllers
{
    /// <summary>
	/// WeChatKeywordController
	/// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Permissions.Name)]
    public partial class WeChatKeywordController : Controller
    {
        readonly IBaseServices<WeChatKeyword> _wechatKeywordServices;
        private readonly IWeChatConfigServices _weChatConfigServices;
        public WeChatKeywordController(IBaseServices<WeChatKeyword> wechatKeywordServices, IWeChatConfigServices weChatConfigServices)
        {
            _wechatKeywordServices = wechatKeywordServices;
            _weChatConfigServices = weChatConfigServices;
        } 
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="pagination">分页条件</param> 
        /// <returns></returns>
        [HttpGet]  
        public async Task<MessageModel<PageModel<WeChatKeyword>>> Get([FromQuery] PaginationModel pagination)
        {
            pagination.OrderByFileds = "Id desc";
            var data = await _wechatKeywordServices.QueryPage(pagination);
            return new MessageModel<PageModel<WeChatKeyword>> { success = true, response = data};
        }  
        /// <summary>
        /// 获取(id)
        /// </summary>
        /// <param name="id">主键ID</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<MessageModel<WeChatKeyword>> Get(string id)
        {
            var data = await _wechatKeywordServices.QueryById(id);
            return new MessageModel<WeChatKeyword> { success = true, response = data };
        } 
        /// <summary>
        /// 添加
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<MessageModel<string>> Post([FromBody] WeChatKeyword obj)
        {
            await _wechatKeywordServices.Add(obj);
            return new MessageModel<string> { success = true};
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public async Task<MessageModel<string>> Put([FromBody] WeChatKeyword obj)
        {
            await _wechatKeywordServices.Update(obj);
            return new MessageModel<string> { success = true};
        }
        /// <summary>
        /// 删除
        /// </summary> 
        /// <returns></returns> 
        [HttpDelete]
        public async Task<MessageModel<string>> Delete(string id)
        {
            await _wechatKeywordServices.DeleteById(id);
            return new MessageModel<string> { success = true};
        }
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public async Task<MessageModel<string>> BatchDelete(string ids)
        {
            var i = ids.Split(",");
            await _wechatKeywordServices.DeleteByIds(i);
            return new MessageModel<string> { success = true };
        }

        [HttpPost]
        public async Task<MessageModel<WeChatApiDto>> UpdateWeChatFile([FromQuery] string id,string type, IFormCollection form)
        {

            var res = await _weChatConfigServices.GetToken(id);
            if (!res.success) return res;

           
            var data = await WeChatHelper.UploadMedia(res.response.access_token, type,form);
            if (data.errcode.Equals(0))
            {
                if ("video".Equals(type))
                {
                    var info = await WeChatHelper.GetMediaInfo(res.response.access_token, data.media_id);
                    data.url = info.down_url;
                }
                return MessageModel<WeChatApiDto>.Success("上传成功", data);
            }
            else
            {
                return MessageModel<WeChatApiDto>.Success($"上传失败:{data.errmsg}", data);
            }
                
        }
        [HttpGet]
        public async Task<MessageModel<WeChatApiDto>> GetWeChatMediaList([FromQuery] string id,string type = "image", int page = 1, int size = 10)
        {

            var res = await _weChatConfigServices.GetToken(id);
            if (!res.success) return res;

            var data = await WeChatHelper.GetMediaList(res.response.access_token,type,page,size);
            if (data.errcode.Equals(0))
            {
                if ("video".Equals(type))
                {
                    foreach (var item in data.item)
                    {
                        var info = await WeChatHelper.GetMediaInfo(res.response.access_token, item.media_id);
                        item.url = data.url = info.down_url;
                    }

                }
                return MessageModel<WeChatApiDto>.Success("获取成功", data);
            }
            else
            {
                return MessageModel<WeChatApiDto>.Success($"获取失败:{data.errmsg}", data);
            }

        }
        
            

    }
}