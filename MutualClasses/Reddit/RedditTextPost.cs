namespace MutualClasses.Reddit
{
    public class RedditTextPost
    {
        /// <summary>
        /// The SubReddit this was posted in
        /// </summary>
        public string subreddit { get; set; }

        /// <summary>
        /// The SubReddit this was posted in. Includes the r/
        /// </summary>
        public string subreddit_name_prefixed { get; set; }

        /// <summary>
        /// The text inside the post
        /// </summary>
        public string selftext { get; set; }

        /// <summary>
        /// The title of the post
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// The Id to get details of the author
        /// </summary>
        public string author_fullname { get; set; }

        /// <summary>
        /// Whether this post is nsfw or not
        /// </summary>
        public bool over_18 { get; set; }

        /// <summary>
        /// The actual name of the author
        /// </summary>
        public string url { get; set; }
        
        /// <summary>
        /// The actual name of the author
        /// </summary>
        public string author { get; set; }
    }
}