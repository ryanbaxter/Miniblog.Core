using System;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Miniblog.Core.Models;
using Miniblog.Core.Services;
using WebEssentials.AspNetCore.Pwa;

namespace Miniblog.Core.Controllers
{
    public class BlogController : Controller
    {
        private readonly IBlogService _blog;
        private readonly IOptionsSnapshot<BlogSettings> _settings;
        private readonly WebManifest _manifest;

        public BlogController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings, WebManifest manifest)
        {
            _blog = blog;
            _settings = settings;
            _manifest = manifest;
        }

        [Route("/contact")]
        [OutputCache(Profile = "default")]
        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact";
            ViewData["Description"] = _manifest.Name;
            return View("~/Views/Blog/Contact.cshtml");
        }

        [Route("/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Index([FromRoute]int page = 0)
        {
            var posts = await _blog.GetPosts(_settings.Value.PostsPerPage, _settings.Value.PostsPerPage * page);
            ViewData["Title"] = _manifest.Name;
            ViewData["Description"] = _manifest.Description;
            ViewData["prev"] = $"/{page + 1}/";
            ViewData["next"] = $"/{(page <= 1 ? null : page - 1 + "/")}";
            return View("~/Views/Blog/Index.cshtml", posts);
        }

        [Route("/blog/category/{category}/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Category(string category, int page = 0)
        {
            var posts = (await _blog.GetPostsByCategory(category)).Skip(_settings.Value.PostsPerPage * page).Take(_settings.Value.PostsPerPage);
            ViewData["Title"] = _manifest.Name + " " + category;
            ViewData["Description"] = $"Articles posted in the {category} category";
            ViewData["prev"] = $"/blog/category/{category}/{page + 1}/";
            ViewData["next"] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";
            return View("~/Views/Blog/Index.cshtml", posts);
        }

        // This is for redirecting potential existing URLs from the old Miniblog URL format
        [Route("/post/{slug}")]
        [HttpGet]
        public IActionResult Redirects(string slug)
        {
            return LocalRedirectPermanent($"/blog/{slug}");
        }

        [Route("/blog/{slug?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Post(string slug)
        {
            var post = await _blog.GetPostBySlug(slug);

            if (post != null)
            {
                return View(post);
            }

            return NotFound();
        }

        [Route("/blog/edit/{id?}")]
        [HttpGet, Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["AllCats"] = (await _blog.GetCategories()).ToList();

            if (string.IsNullOrEmpty(id))
            {
                return View(new Post());
            }

            var post = await _blog.GetPostById(id);

            if (post != null)
            {
                return View(post);
            }

            return NotFound();
        }

        [Route("/contact")]
        [HttpPost]
        public IActionResult Contact(Contact contact)
        {
            if (!ModelState.IsValid)
            {
                return View("Contact", contact);
            }

            try
            {
                var subject = String.Format("Message from {0} ({1})", contact.Name, contact.Email);

                SendEmail(contact.Email, _settings.Value.Email, subject, contact.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);

                return View("Contact", contact);
            }            

            return Redirect("~/");
        }

        [Route("/blog/{slug?}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> UpdatePost(Post post)
        {
            if (!ModelState.IsValid)
            {
                return View("Edit", post);
            }

            var existing = await _blog.GetPostById(post.ID) ?? post;
            string categories = Request.Form["categories"];

            existing.Categories = categories.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim().ToLowerInvariant()).ToList();
            existing.Title = post.Title.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug.Trim() : Models.Post.CreateSlug(post.Title);
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();
            existing.Excerpt = post.Excerpt.Trim();

            await SaveFilesToDisk(existing);

            await _blog.SavePost(existing);

            return Redirect(post.GetEncodedLink());
        }

        private async Task SaveFilesToDisk(Post post)
        {
            var imgRegex = new Regex("<img[^>]+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var base64Regex = new Regex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase);
            string[] allowedExtensions = new [] {
              ".jpg",
              ".jpeg",
              ".gif",
              ".png",
              ".webp"
            };

            foreach (Match match in imgRegex.Matches(post.Content))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<root>" + match.Value + "</root>");

                var img = doc.FirstChild.FirstChild;
                var srcNode = img.Attributes["src"];
                var fileNameNode = img.Attributes["data-filename"];

                // The HTML editor creates base64 DataURIs which we'll have to convert to image files on disk
                if (srcNode != null && fileNameNode != null)
                {
                    string extension = System.IO.Path.GetExtension(fileNameNode.Value);

                    // Only accept image files
                    if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var base64Match = base64Regex.Match(srcNode.Value);
                    if (base64Match.Success)
                    {
                        byte[] bytes = Convert.FromBase64String(base64Match.Groups["base64"].Value);
                        srcNode.Value = await _blog.SaveFile(bytes, fileNameNode.Value).ConfigureAwait(false);

                        img.Attributes.Remove(fileNameNode);
                        post.Content = post.Content.Replace(match.Value, img.OuterXml);
                    }
                }
            }
        }

        private void SendEmail(string from, string to, string subject, string body)
        {
            using (SmtpClient smtpClient = new SmtpClient(_settings.Value.Smtp.Host, _settings.Value.Smtp.Port))
            {
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;

                smtpClient.Credentials = new System.Net.NetworkCredential(_settings.Value.Smtp.UserName, _settings.Value.Smtp.Password);

                using (MailMessage mailMessage = new MailMessage(from, to, subject, body))
                {
                    smtpClient.Send(mailMessage);
                }
            }
        }

        [Route("/blog/deletepost/{id}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeletePost(string id)
        {
            var existing = await _blog.GetPostById(id);

            if (existing != null)
            {
                await _blog.DeletePost(existing);
                return Redirect("/");
            }

            return NotFound();
        }

        [Route("/blog/comment/{postId}")]
        [HttpPost]
        public async Task<IActionResult> AddComment(string postId, Comment comment)
        {
            var post = await _blog.GetPostById(postId);

            if (!ModelState.IsValid)
            {
                return View("Post", post);
            }

            if (post == null || !post.AreCommentsOpen(_settings.Value.CommentsCloseAfterDays))
            {
                return NotFound();
            }

            comment.IsAdmin = User.Identity.IsAuthenticated;
            comment.Content = comment.Content.Trim();
            comment.Author = comment.Author.Trim();
            comment.Email = comment.Email.Trim();

            // the website form key should have been removed by javascript
            // unless the comment was posted by a spam robot
            if (!Request.Form.ContainsKey("website"))
            {
                post.Comments.Add(comment);
                await _blog.SavePost(post);
            }

            return Redirect(post.GetEncodedLink() + "#" + comment.ID);
        }

        [Route("/blog/comment/{postId}/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            var post = await _blog.GetPostById(postId);

            if (post == null)
            {
                return NotFound();
            }

            var comment = post.Comments.FirstOrDefault(c => c.ID.Equals(commentId, StringComparison.OrdinalIgnoreCase));

            if (comment == null)
            {
                return NotFound();
            }

            post.Comments.Remove(comment);
            await _blog.SavePost(post);

            return Redirect(post.GetEncodedLink() + "#comments");
        }
    }
}
