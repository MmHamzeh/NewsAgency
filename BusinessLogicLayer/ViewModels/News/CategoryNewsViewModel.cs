﻿using System;

namespace BusinessLogicLayer.ViewModels.News
{
    public class CategoryNewsViewModel
    {
        public int NewsId { get; set; }
        public string NewsTitle { get; set; }
        public string NewsHeadLine { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryTitle { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int AuthorId { get; set; }
        public string Author { get; set; }
    }
}