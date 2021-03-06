﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-02-09
// Last Modified:           2016-02-27
// 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cloudscribe.SimpleContent.Models
{
    
    public class ProjectSettings
    {
        public ProjectSettings()
        {
        }

        public string ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public int PostsPerPage { get; set; }
        public int DaysToComment { get; set; }
        public bool ModerateComments { get; set; }
        public string LocalMediaVirtualPath { get; set; } = string.Empty;
        public string LocalMediaAbsoluteBaseUrl { get; set; } = string.Empty;
        public string CdnUrl { get; set; } = string.Empty;
        public int GravatarSize { get; set; } = 50;
        public string PubDateFormat { get; set; } = "MMMM d. yyyy";
        public bool IncludePubDateInPostUrls { get; set; } = true;
        private string timeZoneId = "Eastern Standard Time";
        public string TimeZoneId
        {
            get { return timeZoneId ?? "Eastern Standard Time"; }
            set { timeZoneId = value; }
        }
        public string RecaptchaPublicKey { get; set; }
        public string RecaptchaPrivateKey { get; set; }
        public string DefaultPageSlug { get; set; } = "home";
        public bool UseDefaultPageAsRootNode { get; set; } = true;
        public string AllowedEditRoles { get; set; } = "Admins";
        public bool ShowTitle { get; set; } = true;

        // if true automatically add the blog index
        public bool AddBlogToPagesTree { get; set; } = true;
        public int BlogPagePosition { get; set; } = 2; // right after home page
        public string BlogPageText { get; set; } = "Blog";
        public string BlogPageNavComponentVisibility { get; set; } = "topnav";

    }
}
