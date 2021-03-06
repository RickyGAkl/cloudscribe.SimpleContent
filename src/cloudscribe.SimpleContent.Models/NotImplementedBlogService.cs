﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cloudscribe.SimpleContent.Models
{
    public class NotImplementedBlogService : IBlogService
    {
        public Task<bool> CommentsAreOpen(Post post, bool userIsOwner)
        {
            throw new NotImplementedException();
        }

        public string CreateSlug(string title)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Delete(string postId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Delete(string blogId, string postId)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, int>> GetArchives()
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, int>> GetCategories()
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, int>> GetCategories(string blogId, bool userIsOwner)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCount(string category)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCount(string projectId, int year, int month = 0, int day = 0)
        {
            throw new NotImplementedException();
        }

        public Task<Post> GetPost(string postId)
        {
            throw new NotImplementedException();
        }

        public Task<Post> GetPost(string blogId, string postId)
        {
            throw new NotImplementedException();
        }

        public Task<Post> GetPostBySlug(string slug)
        {
            throw new NotImplementedException();
        }

        public Task<List<Post>> GetPosts(string blogId, int numberToGet, int year, int month = 0, int day = 0)
        {
            throw new NotImplementedException();
        }

        public Task<List<Post>> GetPosts(string blogId, int year, int month = 0, int day = 0, int pageNumber = 1, int pageSize = 10)
        {
            throw new NotImplementedException();
        }

        public Task<List<Post>> GetRecentPosts(int numberToGet)
        {
            throw new NotImplementedException();
        }

        public Task<List<Post>> GetRecentPosts(string blogId, int numberToGet)
        {
            throw new NotImplementedException();
        }

        

        public Task<List<Post>> GetVisiblePosts()
        {
            throw new NotImplementedException();
        }

        public Task<List<Post>> GetVisiblePosts(string category, int pageNumber)
        {
            throw new NotImplementedException();
        }

        public Task HandlePubDateAboutToChange(Post post, DateTime newPubDate)
        {
            throw new NotImplementedException();
        }

        public Task<string> ResolveBlogUrl(ProjectSettings blog)
        {
            throw new NotImplementedException();
        }

        public Task<string> ResolveMediaUrl(string fileName)
        {
            throw new NotImplementedException();
        }

        public Task<string> ResolvePostUrl(Post post)
        {
            throw new NotImplementedException();
        }

        public Task Save(Post post, bool isNew)
        {
            throw new NotImplementedException();
        }

        public Task Save(string blogId, Post post, bool isNew, bool publish)
        {
            throw new NotImplementedException();
        }

        public Task SaveMedia(string blogId, byte[] bytes, string fileName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SlugIsAvailable(string slug)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SlugIsAvailable(string blogId, string slug)
        {
            throw new NotImplementedException();
        }


    }
}
