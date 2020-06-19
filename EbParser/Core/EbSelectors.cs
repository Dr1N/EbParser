namespace EbParser.Core
{
    internal static class EbSelectors
    {
        public const string PagesSelector = "div.pages>a.page";
        public const string PostLinkSelector = "h3.entry-title a";
        public const string PostTitleSelector = "h1.entry-title";
        public const string PostAuthorSelector = "div.author-date a";
        public const string PostTimeSelector = "time.entry-date";
        public const string PostPosterSelector = "div.section-post-header img.wp-post-image";
        public const string PostContentSelector = "div.the_content";
        public const string PostCategorytSelector = "div.category a";
        public const string PostTagstSelector = "ul.post-tags li";

        public const string PostCommentContainerSelector = "ol.commentlist";
        public const string PostCommentListSelector = "ol.commentlist li.comment";
        public const string PostCommentParentPattern = "li#comment-{0}";
        public const string PostCommentAuthorSelector = "cite.fn";
        public const string PostCommentDateSelector = "div.commentmetadata";
        public const string PostCommentIdSelector = "div.comment-body";
        public const string PostCommentContentSelector = "p";
    }
}