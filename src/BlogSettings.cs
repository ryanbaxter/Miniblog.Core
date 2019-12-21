namespace Miniblog.Core
{
    public class BlogSettings
    {
        public string Owner { get; set; }
        public string Email { get; set; }
        public int PostsPerPage { get; set; } = 2;
        public int CommentsCloseAfterDays { get; set; } = 10;

        public Smtp Smtp { get; set; }

        public Captcha Captcha { get; set; }
    }

    public class Smtp
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 25;
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; } = true;
    }

    public class Captcha
    {
        public string EndPoint { get; set; }
        public string SiteKey { get; set; }
        public string SecretKey { get; set; }
    }
}
