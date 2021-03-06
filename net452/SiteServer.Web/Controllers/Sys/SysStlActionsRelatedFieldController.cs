﻿using System.Text;
using System.Web;
using System.Web.Http;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.RestRoutes.Sys.Stl;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Plugin.Impl;
using SiteServer.Plugin;
using SiteServer.Utils;

namespace SiteServer.API.Controllers.Sys
{
    public class SysStlActionsRelatedFieldController : ApiController
    {
        [HttpPost, Route(ApiRouteActionsRelatedField.Route)]
        public void Main(int siteId)
        {
            var rest = Request.GetAuthenticatedRequest();

            var callback = Request.GetQueryString("callback");
            var relatedFieldId = Request.GetQueryInt("relatedFieldId");
            var parentId = Request.GetQueryInt("parentId");
            var jsonString = GetRelatedField(relatedFieldId, parentId);
            var call = callback + "(" + jsonString + ")";

            HttpContext.Current.Response.Write(call);
            HttpContext.Current.Response.End();
        }

        public string GetRelatedField(int relatedFieldId, int parentId)
        {
            var jsonString = new StringBuilder();

            jsonString.Append("[");

            var list = DataProvider.RelatedFieldItem.GetRelatedFieldItemInfoList(relatedFieldId, parentId);
            if (list.Count > 0)
            {
                foreach (var itemInfo in list)
                {
                    jsonString.AppendFormat(@"{{""id"":""{0}"",""name"":""{1}"",""value"":""{2}""}},", itemInfo.Id, StringUtils.ToJsString(itemInfo.ItemName), StringUtils.ToJsString(itemInfo.ItemValue));
                }
                jsonString.Length -= 1;
            }

            jsonString.Append("]");
            return jsonString.ToString();
        }
    }
}
