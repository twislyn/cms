﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.BackgroundPages.Core;
using SiteServer.BackgroundPages.Utils;
using SiteServer.CMS.Caches;
using SiteServer.Utils;
using SiteServer.CMS.Core.Enumerations;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Database.Models;
using SiteServer.CMS.Fx;
using SiteServer.Utils.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
    public class ModalCrossSiteTransEdit : BasePageCms
    {
        public DropDownList DdlTransType;
        public PlaceHolder PhSite;
        public DropDownList DdlSiteId;
        public ListBox LbChannelId;
        public PlaceHolder PhNodeNames;
        public TextBox TbNodeNames;
        public PlaceHolder PhIsAutomatic;
        public DropDownList DdlIsAutomatic;
        public DropDownList DdlTranslateDoneType;

        private ChannelInfo _channelInfo;

        public static string GetOpenWindowString(int siteId, int channelId)
        {
            return LayerUtils.GetOpenScript("跨站转发设置", FxUtils.GetCmsUrl(siteId, nameof(ModalCrossSiteTransEdit), new NameValueCollection
            {
                {"channelId", channelId.ToString()}
            }));
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            WebPageUtils.CheckRequestParameter("siteId", "channelId");
            var channelId = int.Parse(AuthRequest.GetQueryString("channelId"));
            _channelInfo = ChannelManager.GetChannelInfo(SiteId, channelId);

            if (IsPostBack) return;

            SystemWebUtils.AddAllListItemsToECrossSiteTransType(DdlTransType, SiteInfo.ParentId > 0);

            SystemWebUtils.SelectSingleItem(DdlTransType, _channelInfo.TransType);

            DdlTransType_OnSelectedIndexChanged(null, EventArgs.Empty);
            SystemWebUtils.SelectSingleItem(DdlSiteId, _channelInfo.TransSiteId.ToString());


            DdlSiteId_OnSelectedIndexChanged(null, EventArgs.Empty);
            SystemWebUtils.SelectMultiItems(LbChannelId, TranslateUtils.StringCollectionToStringList(_channelInfo.TransChannelIds));
            TbNodeNames.Text = _channelInfo.TransChannelNames;

            FxUtils.AddListItems(DdlIsAutomatic, "系统自动转发", "需手动操作");
            SystemWebUtils.SelectSingleItemIgnoreCase(DdlIsAutomatic, _channelInfo.TransIsAutomatic.ToString());

            SystemWebUtils.AddListItemsToETranslateContentType(DdlTranslateDoneType, false);
            SystemWebUtils.SelectSingleItem(DdlTranslateDoneType, _channelInfo.TransDoneType);
        }

        protected void DdlTransType_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            DdlSiteId.Items.Clear();
            DdlSiteId.Enabled = true;

            PhIsAutomatic.Visible = false;

            var contributeType = ECrossSiteTransTypeUtils.GetEnumType(DdlTransType.SelectedValue);
            if (contributeType == ECrossSiteTransType.None)
            {
                PhSite.Visible = PhNodeNames.Visible = false;
            }
            else if (contributeType == ECrossSiteTransType.SelfSite || contributeType == ECrossSiteTransType.SpecifiedSite)
            {
                PhSite.Visible = true;
                PhNodeNames.Visible = false;

                PhIsAutomatic.Visible = true;
            }
            else if (contributeType == ECrossSiteTransType.ParentSite)
            {
                PhSite.Visible = true;
                PhNodeNames.Visible = false;
                DdlSiteId.Enabled = false;

                PhIsAutomatic.Visible = true;
            }
            else if (contributeType == ECrossSiteTransType.AllParentSite || contributeType == ECrossSiteTransType.AllSite)
            {
                PhSite.Visible = false;
                PhNodeNames.Visible = true;
            }

            if (PhSite.Visible)
            {
                var siteIdList = SiteManager.GetSiteIdList();

                var allParentSiteIdList = new List<int>();
                if (contributeType == ECrossSiteTransType.AllParentSite)
                {
                    SiteManager.GetAllParentSiteIdList(allParentSiteIdList, siteIdList, SiteId);
                }
                else if (contributeType == ECrossSiteTransType.SelfSite)
                {
                    siteIdList = new List<int>
                    {
                        SiteId
                    };
                }

                foreach (var psId in siteIdList)
                {
                    var psInfo = SiteManager.GetSiteInfo(psId);
                    var show = false;
                    if (contributeType == ECrossSiteTransType.SpecifiedSite)
                    {
                        show = true;
                    }
                    else if (contributeType == ECrossSiteTransType.SelfSite)
                    {
                        if (psId == SiteId)
                        {
                            show = true;
                        }
                    }
                    else if (contributeType == ECrossSiteTransType.ParentSite)
                    {
                        if (psInfo.Id == SiteInfo.ParentId || (SiteInfo.ParentId == 0 && psInfo.Root))
                        {
                            show = true;
                        }
                    }
                    if (!show) continue;

                    var listitem = new ListItem(psInfo.SiteName, psId.ToString());
                    if (psInfo.Root) listitem.Selected = true;
                    DdlSiteId.Items.Add(listitem);
                }
            }
            DdlSiteId_OnSelectedIndexChanged(sender, e);
        }

        protected void DdlSiteId_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            LbChannelId.Items.Clear();
            if (PhSite.Visible && DdlSiteId.Items.Count > 0)
            {
                SystemWebUtils.AddListItemsForAddContent(LbChannelId.Items, SiteManager.GetSiteInfo(int.Parse(DdlSiteId.SelectedValue)), false, AuthRequest.AdminPermissionsImpl);
            }
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            var isSuccess = false;

            try
            {
                _channelInfo.TransType = DdlTransType.SelectedValue;
                _channelInfo.TransSiteId = ECrossSiteTransTypeUtils.Equals(_channelInfo.TransType, ECrossSiteTransType.SpecifiedSite) ? TranslateUtils.ToInt(DdlSiteId.SelectedValue) : 0;
                _channelInfo.TransChannelIds = SystemWebUtils.GetSelectedListControlValueCollection(LbChannelId);
                _channelInfo.TransChannelNames = TbNodeNames.Text;

                _channelInfo.TransIsAutomatic = TranslateUtils.ToBool(DdlIsAutomatic.SelectedValue);

                _channelInfo.TransDoneType = DdlTranslateDoneType.SelectedValue;

                DataProvider.Channel.Update(_channelInfo);

                AuthRequest.AddSiteLog(SiteId, "修改跨站转发设置");

                isSuccess = true;
            }
            catch (Exception ex)
            {
                FailMessage(ex, ex.Message);
            }

            if (isSuccess)
            {
                LayerUtils.Close(Page);
            }
        }
    }
}
