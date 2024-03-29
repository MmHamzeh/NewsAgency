﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLogicLayer.IServices;
using BusinessLogicLayer.ViewModels.News;
using Common.Enums.Models;
using DataAccessLayer.Database;
using DataAccessLayer.Entities;
using DataAccessLayer.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BusinessLogicLayer.Services
{
    public class NewsService : GenericRepository<News>, INewsService
    {
        private readonly INewsCategoryService _newsCategoryService;
        private readonly ICategoryService _categoryService;
        private readonly INewsSeenService _newsSeenService;

        public NewsService(DatabaseContext context, INewsCategoryService newsCategoryService, ICategoryService categoryService, INewsSeenService newsSeenService) : base(context)
        {
            _newsCategoryService = newsCategoryService;
            _categoryService = categoryService;
            _newsSeenService = newsSeenService;
        }
        public override News Add(News entity, int createdById)
        {
            entity.AuthorId = createdById;
            return base.Add(entity, createdById);
        }

        public override Task<News> AddAsync(News entity, int createdById)
        {
            entity.AuthorId = createdById;
            return base.AddAsync(entity, createdById);
        }

        public async Task<NewsPaginationSection> GetLastNewsByCategoryIdAsync(int quentity, int categoryId, int page = 1)
        {
            var cateoryIds = _categoryService.FindCategoryChildsByParentId(categoryId);

            var newsCategories = await _newsCategoryService
                .FindBy(newsCategory => cateoryIds
                    .Any(catIds => catIds == newsCategory.CategoryId))
                .Include(newsCategory => newsCategory.Category)
                .Include(newsCategory => newsCategory.News)
                    .ThenInclude(news => news.Author)
                .OrderByDescending(newsCategory => newsCategory.CreatedOn)
                .Skip((page - 1) * quentity)
                .Take(quentity)
                .ToListAsync();

            var wholeNewsesQuentity = await _newsCategoryService
                .FindBy(newsCategory => cateoryIds
                    .Any(catIds => catIds == newsCategory.CategoryId)).CountAsync();

            var numOfPages = (wholeNewsesQuentity / quentity);

            if (newsCategories.Count() % quentity != 0)
                numOfPages++;

            var categoryNewsSetction = new NewsPaginationSection
            {
                NewsViewModels = new List<NewsViewModel>(),
                MainCategoryId = categoryId,
                MainCategoryTitle = _categoryService.Get(categoryId).Title,
                NumberOfPages = numOfPages,
                CurrentPageNumber = page 
            };

            foreach (var newsCategory in newsCategories)
            {
                var categoryNews = new NewsViewModel()
                {
                    NewsTitle = newsCategory.News.Title,
                    NewsHeadLine = newsCategory.News.Headline,
                    ImageUrl = newsCategory.News.ImageUrl,
                    CategoryTitle = newsCategory.Category.Title,
                    CategoryId = newsCategory.Category.Id,
                    NewsId = newsCategory.NewsId,
                    CreatedOn = newsCategory.News.CreatedOn,
                    AuthorFullName = newsCategory.News.Author.FullName,
                    AuthorId = newsCategory.News.AuthorId
                };

                categoryNewsSetction.NewsViewModels.Add(categoryNews);
            }

            return categoryNewsSetction;
        }

        public async Task<OtherNewsSection> GetLastNewsAsync(int quentity)
        {

            var lastNewses = await GetAll()
                .Include(news => news.Author)
                .Include(news => news.NewsCategories)
                    .ThenInclude(newsCategory => newsCategory.Category)
                .OrderByDescending(news => news.UpdatedOn)
                .Take(quentity)
                .ToListAsync();

            var categoryNewsSetction = new OtherNewsSection
            {
                NewsViewModels = new List<NewsViewModel>(),
            };

            foreach (var news in lastNewses)
            {
                var lastNews = new NewsViewModel()
                {
                    NewsTitle = news.Title,
                    NewsHeadLine = news.Headline,
                    ImageUrl = news.ImageUrl,
                    CategoryTitle = news.NewsCategories.Where(nc => nc.IsMain).FirstOrDefault().Category.Title,
                    CategoryId = news.Id,
                    NewsId = news.Id,
                    CreatedOn = news.CreatedOn,
                    AuthorFullName = news.Author.FullName,
                    AuthorId = news.AuthorId
                };
                categoryNewsSetction.NewsViewModels.Add(lastNews);
            }

            return categoryNewsSetction;
        }

        public NewsViewModel GetNewsFull(int id)
        {
            var news = GetAll()
                .Include(n => n.NewsSeens)
                .Include(n => n.Author)
                .Include(n => n.NewsCategories)
                .ThenInclude(nc => nc.Category)
                .FirstOrDefault(n => n.Id == id);

            var newsViewModel = new NewsViewModel()
            {
                AuthorId = news.AuthorId,
                NewsTitle = news.Title,
                CategoryId = news.NewsCategories.FirstOrDefault(nc => nc.IsMain).CategoryId,
                ImageUrl = news.ImageUrl,
                CreatedOn = news.CreatedOn,
                CategoryTitle = news.NewsCategories.FirstOrDefault(nc => nc.IsMain).Category.Title,
                NewsId = news.Id,
                NewsHeadLine = news.Headline,
                SeenCount = news.NewsSeens.Count(),
                AuthorFullName = news.Author.FullName,
                NewsText = news.Text
            };

            return newsViewModel;
        }

        public async Task<NewsPaginationSection> GetLastNewsByAuthorIdAsync(int quentity, int authorId, int page = 1)
        {
            var authorNewses = await FindBy(news => news.AuthorId == authorId)
                .Include(news => news.Author)
                .Include(news => news.NewsCategories)
                .ThenInclude(newsCategory => newsCategory.Category)
                .OrderByDescending(newsCategory => newsCategory.CreatedOn)
                .Skip((page - 1) * quentity)
                .Take(quentity)
                .ToListAsync();

            var wholeNewsesQuentity = await FindBy(news => news.AuthorId == authorId).CountAsync();

            var numOfPages = (wholeNewsesQuentity / quentity);

            if (authorNewses.Count() % quentity > 0)
                numOfPages++;

            var authorNewsSetction = new NewsPaginationSection
            {
                NewsViewModels = new List<NewsViewModel>(),
                CurrentPageNumber = page,
                NumberOfPages = numOfPages
            };

            foreach (var authorNews in authorNewses)
            {
                var news = new NewsViewModel
                {
                    NewsTitle = authorNews.Title,
                    NewsHeadLine = authorNews.Headline,
                    ImageUrl = authorNews.ImageUrl,
                    CategoryTitle = authorNews.NewsCategories.FirstOrDefault(nc => nc.IsMain).Category.Title,
                    CategoryId = authorNews.NewsCategories.FirstOrDefault(nc => nc.IsMain).Category.Id,
                    NewsId = authorNews.Id,
                    CreatedOn = authorNews.CreatedOn,
                    AuthorFullName = authorNews.Author.FullName,
                    AuthorId = authorNews.AuthorId,
                };

                authorNewsSetction.NewsViewModels.Add(news);
            }

            return authorNewsSetction;
        }

        public async Task<NewsPaginationSection> GetMostSeenNewses(int quentity)
        {

            var mostSeenNewses = await GetAll()
                .Include(newsSeen => newsSeen.NewsCategories)
                .ThenInclude(NewsCategory => NewsCategory.Category)
                .Include(news => news.Author)
                .Include(news => news.NewsSeens)
                .OrderByDescending(news => news.SeenCount)
                .Take(quentity)
                .ToListAsync();

            var wholeNewsesQuentity = 2;

            var numOfPages = (quentity / wholeNewsesQuentity );

            if (quentity % 2 > 0)
                numOfPages++;

            var mostSeenNewsesSection = new NewsPaginationSection
            {
                NewsViewModels = new List<NewsViewModel>(),
                NumberOfPages = numOfPages
            };

            foreach (var mostSeenNews in mostSeenNewses)
            {
                var categoryNews = new NewsViewModel()
                {
                    NewsTitle = mostSeenNews.Title,
                    NewsHeadLine = mostSeenNews.Headline,
                    ImageUrl = mostSeenNews.ImageUrl,
                    CategoryTitle = mostSeenNews.NewsCategories.FirstOrDefault(nc => nc.IsMain == true).Category.Title,
                    CategoryId = mostSeenNews.NewsCategories.FirstOrDefault(nc => nc.IsMain == true).Id,
                    NewsId = mostSeenNews.Id,
                    CreatedOn = mostSeenNews.CreatedOn,
                    AuthorFullName = mostSeenNews.Author.FullName,
                    AuthorId = mostSeenNews.AuthorId
                };

                mostSeenNewsesSection.NewsViewModels.Add(categoryNews);
            }

            return mostSeenNewsesSection;
        }
    }
}