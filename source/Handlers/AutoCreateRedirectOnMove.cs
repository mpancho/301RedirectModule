﻿using SharedSource.RedirectModule.Helpers;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Links;
using Sitecore.SecurityModel;
using System;
using System.Linq;

namespace SharedSource.RedirectModule.Handlers
{
    public class AutoCreateRedirectOnMove
    {
        string pathNeamb = "/neamb/home";
        string pathSeiumb = "/neambc/home";
        string pathSeiumbV2 = "/seiumb/home";

        protected void OnItemMoved(object sender, EventArgs args)
        {
            //ensures arguments aren't null
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            if (Sitecore.Configuration.Settings.GetBoolSetting(Constants.Settings.AutoGenerateRedirectsOnMove, true))
            {
                Item item = Event.ExtractParameter<Item>(args, 0);
                ID oldParentID = Event.ExtractParameter<ID>(args, 1);

                // do not create redirect if path has not changed
                if (oldParentID == item.ParentID) return;

                using (new SecurityDisabler())
                {
                    CreateRedirectItem(item, item.Database.GetItem(oldParentID));
                }
            }
        }

        public virtual void CreateRedirectItem(Item item, Item oldParent)
        {
            // we only want a redirect on pages and media assets
            if (oldParent.Paths.IsContentItem || oldParent.Paths.IsMediaItem)
            {
                var parentPath = LinkManager.GetItemUrl(oldParent).Replace("/sitecore/shell", "");
                if (!parentPath.Equals("/") && parentPath.Length > 0)
                    parentPath += "/";
                string oldPath = parentPath + LinkManager.GetItemUrl(item).Split('/').Last();

                Database db = Sitecore.Configuration.Factory.GetDatabase("master");
                // Get the generated folder underneath the redirects folder.  It is a bucketed item.
                Item parentItem = db.GetItem(new ID("{46CE2092-FF8D-454E-B826-A2ADDB7E0BA3}"));

                if (parentPath.Contains(pathNeamb))
                {
                    if (parentPath.StartsWith(pathNeamb)) oldPath = oldPath.Replace(pathNeamb, "");
                    parentItem = db.GetItem(new ID("{5443128F-ED27-40A5-B84D-10C28B316800}"));
                }
                else if(parentPath.Contains(pathSeiumb))
                {
                    if (parentPath.StartsWith(pathSeiumb)) oldPath = oldPath.Replace(pathSeiumb, "");
                    parentItem = db.GetItem(new ID("{32322351-49C9-4679-9CCB-A3D4AEBC407D}"));
                }
                else if (parentPath.Contains(pathSeiumbV2))
                {
                    if (parentPath.StartsWith(pathSeiumbV2)) oldPath = oldPath.Replace(pathSeiumbV2, "");
                    parentItem = db.GetItem(new ID("{81A29287-A5E4-455F-86CD-FE75C3116B78}"));
                }

                if (parentItem != null)
                {
                    //Now we need to get the template from which the item is created (Redirect Url)
                    TemplateItem template = db.GetTemplate(new ID("{B5967A68-7F70-42D3-9874-0E4D001DBC20}"));

                    if (template != null)
                    {
                        // Create the item
                        Item newItem = parentItem.Add(ItemUtil.ProposeValidItemName(oldPath.Replace('/', ' ')), template);

                        newItem.Editing.BeginEdit();
                        try
                        {
                            // Assign values to the fields of the new item
                            newItem.Fields["Requested Url"].Value = oldPath;
                            newItem.Fields["Response Status Code"].Value = "{3184B308-C050-4A16-9F82-D77190A28F0F}";  // 301
                            newItem.Fields["Redirect To Item"].Value = item.ID.ToString();
                            newItem.Editing.EndEdit();
                        }
                        catch (System.Exception ex)
                        {
                            // The update failed, write a message to the log
                            Sitecore.Diagnostics.Log.Error("Could not update item " + newItem.Paths.FullPath + ": " + ex.Message, this);
                            newItem.Editing.CancelEdit();
                        }
                    }
                }
            }
        }
    }
}
