﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-02-09
// Last Modified:           2016-03-21
// 

using cloudscribe.SimpleContent.Common;
using cloudscribe.SimpleContent.Models;
using cloudscribe.SimpleContent.Web.ViewModels;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;
using Microsoft.Extensions.OptionsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cloudscribe.SimpleContent.Services;

namespace cloudscribe.SimpleContent.Web.Controllers
{
    public class BlogController : Controller
    {

        public BlogController(
            IProjectService projectService,
            IBlogService blogService,
            ILogger<BlogController> logger)
        {
            this.projectService = projectService;
            this.blogService = blogService;
            log = logger;
        }

        private IProjectService projectService;
        private IBlogService blogService;
        private ILogger log;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string category = "",
            int page = 1)
        {

            var model = new BlogViewModel();
            model.ProjectSettings = await projectService.GetCurrentProjectSettings();

            ViewData["Title"] = model.ProjectSettings.Title;

            model.Posts = await blogService.GetVisiblePosts(category, page);
            model.Categories = await blogService.GetCategories();
            model.Archives = await blogService.GetArchives();
            model.Paging.ItemsPerPage = model.ProjectSettings.PostsPerPage;
            model.Paging.CurrentPage = page;
            model.Paging.TotalItems = await blogService.GetCount(category);
            //TODO: fix https://github.com/joeaudette/cloudscribe.SimpleContent/issues/1
            try
            {
                model.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(model.ProjectSettings.TimeZoneId);
            }
            catch(Exception)
            {
                //temporary workaround for mac/linux
                model.TimeZone = TimeZoneInfo.Utc;
            }
            
            model.CanEdit = User.CanEditProject(model.ProjectSettings.ProjectId);
            if(model.CanEdit)
            {
                model.EditorSettings.NewItemPath = Url.Link("newpost", null);
                model.EditorSettings.EditPath = Url.Action("Post", "Blog", new { slug = "", mode = "new" });
                model.EditorSettings.CancelEditPath = Url.Action("Index", "Blog");
                model.EditorSettings.IndexUrl = Url.Action("Index", "Blog");
                model.EditorSettings.CurrentSlug = string.Empty;
                model.EditorSettings.SupportsCategories = true;
            }


            return View("Index", model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Archive(
            int year,
            int month = 0,
            int day = 0,
            int page = 1)
        {

            var model = new BlogViewModel();
            model.ProjectSettings = await projectService.GetCurrentProjectSettings();

            ViewData["Title"] = model.ProjectSettings.Title;

            model.Posts = await blogService.GetPosts(
                model.ProjectSettings.ProjectId,
                year,
                month,
                day,
                page,
                model.ProjectSettings.PostsPerPage
                );

            model.Categories = await blogService.GetCategories();
            model.Archives = await blogService.GetArchives();
            model.Paging.ItemsPerPage = model.ProjectSettings.PostsPerPage;
            model.Paging.CurrentPage = page;
            model.Paging.TotalItems = await blogService.GetCount(
                model.ProjectSettings.ProjectId,
                year,
                month,
                day);

            //TODO: fix https://github.com/joeaudette/cloudscribe.SimpleContent/issues/1
            try
            {
                model.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(model.ProjectSettings.TimeZoneId);
            }
            catch (Exception)
            {
                //temporary workaround for mac/linux
                model.TimeZone = TimeZoneInfo.Utc;
            }

            model.Year = year;
            model.Month = month;
            model.Day = day;

            model.CanEdit = User.CanEditProject(model.ProjectSettings.ProjectId);
            if (model.CanEdit)
            {
                model.EditorSettings.NewItemPath = Url.Link("newpost", null);
                model.EditorSettings.EditPath = Url.Action("Post", "Blog", new { slug = "", mode = "new" });
                model.EditorSettings.CancelEditPath = Url.Action("Index", "Blog");
                model.EditorSettings.IndexUrl = Url.Action("Index", "Blog");
                model.EditorSettings.CurrentSlug = string.Empty;
                model.EditorSettings.SupportsCategories = true;
            }


            return View("Archive", model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Category(
            string category = "",
            int pageNumber = 1)
        {
            return await Index(category, pageNumber);
        }

        [HttpGet]
        public async Task<IActionResult> New()
        {
            return await Post(0, 0, 0, "", "new");
        }

        [HttpGet]
        [AllowAnonymous]
        [ActionName("PostNoDate")]
        public async Task<IActionResult> Post(string slug, string mode = "")
        {
            return await Post(0, 0, 0, slug, mode);
        }

        [HttpGet]
        [AllowAnonymous]
        [ActionName("PostWithDate")]
        public async Task<IActionResult> Post(int year , int month, int day, string slug, string mode = "")
        {
            var projectSettings = await projectService.GetCurrentProjectSettings();

            if (projectSettings == null)
            {
                return RedirectToAction("Index");
            }

            if(!projectSettings.IncludePubDateInPostUrls)
            {
                if(year > 0)
                {
                    //TODO: an option for permanent redirect
                    return RedirectToRoute("postwithoutdate", new { slug = slug });
                }
            }

            var canEdit = User.CanEditProject(projectSettings.ProjectId);
            var isNew = false;
            Post post = null;
            if(!string.IsNullOrEmpty(slug))
            {
                post = await blogService.GetPostBySlug(slug);
            }
            
            var model = new BlogViewModel();

            if (post == null)
            {
                ViewData["Title"] = "New Post";
                if ((canEdit) && (mode.Length > 0))
                {
                    post = new Post();
                    post.BlogId = projectSettings.ProjectId;
                    isNew = true;
                }
                else
                {
                    return RedirectToAction("Index");
                }

            }
            else
            {
               if(projectSettings.IncludePubDateInPostUrls)
                {
                    if(year == 0)
                    {
                        //TODO: option whether to use permanent redirect
                        return RedirectToRoute("postwithdate", 
                            new {
                                year = post.PubDate.Year,
                                month = post.PubDate.Month.ToString("00"),
                                day = post.PubDate.Day.ToString("00"),
                                slug = post.Slug
                            });
                    }
                }

                ViewData["Title"] = post.Title;
            }

            model.Mode = mode;
            model.CurrentPost = post;
            model.ProjectSettings = projectSettings;
            model.Categories = await blogService.GetCategories();
            model.Archives = await blogService.GetArchives();
            model.CanEdit = canEdit;
            model.ShowComments = mode.Length == 0; // do we need this for a global disable
            model.CommentsAreOpen = await blogService.CommentsAreOpen(post, canEdit);
            //model.ApprovedCommentCount = post.Comments.Where(c => c.IsApproved == true).Count();
            //TODO: fix https://github.com/joeaudette/cloudscribe.SimpleContent/issues/1
            try
            {
                model.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(model.ProjectSettings.TimeZoneId);
            }
            catch (Exception)
            {
                //temporary workaround for mac/linux
                model.TimeZone = TimeZoneInfo.Utc;
            }

            if (canEdit)
            {
                if (isNew)
                {
                    model.EditorSettings.CancelEditPath = Url.Action("Index", "Blog");
                    model.EditorSettings.CurrentSlug = string.Empty;
                    model.EditorSettings.IsPublished = true;
                    model.EditorSettings.EditMode = "new";
                    //model.EditorSettings.EditPath = Url.Action("Post", "Blog", new { slug = model.CurrentPost.Slug, mode = "edit" });
                }
                else
                {
                    model.EditorSettings.EditMode = "edit";
                    model.EditorSettings.CurrentSlug = model.CurrentPost.Slug;
                    model.EditorSettings.IsPublished = model.CurrentPost.IsPublished;
                    if(model.ProjectSettings.IncludePubDateInPostUrls)
                    {
                        model.EditorSettings.EditPath = Url.Link("postwithdate",  
                            new {
                            year = model.CurrentPost.PubDate.Year,
                            month = model.CurrentPost.PubDate.Month.ToString("00"),
                            day = model.CurrentPost.PubDate.Day.ToString("00"),
                            slug = model.CurrentPost.Slug,
                            mode = "edit" });

                        model.EditorSettings.CancelEditPath = Url.Link("postwithdate",
                            new
                            {
                                year = model.CurrentPost.PubDate.Year,
                                month = model.CurrentPost.PubDate.Month.ToString("00"),
                                day = model.CurrentPost.PubDate.Day.ToString("00"),
                                slug = model.CurrentPost.Slug
                            });
                    }
                    else
                    {
                        model.EditorSettings.EditPath = Url.Link("postwithoutdate", new { slug = model.CurrentPost.Slug, mode = "edit" });
                        model.EditorSettings.CancelEditPath = Url.Link("postwithoutdate", new { slug = model.CurrentPost.Slug});
                    }
                    
                }

                model.EditorSettings.EditMode = mode;
                model.EditorSettings.SupportsCategories = true;
                model.EditorSettings.IndexUrl = Url.Action("Index", "Blog");
                model.EditorSettings.CategoryPath = Url.Action("Category", "Blog"); // TODO: should we support categories on pages? this action doesn't exist right now
                model.EditorSettings.DeletePath = Url.Action("AjaxDelete", "Blog");
                model.EditorSettings.SavePath = Url.Action("AjaxPost", "Blog");
                model.EditorSettings.NewItemPath = Url.Link("newpost", null);

            }

            return View("Post", model);


        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task AjaxPost(PostViewModel model)
        {
            if (string.IsNullOrEmpty(model.Title))
            {
                log.LogInformation("returning 500 because no title was posted");
                Response.StatusCode = 500;
                return;
            }
            
            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("returning 500 blog not found");
                Response.StatusCode = 500;
                return; 
            }

            if (!User.CanEditProject(project.ProjectId))
            {
                log.LogInformation("returning 403 user is not allowed to edit");
                Response.StatusCode = 403;
                return; 
            }

            string[] categories = new string[0];
            if (!string.IsNullOrEmpty(model.Categories))
            {
                categories = model.Categories.Split(new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries);
            }


            Post post = null;
            if (!string.IsNullOrEmpty(model.Id))
            {
                post = await blogService.GetPost(model.Id);
            }

            var isNew = false;
            if (post != null)
            {
                post.Title = model.Title;
                post.MetaDescription = model.MetaDescription;
                post.Content = model.Content;
                post.Categories = categories.ToList();
            }
            else
            {
                isNew = true;
                var slug = blogService.CreateSlug(model.Title);
                var available = await blogService.SlugIsAvailable(slug);
                if (!available)
                {
                    log.LogInformation("returning 409 because slug already in use");
                    Response.StatusCode = 409;
                    return;
                }

                post = new Post()
                {
                    Author = User.GetDisplayName(),
                    Title = model.Title,
                    MetaDescription = model.MetaDescription,
                    Content = model.Content,
                    Slug = slug,
                    Categories = categories.ToList()
                };
            }

            post.IsPublished = model.IsPublished;
            if(!string.IsNullOrEmpty(model.PubDate))
            {
                var localTime = DateTime.Parse(model.PubDate);
                try
                {
                    //TODO: fix https://github.com/joeaudette/cloudscribe.SimpleContent/issues/1
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(project.TimeZoneId);
                    
                    var pubDate = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Utc);
                    // TODO: this logic probably needs to also be implemented in the metaweblog service in case the pubdate is changed from there
                    if (!isNew)
                    {
                        if (pubDate != post.PubDate)
                        {
                            await blogService.HandlePubDateAboutToChange(post, pubDate).ConfigureAwait(false);
                        }
                    }
                    post.PubDate = pubDate;
                }
                catch(Exception)
                {
                    post.PubDate = localTime;
                }
                

                
                
            }
            
            await blogService.Save(post, isNew);
            string url;
            if(project.IncludePubDateInPostUrls)
            {
                url = Url.Link("postwithdate", 
                    new {
                        year = post.PubDate.Year,
                        month = post.PubDate.Month.ToString("00"),
                        day = post.PubDate.Date.Day.ToString("00"),
                        slug = post.Slug
                    });
            }
            else
            {
                url = Url.Link("postwithoutdate", new { slug = post.Slug });
            }
            
            
            await Response.WriteAsync(url);
            
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task AjaxDelete(string id)
        {
            var blog = await projectService.GetCurrentProjectSettings();

            if (blog == null)
            {
                log.LogInformation("returning 500 blog not found");
                Response.StatusCode = 500;
                return; // new EmptyResult();
            }

            if (!User.CanEditProject(blog.ProjectId))
            {
                log.LogInformation("returning 403 user is not allowed to edit");
                Response.StatusCode = 403;
                return; //new EmptyResult();
            }

            if (string.IsNullOrEmpty(id))
            {
                log.LogInformation("returning 404 postid not provided");
                Response.StatusCode = 404;
                return; //new EmptyResult();
            }

            var post = await blogService.GetPost(id);

            if (post == null)
            {
                log.LogInformation("returning 404 not found");
                Response.StatusCode = 404;
                return; //new EmptyResult();
            }

            var result = await blogService.Delete(post.Id);

            Response.StatusCode = 200;
            return; //new EmptyResult();

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjaxPostComment(CommentViewModel model)
        {
            
            // this should validate the [EmailAddress] on the model
            // failure here should indicate invalid email since it it the only attribute in use
            if (!ModelState.IsValid)
            {
                
                Response.StatusCode = 403;
                await Response.WriteAsync("Please enter a valid e-mail address");
                return new EmptyResult();
            }

            //TODO: validate captcha server side


            var blog = await projectService.GetCurrentProjectSettings();

            if (blog == null)
            {
                log.LogDebug("returning 500 blog not found");
                Response.StatusCode = 500;
                return new EmptyResult();
            }

            if (string.IsNullOrEmpty(model.PostId))
            {
                log.LogDebug("returning 500 because no postid was posted");
                Response.StatusCode = 500;
                return new EmptyResult();
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                log.LogDebug("returning 403 because no name was posted");
                Response.StatusCode = 403;
                await Response.WriteAsync("Please enter a valid name");
                return new EmptyResult();
            }

            if (string.IsNullOrEmpty(model.Content))
            {
                log.LogDebug("returning 403 because no content was posted");
                Response.StatusCode = 403;
                await Response.WriteAsync("Please enter a valid content");
                return new EmptyResult();
            }

            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var comment = new Comment()
            {
                Id = Guid.NewGuid().ToString(),
                Author = model.Name,
                Email = model.Email,
                Website = GetUrl(model.WebSite),
                Ip = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString(),
                UserAgent = userAgent,
                IsAdmin = User.CanEditProject(blog.ProjectId),
                Content = HtmlEncoder.Default.HtmlEncode(model.Content.Trim()).Replace("\n", "<br />"),
                IsApproved = !blog.ModerateComments,
                PubDate = DateTime.UtcNow
            };

            var blogPost = await blogService.GetPost(model.PostId);

            blogPost.Comments.Add(comment);
            await blogService.Save(blogPost, false);


            //post.Comments.Add(comment);
            //Storage.Save(post);

            //if (!context.User.Identity.IsAuthenticated)
            //{
            //    MailMessage mail = GenerateEmail(comment, post, context.Request);
            //    System.Threading.ThreadPool.QueueUserWorkItem((s) => SendEmail(mail));
            //}

            //RenderComment(context, comment);

            var viewModel = new BlogViewModel();
            viewModel.ProjectSettings = blog;
            viewModel.CurrentPost = blogPost;
            viewModel.TmpComment = comment;


            return PartialView("CommentPartial", viewModel);
            
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task AjaxApproveComment(string postId, string commentId)
        {
            
            if (string.IsNullOrEmpty(postId))
            {
                log.LogDebug("returning 404 because no postid was posted");
                Response.StatusCode = 404;
                return;// new EmptyResult();
            }

            if (string.IsNullOrEmpty(commentId))
            {
                log.LogDebug("returning 404 because no commentid was posted");
                Response.StatusCode = 404;
               // await Response.WriteAsync("Comm");
                return;// new EmptyResult();
            }

            var blog = await projectService.GetCurrentProjectSettings();

            if (blog == null)
            {
                log.LogDebug("returning 500 blog not found");
                Response.StatusCode = 500;
                return;// new EmptyResult();
            }

            if (!User.CanEditProject(blog.ProjectId))
            {
                log.LogInformation("returning 403 user is not allowed to edit");
                Response.StatusCode = 403;
                return;// new EmptyResult();
            }

            var blogPost = await blogService.GetPost(postId);

            if (blogPost == null)
            {
                log.LogDebug("returning 404 blog post not found");
                Response.StatusCode = 404;
                return;// new EmptyResult();
            }

            var comment = blogPost.Comments.FirstOrDefault(c => c.Id == commentId);

            if (comment == null)
            {
                log.LogDebug("returning 404 comment not found");
                Response.StatusCode = 404;
                return;// new EmptyResult();
            }

            comment.IsApproved = true;
            //blogPost.Comments.Add(comment);
            await blogService.Save(blogPost, false);

            Response.StatusCode = 200;

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task AjaxDeleteComment(string postId, string commentId)
        {
            if (string.IsNullOrEmpty(postId))
            {
                log.LogDebug("returning 404 because no postid was posted");
                Response.StatusCode = 404;
                return;// new EmptyResult();
            }

            if (string.IsNullOrEmpty(commentId))
            {
                log.LogDebug("returning 404 because no commentid was posted");
                Response.StatusCode = 404;
                // await Response.WriteAsync("Comm");
                return;// new EmptyResult();
            }

            var blog = await projectService.GetCurrentProjectSettings();

            if (blog == null)
            {
                log.LogDebug("returning 500 blog not found");
                Response.StatusCode = 500;
                return;// new EmptyResult();
            }

            if (!User.CanEditProject(blog.ProjectId))
            {
                log.LogInformation("returning 403 user is not allowed to edit");
                Response.StatusCode = 403;
                return;// new EmptyResult();
            }

            var blogPost = await blogService.GetPost(postId);

            if (blogPost == null)
            {
                log.LogDebug("returning 404 blog post not found");
                Response.StatusCode = 404;
                return;// new EmptyResult();
            }

            var comment = blogPost.Comments.FirstOrDefault(c => c.Id == commentId);

            if (comment == null)
            {
                log.LogDebug("returning 404 comment not found");
                Response.StatusCode = 404;
                return;// new EmptyResult();
            }

            //comment.IsApproved = true;
            blogPost.Comments.Remove(comment);
            await blogService.Save(blogPost, false);

            Response.StatusCode = 200;
        }

        private string GetUrl(string website)
        {
            if (!website.Contains("://"))
                website = "http://" + website;

            Uri url;
            if (Uri.TryCreate(website, UriKind.Absolute, out url))
                return url.ToString();

            return string.Empty;
        }

    }
}
