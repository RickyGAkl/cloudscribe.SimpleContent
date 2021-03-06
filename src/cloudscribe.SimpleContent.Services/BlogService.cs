﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-02-09
// Last Modified:           2016-03-20
// 

using cloudscribe.SimpleContent.Common;
using cloudscribe.SimpleContent.Models;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace cloudscribe.SimpleContent.Services
{
    public class BlogService : IBlogService
    {
        public BlogService(
            IProjectService projectService,
            IPostRepository blogRepository,
            IMediaProcessor mediaProcessor,
            IUrlHelper urlHelper,
            IHttpContextAccessor contextAccessor = null)
        {
            repo = blogRepository;
            context = contextAccessor?.HttpContext;
            this.mediaProcessor = mediaProcessor;
            //settingsResolver = blogSettingsResolver;
            this.urlHelper = urlHelper;
            //this.settingsRepo = settingsRepo;
            this.projectService = projectService;
            htmlProcessor = new HtmlProcessor();
        }

        private IProjectService projectService;
        private readonly HttpContext context;
        private CancellationToken CancellationToken => context?.RequestAborted ?? CancellationToken.None;
        private IUrlHelper urlHelper;
        private IPostRepository repo;
        private IMediaProcessor mediaProcessor;
       // private IProjectSettingsRepository settingsRepo;
       // private IProjectSettingsResolver settingsResolver;
        private ProjectSettings settings = null;
        private bool userIsBlogOwner = false;
        private HtmlProcessor htmlProcessor;

        private async Task<bool> EnsureBlogSettings()
        {
            if(settings != null) { return true; }
            settings = await projectService.GetCurrentProjectSettings().ConfigureAwait(false);
            if (settings != null)
            {
                if(context.User.Identity.IsAuthenticated)
                {
                    var userBlog = context.User.GetProjectId();
                    if(!string.IsNullOrEmpty(userBlog))
                    {
                        if(settings.ProjectId == userBlog) { userIsBlogOwner = true; }

                    }
                }

                return true;
            }
            return false;
        }

        //public async Task<ProjectSettings> GetCurrentBlogSettings()
        //{
        //    await EnsureBlogSettings().ConfigureAwait(false);
        //    return settings;
        //}

        //public async Task<List<ProjectSettings>> GetUserProjects(string userName)
        //{
        //    //await EnsureBlogSettings().ConfigureAwait(false);
        //    //return settings;
        //    return await projectService.GetUserProjects(userName).ConfigureAwait(false);
        //}

        //public async Task<ProjectSettings> GetProjectSettings(string projectId)
        //{
        //    //await EnsureBlogSettings().ConfigureAwait(false);
        //    //return settings;
        //    return await projectService.GetProjectSettings(projectId).ConfigureAwait(false);
        //}

        //public async Task<List<Post>> GetAllPosts()
        //{
        //    await EnsureBlogSettings().ConfigureAwait(false);

        //    return await repo.GetAllPosts(settings.BlogId, CancellationToken).ConfigureAwait(false);
        //}

        public async Task<List<Post>> GetVisiblePosts()
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetVisiblePosts(
                settings.ProjectId,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<Post>> GetVisiblePosts(
            string category,
            int pageNumber)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetVisiblePosts(
                settings.ProjectId,
                category,
                userIsBlogOwner,
                pageNumber,
                settings.PostsPerPage,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> GetCount(string category)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetCount(
                settings.ProjectId,
                category,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> GetCount(
            string projectId,
            int year,
            int month = 0,
            int day = 0)
        {
            return await repo.GetCount(
                projectId,
                year,
                month,
                day,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<Post>> GetRecentPosts(int numberToGet)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetRecentPosts(
                settings.ProjectId,
                numberToGet,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<Post>> GetRecentPosts(string projectId, int numberToGet)
        {
            //await EnsureBlogSettings().ConfigureAwait(false);
            //var settings = await settingsRepo.GetProjectSettings(projectId, CancellationToken).ConfigureAwait(false);

            return await repo.GetRecentPosts(
                projectId,
                numberToGet,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<Post>> GetPosts(
            string projectId, 
            int year, 
            int month = 0, 
            int day = 0, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            return await repo.GetPosts(projectId, year, month, day, pageNumber, pageSize).ConfigureAwait(false);
        }

        public async Task Save(
            string projectId, 
            Post post, 
            bool isNew, 
            bool publish)
        {
            var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            if(isNew)
            {
                await InitializeNewPosts(projectId, post, publish);
            }


            //contextAccessor
            var imageAbsoluteBaseUrl = urlHelper.Content("~" + settings.LocalMediaVirtualPath);
            if(context != null)
            {
                imageAbsoluteBaseUrl = context.Request.AppBaseUrl() + settings.LocalMediaVirtualPath;
            }

            // open live writer passes in posts with absolute urls
            // we want to change them to relative to keep the files portable
            // to a different root url
            post.Content = await htmlProcessor.ConvertMediaUrlsToRelative(
                settings.LocalMediaVirtualPath,
                imageAbsoluteBaseUrl, //this shold be resolved from virtual using urlhelper
                post.Content);

            // here we need to process any base64 embedded images
            // save them under wwwroot
            // and update the src in the post with the new url
            // since this overload of Save is only called from metaweblog
            // and metaweblog does not base64 encode the images like the browser client
            // this call may not be needed here
            await mediaProcessor.ConvertBase64EmbeddedImagesToFilesWithUrls(
                settings.LocalMediaVirtualPath,
                post
                ).ConfigureAwait(false);

            var nonPublishedDate = new DateTime(1, 1, 1);
            if (post.PubDate == nonPublishedDate)
            {
                post.PubDate = DateTime.UtcNow;
            }

            await repo.Save(settings.ProjectId, post, isNew).ConfigureAwait(false);
        }

        public async Task Save(Post post, bool isNew)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            // here we need to process any base64 embedded images
            // save them under wwwroot
            // and update the src in the post with the new url
            await mediaProcessor.ConvertBase64EmbeddedImagesToFilesWithUrls(
                settings.LocalMediaVirtualPath,
                post
                ).ConfigureAwait(false);

            var nonPublishedDate = new DateTime(1, 1, 1);
            if(post.PubDate == nonPublishedDate)
            {
                post.PubDate = DateTime.UtcNow;
            }

            await repo.Save(settings.ProjectId, post, isNew).ConfigureAwait(false);
        }

        public async Task HandlePubDateAboutToChange(Post post, DateTime newPubDate)
        {
            await repo.HandlePubDateAboutToChange(post, newPubDate);
        }

        private async Task InitializeNewPosts(string projectId, Post post, bool publish)
        {
            if(publish)
            {
                post.PubDate = DateTime.UtcNow;
            }

            if(string.IsNullOrEmpty(post.Slug))
            {
                var slug = CreateSlug(post.Title);
                var available = await SlugIsAvailable(slug);
                if (available)
                {
                    post.Slug = slug;
                }

            }
        }

        public async Task<string> ResolveMediaUrl(string fileName)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return settings.LocalMediaVirtualPath + fileName;
        }

        public Task<string> ResolvePostUrl(Post post)
        {
            //await EnsureBlogSettings().ConfigureAwait(false);


            var result = urlHelper.Action("Post", "Blog", new { slug = post.Slug });

            return Task.FromResult(result);
        }

        

        public Task<string> ResolveBlogUrl(ProjectSettings blog)
        {
            //await EnsureBlogSettings().ConfigureAwait(false);


            var result = urlHelper.Action("Index", "Blog");

            return Task.FromResult(result);
        }


        public async Task<Post> GetPost(string postId)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetPost(
                settings.ProjectId,
                postId,
                CancellationToken)
                .ConfigureAwait(false);

        }

        public async Task<Post> GetPost(string projectId, string postId)
        {
           // await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetPost(
                projectId,
                postId,
                CancellationToken)
                .ConfigureAwait(false);

        }

        public async Task<Post> GetPostBySlug(string slug)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetPostBySlug(
                settings.ProjectId,
                slug,
                CancellationToken)
                .ConfigureAwait(false);

        }

        public string CreateSlug(string title)
        {
            return ContentUtils.CreateSlug(title);
        }

        public async Task<bool> SlugIsAvailable(string slug)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.SlugIsAvailable(
                settings.ProjectId,
                slug,
                CancellationToken)
                .ConfigureAwait(false);
        }
        
        public async Task<bool> SlugIsAvailable(string projectId, string slug)
        {
            

            return await repo.SlugIsAvailable(
                projectId,
                slug,
                CancellationToken)
                .ConfigureAwait(false);
        }

        

        public async Task<bool> Delete(string postId)
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.Delete(settings.ProjectId, postId).ConfigureAwait(false);

        }

        public async Task<bool> Delete(string projectId, string postId)
        {
            //await EnsureBlogSettings().ConfigureAwait(false);
            //var settings = await settingsRepo.GetBlogSetings(projectId, CancellationToken).ConfigureAwait(false);

            return await repo.Delete(projectId, postId).ConfigureAwait(false);

        }

        public async Task<Dictionary<string, int>> GetCategories()
        {
            await EnsureBlogSettings().ConfigureAwait(false);

            return await repo.GetCategories(
                settings.ProjectId,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<Dictionary<string, int>> GetCategories(string projectId, bool userIsOwner)
        {
            var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            return await repo.GetCategories(
                settings.ProjectId,
                userIsOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<Dictionary<string, int>> GetArchives()
        {
            await EnsureBlogSettings().ConfigureAwait(false);
            //var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            return await repo.GetArchives(
                settings.ProjectId,
                userIsBlogOwner,
                CancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> CommentsAreOpen(Post post, bool userIsOwner)
        {
            if(userIsBlogOwner) { return true; }
            await EnsureBlogSettings().ConfigureAwait(false);

            var result = post.PubDate > DateTime.UtcNow.AddDays(-settings.DaysToComment);
            return result;
        }

        public async Task SaveMedia(string projectId, byte[] bytes, string fileName)
        {
            var settings = await projectService.GetProjectSettings(projectId).ConfigureAwait(false);

            await mediaProcessor.SaveMedia(settings.LocalMediaVirtualPath, fileName, bytes).ConfigureAwait(false);
        }

        
    }
}
