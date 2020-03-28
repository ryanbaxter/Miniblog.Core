namespace Miniblog.Core
{
    public class BlogSettings
    {
        public PostListView ListView { get; set; } = PostListView.TitlesAndExcerpts;

        public string Owner { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int PostsPerPage { get; set; } = 2;

        public int CommentsCloseAfterDays { get; set; } = 10;

        public Smtp Smtp { get; set; } = new Smtp();

        public Captcha Captcha { get; set; } = new Captcha();
    }

    public class Smtp
    {
        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 25;

        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool EnableSsl { get; set; } = true;
    }

    public class Captcha
    {
        public string EndPoint { get; set; } = string.Empty;

        public string SiteKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public float Score { get; set; } = 0.5F;
    }
}
